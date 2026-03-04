using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstPersonCamera.OptionsUI;

namespace FirstPersonCamera.StandaloneUI.Tabs
{
    internal static class DisplayTabBuilder
    {
        public static void Build(
            Transform parent,
            Dictionary<string, Slider> sliders,
            Dictionary<string, Toggle> toggles,
            Dictionary<string, TMP_Dropdown> dropdowns,
            Dictionary<string, Button> keybindButtons)
        {
            // 装备隐藏
            StandaloneUICreator.CreateSectionTitle(parent, "🎩 装备隐藏");
            StandaloneUICreator.CreateToggleRow(parent, "隐藏头盔", OptionsUIConstants.HideHelmetKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "隐藏口罩/面罩", OptionsUIConstants.HideFaceMaskKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "隐藏护甲", OptionsUIConstants.HideArmorKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "隐藏背包", OptionsUIConstants.HideBackpackKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "隐藏近战武器", OptionsUIConstants.HideMeleeKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(parent, "隐藏耳机/耳饰", OptionsUIConstants.HideHeadsetKey, false, toggles);
            StandaloneUICreator.CreateDivider(parent);

            // 身体部位隐藏
            StandaloneUICreator.CreateSectionTitle(parent, "👤 身体部位");
            StandaloneUICreator.CreateToggleRow(parent, "身体显示隐藏", OptionsUIConstants.HideRightHandKey, true, toggles);
            StandaloneUICreator.CreateDivider(parent);

            // 武器显示
            StandaloneUICreator.CreateSectionTitle(parent, "🔫 武器显示");
            StandaloneUICreator.CreateToggleRow(parent, "强制显示持握武器", OptionsUIConstants.ForceShowHeldWeaponKey, true, toggles);
        }
    }
}

