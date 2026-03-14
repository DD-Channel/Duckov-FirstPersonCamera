using HarmonyLib;
using Duckov.Sounds;
using UnityEngine;
using System.Reflection;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Patches
{
    [HarmonyPatch(typeof(SoundVisualization))]
    [HarmonyPatch("RefreshEntryPosition")]
    public static class SoundVisualization_RefreshEntryPosition_Patch
    {
        private static FieldInfo _displayOffsetField;
        private static Canvas _canvasCache;

        static SoundVisualization_RefreshEntryPosition_Patch()
        {
            _displayOffsetField = AccessTools.Field(typeof(SoundVisualization), "displayOffset");
        }

        static bool Prefix(SoundVisualization __instance, SoundDisplay e)
        {
            // 检查选项是否开启，如果关闭则让原方法执行（头顶显示）
            bool show = OptionsHelper.LoadInt(OptionsUIConstants.ShowSoundVisualizationKey, 1) == 1;
            if (!show) return true;

            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode)
                return true;

            Camera cam = controller.MainCamera;
            if (cam == null) return true;

            float displayOffset = _displayOffsetField != null ? (float)_displayOffsetField.GetValue(__instance) : 400f;

            // 确保箭头父级为 Canvas 根，避免裁剪
            if (_canvasCache == null)
                _canvasCache = __instance.GetComponentInParent<Canvas>();
            if (_canvasCache != null && e.transform.parent != _canvasCache.transform)
            {
                e.transform.SetParent(_canvasCache.transform, true);
            }

            Vector3 worldPos = e.CurrentSount.pos;
            Vector3 viewPos = cam.WorldToViewportPoint(worldPos);
            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 direction;

            if (viewPos.z < 0) // 后方
            {
                direction = screenCenter - (Vector2)viewPos;
            }
            else
            {
                direction = (Vector2)viewPos - screenCenter;
            }

            if (direction.sqrMagnitude > 0.0001f)
                direction.Normalize();
            else
                direction = Vector2.up;

            e.transform.localPosition = direction * displayOffset;
            e.transform.rotation = Quaternion.FromToRotation(Vector2.up, direction);

            return false; // 跳过原方法
        }
    }
}