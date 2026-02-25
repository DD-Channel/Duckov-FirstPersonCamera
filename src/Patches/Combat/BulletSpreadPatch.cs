using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using FirstPersonCamera;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 子弹散布控制
    /// 在第一人称模式下，当玩家瞄准（ADS）时，消除子弹散布，
    /// 使瞄准射击更加精确
    /// </summary>
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    [HarmonyPatch("ShootOneBullet")]
    internal static class BulletSpreadPatch
    {
        #region 私有字段
        /// <summary>
        /// scatterBeforeControl字段的反射信息（用于消除子弹散布）
        /// </summary>
        private static FieldInfo scatterBeforeControlField;
        
        /// <summary>
        /// ADS判断阈值（用于判断是否在瞄准状态）
        /// </summary>
        private const float AdsValueThreshold = 0.05f;
        #endregion

        #region Harmony补丁方法
        /// <summary>
        /// 射击子弹的前置补丁
        /// 在瞄准状态下消除子弹散布，使射击更加精确
        /// </summary>
        /// <param name="__instance">武器实例</param>
        public static void Prefix(ItemAgent_Gun __instance)
        {
            // 仅在第一人称模式下应用
            if (FirstPersonCameraController.Instance == null || 
                !FirstPersonCameraController.Instance.IsFirstPersonMode)
            {
                return;
            }

            // 检查持有者是否为主角色
            if (__instance.Holder == null || !__instance.Holder.IsMainCharacter)
            {
                return;
            }

            // 检查是否在瞄准状态
            bool isAiming = IsAiming(__instance);

            if (isAiming)
            {
                // 消除子弹散布，使瞄准射击更加精确
                ZeroOutScatter(__instance);
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查是否在瞄准状态
        /// </summary>
        /// <param name="gun">武器实例</param>
        /// <returns>如果在瞄准状态则返回true，否则返回false</returns>
        private static bool IsAiming(ItemAgent_Gun gun)
        {
            // 检查ADS值
            float adsValue = 0f;
            try
            {
                adsValue = gun.AdsValue;
            }
            catch
            {
                adsValue = 0f;
            }

            // 检查右键是否按下
            bool rightMouseButtonPressed = IsRightMouseButtonPressed();

            // 如果ADS值超过阈值或右键按下，则认为在瞄准状态
            return adsValue > AdsValueThreshold || rightMouseButtonPressed;
        }

        /// <summary>
        /// 检查右键是否按下
        /// </summary>
        /// <returns>如果右键按下则返回true，否则返回false</returns>
        private static bool IsRightMouseButtonPressed()
        {
            try
            {
                // 优先使用新输入系统
                if (Keyboard.current != null && Mouse.current != null)
                {
                    var mouse = Mouse.current;
                    if (mouse != null)
                    {
                        return mouse.rightButton.isPressed;
                    }
                }
                // 后备方案：使用旧输入系统
                return Input.GetMouseButton(1);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 消除子弹散布
        /// 通过反射将scatterBeforeControl字段设置为0，防止散布计算
        /// </summary>
        /// <param name="gun">武器实例</param>
        private static void ZeroOutScatter(ItemAgent_Gun gun)
        {
            // 延迟获取字段信息
            if (scatterBeforeControlField == null)
            {
                scatterBeforeControlField = typeof(ItemAgent_Gun).GetField(
                    "scatterBeforeControl",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (scatterBeforeControlField != null)
            {
                try
                {
                    // 将散布值设置为0，消除子弹散布
                    scatterBeforeControlField.SetValue(gun, 0f);
                }
                catch
                {
                    // 设置失败，静默处理
                }
            }
        }
        #endregion
    }
}

