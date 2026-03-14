using HarmonyLib;
using System.Reflection;
using FirstPersonCamera.Utilities;
using FirstPersonCamera;
using FOW;
using UnityEngine;

namespace FirstPersonCamera.Patches
{
    /// <summary>
    /// Harmony 补丁：强制控制战争迷雾。
    /// 开关开启时，设置 allVision = true 且半径极大值，确保全图可见。
    /// 开关关闭时，恢复游戏规则控制 allVision，不干预半径（由游戏动态更新）。
    /// </summary>
    [HarmonyPatch(typeof(FogOfWarManager))]
    [HarmonyPatch("Update")]
    [HarmonyPriority(600)] // 高优先级，确保在其他补丁后执行
    public static class Patch_FogOfWarManager_Update
    {
        private static FieldInfo _allVisionField;
        private static FieldInfo _mainVisField;
        private static bool _fieldsInitialized;

        private static void InitializeFields()
        {
            if (_fieldsInitialized) return;
            var type = typeof(FogOfWarManager);
            _allVisionField = type.GetField("allVision", BindingFlags.NonPublic | BindingFlags.Instance);
            _mainVisField = type.GetField("mainVis", BindingFlags.Public | BindingFlags.Instance);
            _fieldsInitialized = true;
        }

        public static void Prefix(FogOfWarManager __instance)
        {
            InitializeFields();
            if (_allVisionField == null) return;

            bool disableFogOfWar = OptionsHelper.LoadInt(OptionsUIConstants.DisableFogOfWarKey, 0) == 1;
            bool isFirstPerson = FirstPersonCameraController.Instance != null &&
                                 FirstPersonCameraController.Instance.IsFirstPersonMode;

            bool targetAllVision;
            if (isFirstPerson && disableFogOfWar)
            {
                targetAllVision = true;
            }
            else if (isFirstPerson)
            {
                targetAllVision = !ShouldHaveFog(__instance);
            }
            else
            {
                return;
            }

            bool currentAllVision = (bool)_allVisionField.GetValue(__instance);
            if (currentAllVision != targetAllVision)
            {
                _allVisionField.SetValue(__instance, targetAllVision);
            }
        }

        public static void Postfix(FogOfWarManager __instance)
        {
            InitializeFields();
            if (_mainVisField == null) return;

            bool disableFogOfWar = OptionsHelper.LoadInt(OptionsUIConstants.DisableFogOfWarKey, 0) == 1;
            bool isFirstPerson = FirstPersonCameraController.Instance != null &&
                                 FirstPersonCameraController.Instance.IsFirstPersonMode;

            var mainVis = _mainVisField.GetValue(__instance) as FogOfWarRevealer3D;
            if (mainVis == null) return;

            if (isFirstPerson)
            {
                // 强制将迷雾揭示者的位置和旋转设置为当前相机，确保视野跟随视角
                if (GameCamera.Instance != null && GameCamera.Instance.renderCamera != null)
                {
                    Transform camTransform = GameCamera.Instance.renderCamera.transform;
                    mainVis.transform.position = camTransform.position;
                    mainVis.transform.rotation = camTransform.rotation;
                }

                if (disableFogOfWar)
                {
                    // 开关开启：强制半径极大值
                    const float hugeRadius = 10000f;
                    if (Mathf.Abs(mainVis.UnobscuredRadius - hugeRadius) > 0.1f)
                    {
                        mainVis.UnobscuredRadius = hugeRadius;
                        mainVis.ViewRadius = hugeRadius;
                    }
                }
                // 开关关闭时不干预半径，由游戏自身动态更新
            }
        }

        /// <summary>
        /// 判断当前地图是否应该拥有迷雾（根据游戏规则）
        /// </summary>
        private static bool ShouldHaveFog(FogOfWarManager instance)
        {
            try
            {
                if (LevelManager.Instance == null || LevelManager.Rule == null)
                    return true;
                return LevelManager.Instance.IsRaidMap && LevelManager.Rule.FogOfWar;
            }
            catch
            {
                return true;
            }
        }
    }
}