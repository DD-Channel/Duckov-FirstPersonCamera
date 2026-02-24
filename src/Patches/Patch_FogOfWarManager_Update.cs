using HarmonyLib;
using System.Reflection;
using FirstPersonCamera.Utilities;
using FirstPersonCamera;
using FOW;
using UnityEngine;

namespace FirstPersonCamera.Patches
{
    /// <summary>
    /// Harmony 补丁：动态干预 FogOfWarManager.Update，根据 Mod 选项和第一人称状态控制战争迷雾。
    /// 修复：骑乘坐骑时迷雾跟随视角移动（位置和方向）。
    /// </summary>
    [HarmonyPatch(typeof(FogOfWarManager))]
    [HarmonyPatch("Update")]
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

            bool shouldEnable = isFirstPerson && disableFogOfWar;
            bool currentAllVision = (bool)_allVisionField.GetValue(__instance);

            if (shouldEnable)
            {
                if (!currentAllVision)
                    _allVisionField.SetValue(__instance, true);
            }
            else if (isFirstPerson)
            {
                bool targetAllVision = !ShouldHaveFog(__instance);
                if (currentAllVision != targetAllVision)
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
                // 选项关闭时强制设置较小的视野范围（让迷雾快速收缩）
                if (!disableFogOfWar)
                {
                    mainVis.UnobscuredRadius = 1.5f;
                    mainVis.ViewRadius = 30f;
                }

                // 强制将迷雾揭示者的位置和旋转设置为当前相机，确保视野跟随视角
                if (GameCamera.Instance != null && GameCamera.Instance.renderCamera != null)
                {
                    Transform camTransform = GameCamera.Instance.renderCamera.transform;
                    mainVis.transform.position = camTransform.position;
                    mainVis.transform.rotation = camTransform.rotation;
                }
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