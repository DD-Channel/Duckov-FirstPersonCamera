using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 子弹散布控制
    /// 根据玩家设置，在瞄准时消除子弹散布
    /// </summary>
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    [HarmonyPatch("ShootOneBullet")]
    internal static class BulletSpreadPatch
    {
        private static FieldInfo scatterBeforeControlField;
        private const float AdsValueThreshold = 0.05f;

        public static void Prefix(ItemAgent_Gun __instance)
        {
            // 仅在第一人称模式下生效
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode)
                return;

            // 仅当玩家是主角时生效
            if (__instance.Holder == null || !__instance.Holder.IsMainCharacter)
                return;

            // 读取玩家设置：是否启用瞄准散布消除（0=禁用，1=启用）
            int enableSpread = OptionsHelper.LoadInt(OptionsUIConstants.EnableSpreadKey, 0);
            if (enableSpread == 0)
                return; // 玩家选择保留散布，不干预

            // 检查是否处于瞄准状态
            if (!IsAiming(__instance))
                return;

            // 消除散布
            ZeroOutScatter(__instance);
        }

        private static bool IsAiming(ItemAgent_Gun gun)
        {
            // 通过 AdsValue 判断是否在开镜状态
            float adsValue = 0f;
            try { adsValue = gun.AdsValue; } catch { }
            bool rightMousePressed = IsRightMouseButtonPressed();
            return adsValue > AdsValueThreshold || rightMousePressed;
        }

        private static bool IsRightMouseButtonPressed()
        {
            try
            {
                if (Keyboard.current != null && Mouse.current != null)
                {
                    var mouse = Mouse.current;
                    if (mouse != null) return mouse.rightButton.isPressed;
                }
                return Input.GetMouseButton(1);
            }
            catch { return false; }
        }

        private static void ZeroOutScatter(ItemAgent_Gun gun)
        {
            // 延迟获取反射字段，提高性能
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
                    scatterBeforeControlField.SetValue(gun, 0f);
                }
                catch { }
            }
        }
    }
}