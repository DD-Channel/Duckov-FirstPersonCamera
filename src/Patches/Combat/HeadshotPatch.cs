using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace FirstPersonCamera
{
    [HarmonyPatch(typeof(Health))]
    [HarmonyPatch("Hurt")]
    internal static class HeadshotPatch
    {
        // 头部碰撞体图层
        private static readonly string[] HeadLayerNames = { "HeadCollider", "Head" };

        // 检测半径：普通敌人0.165米，特殊敌人0.33米
        private const float DefaultRadius = 0.165f;
        private const float SpecialRadius = 0.33f;

        // 特殊敌人关键词（基于CharacterModel名称）
        private static readonly HashSet<string> SpecialKeywords = new HashSet<string>
        {
            "CharacterModel_Duck_Boss_1",
            "0_CharacterModel_Custom_Enemy _Boss_Storm5"
        };

        private static int? headMask; // 延迟初始化

        private static bool IsFirstPersonModeActive()
        {
            var controller = FirstPersonCameraController.Instance;
            return controller != null && controller.IsFirstPersonMode;
        }

        private static bool InitializeHeadMask()
        {
            if (headMask.HasValue)
                return headMask.Value != 0;

            int mask = 0;
            foreach (string layer in HeadLayerNames)
            {
                int id = LayerMask.NameToLayer(layer);
                if (id >= 0)
                    mask |= 1 << id;
            }
            headMask = mask;
            return mask != 0;
        }

        private static bool IsHeadHit(Vector3 point, CharacterMainControl character, float radius)
        {
            if (!InitializeHeadMask() || headMask == 0)
                return false;

            Collider[] hits = Physics.OverlapSphere(point, radius, headMask.Value, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            foreach (var col in hits)
            {
                if (col == null) continue;

                // 归属判断：根对象匹配 或 组件匹配
                bool rootMatch = col.transform.root == character.transform.root;
                bool compMatch = col.GetComponentInParent<CharacterMainControl>() == character;

                if (rootMatch || compMatch)
                    return true;
            }
            return false;
        }

        private static bool IsSpecialEnemy(CharacterMainControl character)
        {
            var model = character.GetComponentInChildren<CharacterModel>(true);
            if (model == null) return false;

            string modelName = model.name;
            foreach (string kw in SpecialKeywords)
            {
                if (modelName.Contains(kw))
                    return true;
            }
            return false;
        }

        public static void Prefix(Health __instance, ref DamageInfo damageInfo)
        {
            // 仅第一人称生效
            if (!IsFirstPersonModeActive()) return;

            // 仅物理伤害
            if (damageInfo.damageType != DamageTypes.normal) return;

            var character = __instance.TryGetCharacter();
            if (character == null) return;

            // 选择半径
            float radius = IsSpecialEnemy(character) ? SpecialRadius : DefaultRadius;

            // 检测头部命中
            if (IsHeadHit(damageInfo.damagePoint, character, radius))
            {
                // 强制暴击
                damageInfo.critRate = 1f;
                if (damageInfo.critDamageFactor < 2f)
                    damageInfo.critDamageFactor = 2f;
            }
        }
    }
}