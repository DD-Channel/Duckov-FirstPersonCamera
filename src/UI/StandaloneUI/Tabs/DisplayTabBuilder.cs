using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstPersonCamera.Utilities;
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
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_DisplayEquipment");//装备隐藏
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideHelmet", OptionsUIConstants.HideHelmetKey, true, toggles);//隐藏头盔
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideFaceMask", OptionsUIConstants.HideFaceMaskKey, true, toggles);//隐藏口罩/面罩
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideArmor", OptionsUIConstants.HideArmorKey, false, toggles);//隐藏护甲
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideBackpack", OptionsUIConstants.HideBackpackKey, false, toggles);//隐藏背包
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideMelee", OptionsUIConstants.HideMeleeKey, false, toggles);//隐藏近战武器
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideHeadset", OptionsUIConstants.HideHeadsetKey, false, toggles);//隐藏耳机/耳饰
            StandaloneUICreator.CreateDivider(parent);

            // 身体部位
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_DisplayBody");//身体部位
            StandaloneUICreator.CreateToggleRow(parent, "FPC_HideBody", OptionsUIConstants.HideRightHandKey, true, toggles);//身体显示隐藏
            StandaloneUICreator.CreateDivider(parent);

            // 武器显示
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_DisplayWeapon");
            StandaloneUICreator.CreateToggleRow(parent, "FPC_ForceShowWeapon", OptionsUIConstants.ForceShowHeldWeaponKey, true, toggles);//强制显示持握武器
        }
    }
}