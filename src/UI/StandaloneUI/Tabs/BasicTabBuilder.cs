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
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_SettingsTabBasic");
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicFov", OptionsUIConstants.FovKey,// 视野fov
                OptionsUIConstants.FovMin, OptionsUIConstants.FovMax, OptionsUIConstants.FovDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicHeightOffset", OptionsUIConstants.OffsetHeightKey,//高度偏移
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetHeightDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicForwardOffset", OptionsUIConstants.OffsetForwardKey,//前向偏移
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetForwardDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicRightOffset", OptionsUIConstants.OffsetRightKey,//右向偏移    
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetRightDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicAntiBob", OptionsUIConstants.AntiBobStrengthKey,//相机防抖强度
                0f, 1f, OptionsUIConstants.AntiBobDefault, sliders);
            StandaloneUICreator.CreateDivider(parent);

            // 渲染/效果设置
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_SettingsTabDisplay");//显示设置
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicRenderDistance", OptionsUIConstants.RenderDistanceKey,//渲染距离
                OptionsUIConstants.RenderDistanceMin, OptionsUIConstants.RenderDistanceMax, OptionsUIConstants.RenderDistanceDefault, sliders);
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicDisableBlur", OptionsUIConstants.DisableBlurKey, false, toggles);//禁用模糊后处理
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicDisableSeeThrough", OptionsUIConstants.DisableSeeThroughKey, false, toggles);//禁用遮挡透视
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicDisableAimOcclusion", OptionsUIConstants.DisableAimOcclusionFadeKey, false, toggles);//禁用瞄准虚化
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicDisableFogOfWar", OptionsUIConstants.DisableFogOfWarKey, false, toggles);//禁用战争迷雾
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicShowSoundVisualization", OptionsUIConstants.ShowSoundVisualizationKey, false, toggles);//第一人称显示声音纹路
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicDisableMuzzleFlash", OptionsUIConstants.DisableMuzzleFlashKey, false, toggles);//禁用枪口火光
            StandaloneUICreator.CreateDivider(parent);

            // 战斗设置
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_SettingsTabCombat");//战斗设置
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicEnableSpread", OptionsUIConstants.EnableSpreadKey, false, toggles);//瞄准时无散布
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicEnableRecoil", OptionsUIConstants.EnableRecoilKey, true, toggles);//启用后坐力
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicRecoilStrength", OptionsUIConstants.RecoilStrengthKey,//后坐力强度
                OptionsUIConstants.RecoilStrengthMin, OptionsUIConstants.RecoilStrengthMax, OptionsUIConstants.RecoilStrengthDefault, sliders);
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicEnableAdsOffset", OptionsUIConstants.EnableAdsOffsetKey, true, toggles);//启用ADS武器位移
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicEnableJump", OptionsUIConstants.EnableJumpKey, false, toggles);//启用跳跃功能
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicAllowDashDuringJump", OptionsUIConstants.AllowDashDuringJumpKey, true, toggles);//跳跃途中允许翻滚"
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicAllowInspectWhileRunning", OptionsUIConstants.AllowInspectWhileRunningKey, false, toggles);//允许奔跑时检视
            StandaloneUICreator.CreateDivider(parent);

            // 受击特效
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_BasicHitFx");//受击特效
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicHitFxEnable", OptionsUIConstants.HitFxEnableKey, true, toggles);//受击效果开关
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicHitFxPost", OptionsUIConstants.HitFxPostStrengthKey,//受击后处理强度
                0f, 1f, OptionsUIConstants.HitFxPostDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicHitFxUi", OptionsUIConstants.HitFxUiStrengthKey,//受击UI强度
                0f, 1f, OptionsUIConstants.HitFxUiDefault, sliders);
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicHitFxDir", OptionsUIConstants.HitFxShowDirKey, true, toggles);//受击方向指示
            StandaloneUICreator.CreateDivider(parent);

            // 狙击枪增强
            StandaloneUICreator.CreateSectionTitle(parent, "FPC_BasicSniperEnhance");//狙击枪增强
            StandaloneUICreator.CreateToggleRow(parent, "FPC_BasicSniperEnhanceEnable", OptionsUIConstants.SniperEnhanceEnabledKey, false, toggles);//启用狙击枪增强
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicSniperDistanceMultiplier", OptionsUIConstants.SniperDistanceMultiplierKey,//射程倍率
                OptionsUIConstants.SniperMultiplierMin, OptionsUIConstants.SniperMultiplierMax,
                OptionsUIConstants.SniperDistanceMultiplierDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicSniperBulletSpeedMultiplier", OptionsUIConstants.SniperBulletSpeedMultiplierKey,//子弹速度倍率
                OptionsUIConstants.SniperMultiplierMin, OptionsUIConstants.SniperMultiplierMax,
                OptionsUIConstants.SniperBulletSpeedMultiplierDefault, sliders);
            StandaloneUICreator.CreateSliderRow(parent, "FPC_BasicSniperADSTimeMultiplier", OptionsUIConstants.SniperADSTimeMultiplierKey,//开镜时间倍率
                OptionsUIConstants.SniperADSTimeMultiplierMin, OptionsUIConstants.SniperADSTimeMultiplierMax,
                OptionsUIConstants.SniperADSTimeMultiplierDefault, sliders);
            StandaloneUICreator.CreateDivider(parent);
        }
    }
}