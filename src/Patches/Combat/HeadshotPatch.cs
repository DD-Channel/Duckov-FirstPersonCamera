using HarmonyLib;
using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 头部暴击判定
    /// 在第一人称模式下，当攻击命中头部碰撞体时，强制触发暴击
    /// 仅对物理伤害类型生效，避免技能/真实伤害受影响
    /// </summary>
    [HarmonyPatch(typeof(Health))]
    [HarmonyPatch("Hurt")]
    internal static class HeadshotPatch
    {
        #region 常量定义
        /// <summary>
        /// 头部碰撞体图层名称
        /// </summary>
        private const string HeadLayerName = "HeadCollider";
        
        /// <summary>
        /// 命中点邻域判定半径（米）
        /// </summary>
        private const float HitCheckRadius = 0.16f;
        
        /// <summary>
        /// 最小暴击伤害倍率
        /// </summary>
        private const float MinCritDamageFactor = 2f;
        
        /// <summary>
        /// 强制暴击的暴击率（1.0表示100%暴击）
        /// </summary>
        private const float ForcedCritRate = 1f;
        
        /// <summary>
        /// 未初始化的图层值（用于标记未初始化状态）
        /// </summary>
        private const int UninitializedLayer = -2;
        #endregion

        #region 私有字段
        /// <summary>
        /// 头部图层索引（延迟初始化）
        /// </summary>
        private static int headLayer = UninitializedLayer;
        
        /// <summary>
        /// 头部图层掩码
        /// </summary>
        private static int headMask;
        #endregion

        #region Harmony补丁方法
        /// <summary>
        /// 伤害处理的前置补丁
        /// 在第一人称模式下，检查是否命中头部，如果命中则强制触发暴击
        /// </summary>
        /// <param name="__instance">Health实例</param>
        /// <param name="damageInfo">伤害信息（引用参数，可修改）</param>
        public static void Prefix(Health __instance, ref DamageInfo damageInfo)
        {
            // 只在第一人称模式下才应用头部暴击补丁
            if (!IsFirstPersonModeActive())
            {
                // 不在第一人称模式下，不修改暴击机制，恢复原版行为
                return;
            }

            // 初始化头部图层（延迟初始化）
            if (!InitializeHeadLayer())
            {
                // 没有配置HeadCollider层时跳过
                return;
            }

            // 仅对物理伤害型进行部位判定（避免技能/真实伤害受影响）
            if (damageInfo.damageType != DamageTypes.normal)
            {
                return;
            }

            // 获取角色对象
            var character = __instance.TryGetCharacter();
            if (character == null)
            {
                return;
            }

            // 检查是否命中头部
            if (IsHeadHit(damageInfo.damagePoint, character))
            {
                // 强制触发暴击
                ApplyHeadshotCrit(ref damageInfo);
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查第一人称模式是否激活
        /// </summary>
        /// <returns>如果第一人称模式激活则返回true，否则返回false</returns>
        private static bool IsFirstPersonModeActive()
        {
            var fpsController = FirstPersonCameraController.Instance;
            return fpsController != null && fpsController.IsFirstPersonMode;
        }

        /// <summary>
        /// 初始化头部图层
        /// </summary>
        /// <returns>如果初始化成功则返回true，否则返回false</returns>
        private static bool InitializeHeadLayer()
        {
            // 延迟初始化
            if (headLayer == UninitializedLayer)
            {
                headLayer = LayerMask.NameToLayer(HeadLayerName);
                headMask = headLayer >= 0 ? (1 << headLayer) : 0;
            }

            // 检查图层是否有效
            return headLayer >= 0 && headMask != 0;
        }

        /// <summary>
        /// 检查是否命中头部
        /// </summary>
        /// <param name="damagePoint">伤害点世界坐标</param>
        /// <param name="character">角色对象</param>
        /// <returns>如果命中头部则返回true，否则返回false</returns>
        private static bool IsHeadHit(Vector3 damagePoint, CharacterMainControl character)
        {
            // 使用球形重叠检测查找头部碰撞体
            Collider[] hits;
            try
            {
                hits = Physics.OverlapSphere(
                    damagePoint,
                    HitCheckRadius,
                    headMask,
                    QueryTriggerInteraction.Collide);
            }
            catch
            {
                return false;
            }

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            // 检查是否命中了该角色的头部碰撞体
            foreach (var collider in hits)
            {
                if (collider == null) continue;

                Transform transform = collider.transform;
                if (transform == null) continue;

                // 通过根节点判断属于同一个角色
                if (transform.root == character.transform.root)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 应用头部暴击效果
        /// 强制设置暴击率和暴击伤害倍率
        /// </summary>
        /// <param name="damageInfo">伤害信息（引用参数）</param>
        private static void ApplyHeadshotCrit(ref DamageInfo damageInfo)
        {
            // 强制暴击：把critRate置为1，使Health.Hurt内部计算命中为暴击，并走头盔护甲分支
            // 仅在第一人称模式下生效
            damageInfo.critRate = ForcedCritRate;

            // 保持已有的critDamageFactor，或至少不低于最小倍率
            if (damageInfo.critDamageFactor < MinCritDamageFactor)
            {
                damageInfo.critDamageFactor = MinCritDamageFactor;
            }
        }
        #endregion
    }
}

