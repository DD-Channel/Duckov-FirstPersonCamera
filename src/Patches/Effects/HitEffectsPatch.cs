using HarmonyLib;
using UnityEngine;
using FirstPersonCamera;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 受击特效触发
    /// 钩住主角色受到伤害的事件，触发第一人称受击特效
    /// 注意：不包含相机震动（已移除）
    /// </summary>
    [HarmonyPatch(typeof(Health))]
    [HarmonyPatch("Hurt")]
    internal static class HitEffectsPatch
    {
        #region 常量定义
        /// <summary>
        /// 默认伤害值（当伤害值无法获取时使用）
        /// </summary>
        private const float DefaultDamageValue = 20f;
        #endregion

        #region Harmony补丁方法
        /// <summary>
        /// 伤害处理的后置补丁
        /// 在主角色受到伤害后，触发第一人称受击特效
        /// </summary>
        /// <param name="__instance">Health实例</param>
        /// <param name="damageInfo">伤害信息</param>
        static void Postfix(Health __instance, DamageInfo damageInfo)
        {
            try
            {
                // 检查第一人称控制器是否存在且处于第一人称模式
                var controller = FirstPersonCameraController.Instance;
                if (controller == null || !controller.IsFirstPersonMode)
                {
                    return;
                }

                // 检查是否为主角色
                if (__instance == null || !__instance.IsMainCharacterHealth)
                {
                    return;
                }

                // 获取伤害值（如果无法获取则使用默认值）
                float damage = GetDamageValue(damageInfo);

                // 获取伤害点（如果可用）
                Vector3? damagePoint = GetDamagePoint(damageInfo);

                // 触发受击特效
                controller.OnHitEffect(damage, damagePoint);
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取伤害值
        /// </summary>
        /// <param name="damageInfo">伤害信息</param>
        /// <returns>伤害值，如果无法获取则返回默认值</returns>
        private static float GetDamageValue(DamageInfo damageInfo)
        {
            // 注意：由于伤害值可能未暴露，使用合理的默认值
            return DefaultDamageValue;
        }

        /// <summary>
        /// 获取伤害点
        /// </summary>
        /// <param name="damageInfo">伤害信息</param>
        /// <returns>伤害点世界坐标，如果无法获取则返回null</returns>
        private static Vector3? GetDamagePoint(DamageInfo damageInfo)
        {
            try
            {
                return damageInfo.damagePoint;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}

