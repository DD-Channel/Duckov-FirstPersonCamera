using HarmonyLib;
using System.Reflection;
using FirstPersonCamera.Utilities;
using FirstPersonCamera;
using FOW;
using UnityEngine;

namespace FirstPersonCamera.Patches
{
    /// <summary>
    /// Harmony 补丁：开镜时根据倍镜倍率动态增加迷雾视野半径（ViewDistance），保持角度和感知半径不变
    /// </summary>
    [HarmonyPatch(typeof(FogOfWarManager))]
    [HarmonyPatch("Update")]
    [HarmonyPriority(550)]
    public static class Patch_FogOfWar_AimZoom
    {
        private static FieldInfo _viewDistanceField;
        private static FieldInfo _mainVisField;
        private static bool _fieldsInitialized;

        private static bool _wasAiming = false;
        private static float _savedViewDistance = -1f;

        private static void InitializeFields()
        {
            if (_fieldsInitialized) return;
            var type = typeof(FogOfWarManager);
            _viewDistanceField = type.GetField("viewDistance", BindingFlags.NonPublic | BindingFlags.Instance);
            _mainVisField = type.GetField("mainVis", BindingFlags.Public | BindingFlags.Instance);
            _fieldsInitialized = true;
        }

        public static void Postfix(FogOfWarManager __instance)
        {
            InitializeFields();
            if (_viewDistanceField == null || _mainVisField == null) return;

            bool disableFogOfWar = OptionsHelper.LoadInt(OptionsUIConstants.DisableFogOfWarKey, 0) == 1;
            bool isFirstPerson = FirstPersonCameraController.Instance != null &&
                                 FirstPersonCameraController.Instance.IsFirstPersonMode;

            // 如果迷雾关闭或不是第一人称，重置状态
            if (disableFogOfWar || !isFirstPerson)
            {
                if (_wasAiming)
                {
                    _wasAiming = false;
                    _savedViewDistance = -1f;
                }
                return;
            }

            var controller = FirstPersonCameraController.Instance;
            bool isAiming = controller != null && controller.IsInAdsState();

            var mainVis = _mainVisField.GetValue(__instance) as FogOfWarRevealer3D;
            if (mainVis == null) return;

            if (isAiming)
            {
                if (!_wasAiming)
                {
                    // 进入开镜，保存当前 viewDistance 值（直接从字段读取）
                    _savedViewDistance = (float)_viewDistanceField.GetValue(__instance);
                    if (_savedViewDistance < 0) _savedViewDistance = 0;
                    _wasAiming = true;
                    // FPLogger.Log($"[AimZoom] 进入开镜，保存 viewDistance={_savedViewDistance}");
                }

                float mag = controller.GetCurrentMagnification();
                float multiplier = GetViewDistanceMultiplier(mag);
                float targetViewDistance = _savedViewDistance * multiplier;

                // 修改内部字段和 mainVis
                _viewDistanceField.SetValue(__instance, targetViewDistance);
                mainVis.ViewRadius = targetViewDistance;

                // FPLogger.Log($"[AimZoom] 开镜中，倍率={mag}, 乘数={multiplier}, 当前 viewDistance={targetViewDistance}");
            }
            else
            {
                if (_wasAiming)
                {
                    // 退出开镜，恢复保存的值
                    if (_savedViewDistance > 0)
                    {
                        _viewDistanceField.SetValue(__instance, _savedViewDistance);
                        mainVis.ViewRadius = _savedViewDistance;
                        // FPLogger.Log($"[AimZoom] 退出开镜，恢复 viewDistance={_savedViewDistance}");
                    }
                    _wasAiming = false;
                    _savedViewDistance = -1f;
                }
            }
        }

        private static float GetViewDistanceMultiplier(float mag)
        {
            // 可根据需要调整这些系数，让视野更远
            if (mag <= 1.2f) return 1.15f;
            if (mag <= 2f) return 1.35f;
            if (mag <= 4f) return 1.55f;
            if (mag <= 8f) return 1.8f;
            return 2f;
        }
    }
}