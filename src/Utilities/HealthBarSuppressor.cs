using UnityEngine;
using Duckov.UI;
using System;
using System.Reflection;

namespace FirstPersonCamera
{
    /// <summary>
    /// 血条抑制器
    /// 在第一人称模式下禁用敌人和玩家头部的血条显示
    /// 使用独立的组件和事件系统，避免与FirstPersonCameraController的血条管理产生冲突
    /// 注意：此组件使用禁用方式，而非透明方式（与Controller中的方法不同）
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class HealthBarSuppressor : MonoBehaviour
    {
        #region 常量定义
        /// <summary>
        /// 反射字段名：OnRequestHealthBar（Health类的血条请求事件）
        /// </summary>
        private const string ReflectionFieldNameOnRequestHealthBar = "OnRequestHealthBar";
        
        /// <summary>
        /// 目标方法名：OnHealthBarRequested（FirstPersonCameraController中的方法）
        /// </summary>
        private const string TargetMethodNameOnHealthBarRequested = "OnHealthBarRequested";
        #endregion

        #region 静态字段
        /// <summary>
        /// 事件是否已挂钩（防止重复订阅）
        /// </summary>
        private static bool eventHooked;
        
        /// <summary>
        /// 是否处于抑制状态（当为true时，禁用敌人和玩家的血条）
        /// </summary>
        private static bool suppressed;
        #endregion

        #region 实例字段
        /// <summary>
        /// 上次的抑制状态（用于检测状态变化）
        /// </summary>
        private bool lastSuppressed;
        #endregion

        #region Unity生命周期方法
        /// <summary>
        /// Unity Awake方法：初始化组件
        /// 使组件在场景切换时保持存活，并挂钩血条请求事件
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Hook();
        }

        /// <summary>
        /// Unity OnDestroy方法：清理资源
        /// 取消挂钩血条请求事件
        /// </summary>
        private void OnDestroy()
        {
            Unhook();
        }

        /// <summary>
        /// Unity Update方法：检测第一人称模式状态并更新抑制状态
        /// 根据第一人称模式的启用/禁用状态，动态启用/禁用血条抑制
        /// </summary>
        private void Update()
        {
            // 检查是否应该处于抑制状态（即第一人称模式是否激活）
            bool shouldSuppress = ShouldSuppressHealthBars();

            // 如果抑制状态发生变化，更新抑制状态
            if (shouldSuppress != lastSuppressed)
            {
                SetSuppressed(shouldSuppress);
                lastSuppressed = shouldSuppress;

                // 如果退出抑制状态，尝试为所有Health对象重新请求血条
                if (!shouldSuppress)
                {
                    TryRequestBarsForAll();
                }
            }
        }
        #endregion

        #region 事件管理方法
        /// <summary>
        /// 挂钩血条请求事件
        /// 订阅Health.OnRequestHealthBar事件，以便拦截血条请求
        /// </summary>
        private static void Hook()
        {
            if (eventHooked) return;

            try
            {
                Health.OnRequestHealthBar += OnRequestHealthBar;
                eventHooked = true;
            }
            catch
            {
                // 挂钩失败，静默处理
            }
        }

        /// <summary>
        /// 取消挂钩血条请求事件
        /// 取消订阅Health.OnRequestHealthBar事件
        /// </summary>
        private static void Unhook()
        {
            if (!eventHooked) return;

            try
            {
                Health.OnRequestHealthBar -= OnRequestHealthBar;
            }
            catch
            {
                // 取消挂钩失败，静默处理
            }
            finally
            {
                eventHooked = false;
            }
        }

        /// <summary>
        /// 血条请求事件回调
        /// 当游戏请求显示血条时，如果处于抑制状态，则禁用敌人和玩家的血条
        /// </summary>
        /// <param name="health">请求显示血条的Health对象</param>
        private static void OnRequestHealthBar(Health health)
        {
            try
            {
                // 如果未处于抑制状态，不处理
                if (!suppressed) return;

                // 如果Health对象无效，不处理
                if (health == null) return;

                // 如果是主角色或敌人，则禁用血条
                if (IsMainCharacterOrEnemy(health))
                {
                    health.showHealthBar = false;
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 检查是否为主角色或敌人
        /// </summary>
        /// <param name="health">Health对象</param>
        /// <returns>如果是主角色或敌人则返回true，否则返回false</returns>
        private static bool IsMainCharacterOrEnemy(Health health)
        {
            if (health == null) return false;
            return health.IsMainCharacterHealth || Team.IsEnemy(Teams.player, health.team);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 设置抑制状态
        /// 在场景加载后调用，用于清除现有血条并禁用管理器
        /// </summary>
        /// <param name="value">是否抑制血条显示</param>
        public static void SetSuppressed(bool value)
        {
            suppressed = value;
        }

        /// <summary>
        /// 移除控制器的事件处理器
        /// 用于移除FirstPersonCameraController的OnHealthBarRequested事件处理器，
        /// 避免与HealthBarSuppressor产生冲突
        /// </summary>
        public static void RemoveControllerHandler()
        {
            try
            {
                // 通过反射获取OnRequestHealthBar事件字段
                var eventField = typeof(Health).GetField(
                    ReflectionFieldNameOnRequestHealthBar,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                var eventDelegate = eventField?.GetValue(null) as Action<Health>;
                if (eventDelegate == null) return;

                // 遍历所有事件处理器
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    var target = handler.Target;
                    
                    // 检查是否是FirstPersonCameraController的OnHealthBarRequested方法
                    if (target != null &&
                        target.GetType().FullName == typeof(FirstPersonCameraController).FullName &&
                        handler.Method.Name == TargetMethodNameOnHealthBarRequested)
                    {
                        // 移除该事件处理器
                        Health.OnRequestHealthBar -= (Action<Health>)handler;
                    }
                }
            }
            catch
            {
                // 移除失败，静默处理
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查是否应该抑制血条
        /// </summary>
        /// <returns>如果应该抑制血条则返回true，否则返回false</returns>
        private bool ShouldSuppressHealthBars()
        {
            try
            {
                var controller = FirstPersonCameraController.Instance;
                return controller != null && controller.IsFirstPersonMode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试为所有Health对象重新请求血条
        /// 在退出抑制状态时调用，用于恢复血条显示
        /// </summary>
        private static void TryRequestBarsForAll()
        {
            try
            {
                // 检查HealthBarManager是否存在
                if (HealthBarManager.Instance == null) return;

                // 查找场景中所有Health对象
                var allHealth = UnityEngine.Object.FindObjectsOfType<Health>(true);
                
                foreach (var health in allHealth)
                {
                    if (health == null) continue;

                    // 如果血条显示标志为true，则重新请求血条
                    if (health.showHealthBar)
                    {
                        HealthBarManager.RequestHealthBar(health, null);
                    }
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion
    }
}


