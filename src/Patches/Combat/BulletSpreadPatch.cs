using HarmonyLib;
using UnityEngine;
using System.Reflection;
using FirstPersonCamera;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 子弹散布控制
    /// 在第一人称模式下，完全消除子弹散布，使射击更加精确
    /// </summary>
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    [HarmonyPatch("ShootOneBullet")]
    internal static class BulletSpreadPatch
    {
        #region 私有字段
        /// <summary>
        /// scatterBeforeControl字段的反射信息
        /// </summary>
        private static FieldInfo scatterBeforeControlField;
        #endregion

        #region Harmony补丁方法
        static void Prefix(ItemAgent_Gun __instance)
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

            // 无论是否瞄准，都消除子弹散布
            ZeroOutScatter(__instance);
        }
        #endregion

        #region 辅助方法
        private static void ZeroOutScatter(ItemAgent_Gun gun)
        {
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
        #endregion
    }
}