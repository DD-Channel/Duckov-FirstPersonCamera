using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Duckov.UI;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 血条管理模块
    /// 负责在第一人称模式下将敌人和玩家的血条设置为透明（而非隐藏），
    /// 以便在退出第一人称模式时能够正确恢复血条显示状态
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 轻量扫描每帧最多处理的血条数量（用于性能优化）
        /// </summary>
        private const int LIGHT_SWEEP_MAX_COUNT_PER_FRAME = 32;
        
        /// <summary>
        /// 定期扫描每帧最多处理的血条数量（用于低预算定期扫描）
        /// </summary>
        private const int PERIODIC_SWEEP_MAX_COUNT_PER_FRAME = 24;
        
        /// <summary>
        /// 血条完全透明的Alpha值
        /// </summary>
        private const float TRANSPARENT_ALPHA = 0f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 缓存的敌人/玩家血条显示状态（用于恢复原始状态）
        /// Key: Health对象，Value: 原始的showHealthBar值
        /// </summary>
        private readonly Dictionary<Health, bool> cachedEnemyShowBar = new Dictionary<Health, bool>();
        
        /// <summary>
        /// 缓存的透明血条的原始Alpha值（用于恢复透明度）
        /// Key: HealthBar对象，Value: 原始的CanvasGroup.alpha值
        /// </summary>
        private readonly Dictionary<HealthBar, float> transparentBarsPrevAlpha = new Dictionary<HealthBar, float>();
        
        /// <summary>
        /// 事件是否已挂钩（防止重复订阅）
        /// </summary>
        private bool eventHooked;
        
        /// <summary>
        /// 下次血条扫描的时间戳（用于定期扫描）
        /// </summary>
        private float nextHealthBarSweepTime;
        
        /// <summary>
        /// 轻量扫描的当前游标位置（用于分帧处理）
        /// </summary>
        private int lightSweepCursor;
        
        /// <summary>
        /// 轻量扫描的缓存数组（避免重复查找）
        /// </summary>
        private HealthBar[] lightSweepCache;
        #endregion

        #region 事件管理
        /// <summary>
        /// 挂钩血条请求事件
        /// 当游戏请求显示血条时，我们会拦截并使其透明
        /// </summary>
        private void HookHealthBarEvents()
        {
            if (eventHooked) return;
            
            try
            {
                Health.OnRequestHealthBar += OnHealthBarRequested;
                eventHooked = true;
            }
            catch
            {
                // 事件订阅失败，静默处理
            }
        }

        /// <summary>
        /// 取消挂钩血条请求事件
        /// 在组件销毁或退出第一人称模式时调用
        /// </summary>
        private void UnhookHealthBarEvents()
        {
            if (!eventHooked) return;
            
            try
            {
                Health.OnRequestHealthBar -= OnHealthBarRequested;
            }
            catch
            {
                // 事件取消订阅失败，静默处理
            }
            finally
            {
                eventHooked = false;
            }
        }
        #endregion

        #region 目标识别方法
        /// <summary>
        /// 判断指定的Health对象是否为敌人
        /// </summary>
        /// <param name="health">要检查的Health对象</param>
        /// <returns>如果是敌人则返回true，否则返回false</returns>
        private bool IsEnemy(Health health)
        {
            if (health == null) return false;
            if (health.IsMainCharacterHealth) return false; // 主角色不是敌人
            return Team.IsEnemy(Teams.player, health.team);
        }

        /// <summary>
        /// 判断指定的Health对象是否为主角色（玩家自己）
        /// </summary>
        /// <param name="health">要检查的Health对象</param>
        /// <returns>如果是主角色则返回true，否则返回false</returns>
        private bool IsSelf(Health health)
        {
            try
            {
                return health != null && health.IsMainCharacterHealth;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断指定的Health对象是否为目标（敌人或玩家自己）
        /// </summary>
        /// <param name="health">要检查的Health对象</param>
        /// <returns>如果是目标则返回true，否则返回false</returns>
        private bool IsTarget(Health health)
        {
            return IsEnemy(health) || IsSelf(health);
        }
        #endregion

        #region 事件回调
        /// <summary>
        /// 血条请求事件回调
        /// 当游戏请求显示血条时，如果目标为敌人或玩家自己，则将其设置为透明
        /// </summary>
        /// <param name="health">请求显示血条的Health对象</param>
        private void OnHealthBarRequested(Health health)
        {
            // 仅在第一人称模式下处理
            if (!isFirstPersonMode) return;
            
            // 仅处理敌人和玩家自己的血条
            if (!IsTarget(health)) return;
            
            // 延迟到下一帧处理，确保血条已创建
            StartCoroutine(MakeHealthBarTransparentNextFrame(health));
            
            // 缓存原始显示状态，并确保显示标志为true（我们通过透明化隐藏，而不是禁用）
            try
            {
                if (!cachedEnemyShowBar.ContainsKey(health))
                {
                    cachedEnemyShowBar[health] = health.showHealthBar;
                    // 保持显示标志为true，仅通过透明度隐藏
                    health.showHealthBar = true;
                }
            }
            catch
            {
                // 缓存失败，静默处理
            }
        }

        /// <summary>
        /// 延迟使血条透明化的协程
        /// 等待一帧以确保血条UI已完全创建
        /// </summary>
        /// <param name="health">目标Health对象</param>
        private IEnumerator MakeHealthBarTransparentNextFrame(Health health)
        {
            yield return new WaitForEndOfFrame();
            TryMakeTargetBarsTransparent(health);
        }
        #endregion

        #region 血条透明化方法
        /// <summary>
        /// 尝试使指定Health对象的所有血条透明化
        /// </summary>
        /// <param name="health">目标Health对象</param>
        private void TryMakeTargetBarsTransparent(Health health)
        {
            if (health == null) return;
            
            try
            {
                // 查找场景中所有与目标Health关联的血条
                var bars = Object.FindObjectsOfType<HealthBar>(true);
                foreach (var bar in bars)
                {
                    if (bar == null) continue;
                    if (bar.target != health) continue;
                    if (!bar.gameObject.activeInHierarchy) continue;
                    
                    MakeBarTransparent(bar);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 使单个血条透明化
        /// 通过CanvasGroup组件控制透明度，而不是释放血条对象
        /// </summary>
        /// <param name="bar">要透明化的HealthBar对象</param>
        private void MakeBarTransparent(HealthBar bar)
        {
            if (bar == null) return;
            
            try
            {
                // 获取或添加CanvasGroup组件
                var canvasGroup = bar.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = bar.gameObject.AddComponent<CanvasGroup>();
                }
                
                // 缓存原始Alpha值（如果尚未缓存）
                if (!transparentBarsPrevAlpha.ContainsKey(bar))
                {
                    transparentBarsPrevAlpha[bar] = canvasGroup.alpha;
                }
                
                // 设置为完全透明
                canvasGroup.alpha = TRANSPARENT_ALPHA;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion

        #region 批量处理方法
        /// <summary>
        /// 尝试隐藏所有敌人和玩家自己的血条
        /// 通过遍历场景中所有Health对象并使其血条透明化
        /// 注意：此方法性能开销较大，应谨慎使用
        /// </summary>
        private void TryHideAllEnemyHealthBars()
        {
            try
            {
                var allHealth = Object.FindObjectsOfType<Health>(true);
                foreach (var health in allHealth)
                {
                    if (!IsTarget(health)) continue;
                    
                    // 缓存原始显示状态
                    if (!cachedEnemyShowBar.ContainsKey(health))
                    {
                        cachedEnemyShowBar[health] = health.showHealthBar;
                    }
                    
                    // 保持显示标志为true，仅通过透明度隐藏
                    if (!health.showHealthBar)
                    {
                        health.showHealthBar = true;
                    }
                    
                    // 使关联的血条透明化
                    TryMakeTargetBarsTransparent(health);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 轻量级分帧扫描，使符合条件的血条透明化
        /// 每帧最多处理指定数量的血条，避免单帧卡顿
        /// </summary>
        /// <param name="maxCount">每帧最多处理的血条数量</param>
        private void LightSweepTransparentSomeBars(int maxCount)
        {
            // 如果缓存为空或已处理完，重新获取所有血条
            if (lightSweepCache == null || lightSweepCursor >= lightSweepCache.Length)
            {
                lightSweepCache = Object.FindObjectsOfType<HealthBar>(true);
                lightSweepCursor = 0;
            }
            
            // 处理指定数量的血条
            int processed = 0;
            while (lightSweepCache != null && 
                   lightSweepCursor < lightSweepCache.Length && 
                   processed < maxCount)
            {
                var bar = lightSweepCache[lightSweepCursor++];
                
                // 跳过无效或未激活的血条
                if (bar == null || !bar.gameObject.activeInHierarchy)
                {
                    continue;
                }
                
                var target = bar.target;
                if (target == null)
                {
                    continue;
                }
                
                // 仅处理敌人和玩家自己的血条
                if (!IsTarget(target))
                {
                    continue;
                }
                
                // 缓存原始显示状态
                if (!cachedEnemyShowBar.ContainsKey(target))
                {
                    cachedEnemyShowBar[target] = target.showHealthBar;
                }
                
                // 确保显示标志为true
                if (!target.showHealthBar)
                {
                    target.showHealthBar = true;
                }
                
                // 使血条透明化
                MakeBarTransparent(bar);
                processed++;
            }
            
            // 如果处理完成，清空缓存
            if (lightSweepCache != null && lightSweepCursor >= lightSweepCache.Length)
            {
                lightSweepCache = null;
                lightSweepCursor = 0;
            }
        }

        /// <summary>
        /// 完整扫描并确保所有符合条件的血条透明化
        /// 作为后备方案，确保没有遗漏的血条
        /// 注意：此方法性能开销较大，应谨慎使用
        /// </summary>
        private void SweepAndEnsureAllBarsTransparent()
        {
            try
            {
                var bars = Object.FindObjectsOfType<HealthBar>(true);
                foreach (var bar in bars)
                {
                    if (bar == null || !bar.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    
                    var target = bar.target;
                    if (target == null)
                    {
                        continue;
                    }
                    
                    if (!IsTarget(target))
                    {
                        continue;
                    }
                    
                    // 缓存原始显示状态
                    if (!cachedEnemyShowBar.ContainsKey(target))
                    {
                        cachedEnemyShowBar[target] = target.showHealthBar;
                    }
                    
                    // 确保显示标志为true
                    if (!target.showHealthBar)
                    {
                        target.showHealthBar = true;
                    }
                    
                    // 使血条透明化
                    MakeBarTransparent(bar);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion

        #region 恢复方法
        /// <summary>
        /// 恢复所有缓存的敌人/玩家血条显示标志
        /// 在退出第一人称模式时调用，恢复原始的showHealthBar状态
        /// </summary>
        private void RestoreEnemyHealthBarFlags()
        {
            try
            {
                foreach (var kvp in cachedEnemyShowBar)
                {
                    var health = kvp.Key;
                    if (health != null)
                    {
                        health.showHealthBar = kvp.Value;
                    }
                }
            }
            catch
            {
                // 恢复失败，静默处理
            }
            finally
            {
                cachedEnemyShowBar.Clear();
            }
        }

        /// <summary>
        /// 恢复所有透明血条的透明度
        /// 在退出第一人称模式时调用，恢复原始的CanvasGroup.alpha值
        /// </summary>
        private void RestoreEnemyHealthBarTransparency()
        {
            try
            {
                foreach (var kvp in transparentBarsPrevAlpha)
                {
                    var bar = kvp.Key;
                    if (bar == null)
                    {
                        continue;
                    }
                    
                    var canvasGroup = bar.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = kvp.Value;
                    }
                }
            }
            catch
            {
                // 恢复失败，静默处理
            }
            finally
            {
                transparentBarsPrevAlpha.Clear();
            }
        }
        #endregion
    }
}

