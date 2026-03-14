using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;

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
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_SettingsTabKeybinds");//按键设置
            StandaloneUICreator.CreateKeybindRow(parent, "FPC_ToggleKey", "FirstPersonCamera_ToggleKeyCode", KeyCode.F5, keybindButtons);//切换第一人称
            StandaloneUICreator.CreateToggleRow(parent, "FPC_PeekMode", OptionsUIConstants.PeekModeKey, false, toggles); //长按探头模式
            StandaloneUICreator.CreateKeybindRow(parent, "FPC_PeekLeft", "FirstPersonCamera_PeekLeftKeyCode", KeyCode.Q, keybindButtons);//向左探头
            StandaloneUICreator.CreateKeybindRow(parent, "FPC_PeekRight", "FirstPersonCamera_PeekRightKeyCode", KeyCode.E, keybindButtons); //向右探头
            StandaloneUICreator.CreateKeybindRow(parent, "FPC_LaserToggle", OptionsUIConstants.LaserToggleKeyCodeKey, KeyCode.None, keybindButtons);//激光开关
            StandaloneUICreator.CreateKeybindRow(parent, "FPC_Inspect", OptionsUIConstants.InspectKeyCodeKey, KeyCode.H, keybindButtons);//检视
        }
    }
}