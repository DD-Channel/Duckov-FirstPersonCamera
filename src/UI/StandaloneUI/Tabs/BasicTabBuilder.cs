using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPersonCamera.StandaloneUI.Tabs
{
    internal static class BasicTabBuilder
    {
        public static void Build(
            Transform parent,
            Dictionary<string, Slider> sliders,
            Dictionary<string, Toggle> toggles,
            Dictionary<string, TMP_Dropdown> dropdowns,
            Dictionary<string, Button> keybindButtons)
        {
            // 相机设置
            StandaloneUICreator.CreateSectionTitle(parent, "📷 相机设置");
            StandaloneUICreator.CreateSliderRow(parent, "视野角度 (FOV)", OptionsUIConstants.FovKey,
                OptionsUIConstants.FovMin, OptionsUIConstants.FovMax, OptionsUIConstants.FovDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "高度偏移", OptionsUIConstants.OffsetHeightKey,
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetHeightDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "前向偏移", OptionsUIConstants.OffsetForwardKey,
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetForwardDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "右向偏移", OptionsUIConstants.OffsetRightKey,
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetRightDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "相机防抖强度", OptionsUIConstants.AntiBobStrengthKey,
                0f, 1f, OptionsUIConstants.AntiBobDefault, sliders);
            StandaloneUICreator.CreateDivider(parent);

            // 渲染/效果设置
            StandaloneUICreator.CreateSectionTitle(parent, "🎨 视觉与效果");
            StandaloneUICreator.CreateSliderRow(parent, "渲染距离 (m)", OptionsUIConstants.RenderDistanceKey,
                OptionsUIConstants.RenderDistanceMin, OptionsUIConstants.RenderDistanceMax, OptionsUIConstants.RenderDistanceDefault, sliders);
            StandaloneUICreator.CreateToggleRow(parent, "禁用模糊后处理", OptionsUIConstants.DisableBlurKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "禁用遮挡透视", OptionsUIConstants.DisableSeeThroughKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "禁用瞄准虚化", OptionsUIConstants.DisableAimOcclusionFadeKey, false, toggles);
            StandaloneUICreator.CreateDivider(parent);

            // 战斗设置（基础归类）
            StandaloneUICreator.CreateSectionTitle(parent, "⚔️ 战斗设置");
            StandaloneUICreator.CreateToggleRow(parent, "启用后坐力", OptionsUIConstants.EnableRecoilKey, true, toggles);
            StandaloneUICreator.CreateSliderRow(parent, "后坐力强度", OptionsUIConstants.RecoilStrengthKey,
                OptionsUIConstants.RecoilStrengthMin, OptionsUIConstants.RecoilStrengthMax, OptionsUIConstants.RecoilStrengthDefault, sliders);
            StandaloneUICreator.CreateToggleRow(parent, "启用ADS武器位移", OptionsUIConstants.EnableAdsOffsetKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "启用跳跃功能", OptionsUIConstants.EnableJumpKey, false, toggles);
            StandaloneUICreator.CreateDivider(parent);

            // 受击特效（归于基础视觉）
            StandaloneUICreator.CreateSectionTitle(parent, "💥 受击特效");
            StandaloneUICreator.CreateToggleRow(parent, "受击效果开关", OptionsUIConstants.HitFxEnableKey, true, toggles);
            StandaloneUICreator.CreateSliderRow(parent, "受击后处理强度", OptionsUIConstants.HitFxPostStrengthKey,
                0f, 1f, OptionsUIConstants.HitFxPostDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "受击UI强度", OptionsUIConstants.HitFxUiStrengthKey,
                0f, 1f, OptionsUIConstants.HitFxUiDefault, sliders);
            StandaloneUICreator.CreateToggleRow(parent, "受击方向指示", OptionsUIConstants.HitFxShowDirKey, true, toggles);
        }
    }
}
