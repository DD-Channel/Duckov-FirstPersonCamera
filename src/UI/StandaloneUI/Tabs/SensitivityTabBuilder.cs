using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPersonCamera.StandaloneUI.Tabs
{
    internal static class SensitivityTabBuilder
    {
        public static void Build(
            Transform parent,
            Dictionary<string, Slider> sliders,
            Dictionary<string, Toggle> toggles,
            Dictionary<string, TMP_Dropdown> dropdowns,
            Dictionary<string, Button> keybindButtons)
        {
            // 鼠标灵敏度
            StandaloneUICreator.CreateSectionTitle(parent, "🎯 鼠标灵敏度");
            StandaloneUICreator.CreateSliderRow(parent, "水平灵敏度", OptionsUIConstants.SensitivityXKey,
                OptionsUIConstants.SensitivityMin, OptionsUIConstants.SensitivityMax, OptionsUIConstants.SensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "垂直灵敏度", OptionsUIConstants.SensitivityYKey,
                OptionsUIConstants.SensitivityMin, OptionsUIConstants.SensitivityMax, OptionsUIConstants.SensitivityDefault, sliders);
            
            // 倍镜灵敏度倍数（相对于普通灵敏度，1.0=100%，0.8=80%，同时应用于水平和垂直）
            StandaloneUICreator.CreateSectionTitle(parent, "🔍 倍镜灵敏度倍数");
            StandaloneUICreator.CreateSliderRow(parent, "1.2x倍镜倍数", OptionsUIConstants.Scope1_2xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope1_2xSensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "2x倍镜倍数", OptionsUIConstants.Scope2xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope2xSensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "4x倍镜倍数", OptionsUIConstants.Scope4xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope4xSensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "8x倍镜倍数", OptionsUIConstants.Scope8xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope8xSensitivityDefault, sliders);
        }
    }
}

