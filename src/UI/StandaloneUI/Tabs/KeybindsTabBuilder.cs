using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstPersonCamera.OptionsUI;

namespace FirstPersonCamera.StandaloneUI.Tabs
{
    internal static class KeybindsTabBuilder
    {
        public static void Build(
            Transform parent,
            Dictionary<string, Slider> sliders,
            Dictionary<string, Toggle> toggles,
            Dictionary<string, TMP_Dropdown> dropdowns,
            Dictionary<string, Button> keybindButtons)
        {
            // 键位设置
            StandaloneUICreator.CreateSectionTitle(parent, "⌨️ 键位设置");

            // 切换第一人称键
            StandaloneUICreator.CreateKeybindRow(parent, "切换第一人称", "FirstPersonCamera_ToggleKeyCode", KeyCode.F5, keybindButtons);
            // peek模式切换
            StandaloneUICreator.CreateToggleRow(parent, "探头按键切换模式", OptionsUIConstants.PeekModeKey, false, toggles);
            // 偏头键（左/右）
            StandaloneUICreator.CreateKeybindRow(parent, "左侧探头", "FirstPersonCamera_PeekLeftKeyCode", KeyCode.Q, keybindButtons);
            StandaloneUICreator.CreateKeybindRow(parent, "右侧探头", "FirstPersonCamera_PeekRightKeyCode", KeyCode.E, keybindButtons);
            
            // 激光开关键
            StandaloneUICreator.CreateKeybindRow(parent, "激光开关", OptionsUIConstants.LaserToggleKeyCodeKey, KeyCode.None, keybindButtons);
            
            // 统一检视键（枪械和近战共用）
            StandaloneUICreator.CreateKeybindRow(parent, "检视", OptionsUIConstants.InspectKeyCodeKey, KeyCode.H, keybindButtons);
           
        }
    }
}

