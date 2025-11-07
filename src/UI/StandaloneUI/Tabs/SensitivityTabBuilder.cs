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
        }
    }
}

