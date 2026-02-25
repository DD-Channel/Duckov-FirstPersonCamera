namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称选项UI常量定义
    /// 包含所有选项键、默认值、范围等常量
    /// </summary>
    public static class OptionsUIConstants
    {
        #region 版本信息
        /// <summary>
        /// 模组版本号
        /// 更新版本时只需修改此处
        /// </summary>
        public const string ModVersion = "1.2.0";
        #endregion

        #region 灵敏度选项键
        /// <summary>
        /// 选项键：鼠标灵敏度（旧版，用于向后兼容）
        /// </summary>
        public const string SensitivityLegacyKey = "FirstPersonCamera_Sensitivity";
        
        /// <summary>
        /// 选项键：鼠标X轴灵敏度
        /// </summary>
        public const string SensitivityXKey = "FirstPersonCamera_SensitivityX";
        
        /// <summary>
        /// 选项键：鼠标Y轴灵敏度
        /// </summary>
        public const string SensitivityYKey = "FirstPersonCamera_SensitivityY";
        
        /// <summary>
        /// 灵敏度最小值
        /// </summary>
        public const float SensitivityMin = 0.01f;
        
        /// <summary>
        /// 灵敏度最大值
        /// </summary>
        public const float SensitivityMax = 1f;
        
        /// <summary>
        /// 倍镜灵敏度倍数最小值（相对于普通灵敏度）
        /// </summary>
        public const float ScopeSensitivityMultiplierMin = 0.01f;
        
        /// <summary>
        /// 倍镜灵敏度倍数最大值（相对于普通灵敏度，2表示2倍普通灵敏度）
        /// </summary>
        public const float ScopeSensitivityMultiplierMax = 2f;
        
        /// <summary>
        /// 灵敏度默认值
        /// </summary>
        public const float SensitivityDefault = 0.10f;
        
        /// <summary>
        /// 选项键：1.2x倍镜灵敏度倍数（同时应用于水平和垂直）
        /// </summary>
        public const string Scope1_2xSensitivityKey = "FirstPersonCamera_Scope1_2xSensitivity";
        
        /// <summary>
        /// 选项键：2x倍镜灵敏度倍数（同时应用于水平和垂直）
        /// </summary>
        public const string Scope2xSensitivityKey = "FirstPersonCamera_Scope2xSensitivity";
        
        /// <summary>
        /// 选项键：4x倍镜灵敏度倍数（同时应用于水平和垂直）
        /// </summary>
        public const string Scope4xSensitivityKey = "FirstPersonCamera_Scope4xSensitivity";
        
        /// <summary>
        /// 选项键：8x倍镜灵敏度倍数（同时应用于水平和垂直）
        /// </summary>
        public const string Scope8xSensitivityKey = "FirstPersonCamera_Scope8xSensitivity";
        
        /// <summary>
        /// 倍镜灵敏度默认值（相对于普通灵敏度的倍数，基准为1）
        /// 例如：0.8表示开镜时灵敏度是普通灵敏度的80%
        /// </summary>
        public const float Scope1_2xSensitivityDefault = 0.8f;
        public const float Scope2xSensitivityDefault = 0.6f;
        public const float Scope4xSensitivityDefault = 0.4f;
        public const float Scope8xSensitivityDefault = 0.2f;
        #endregion

        #region 相机偏移选项键
        /// <summary>
        /// 选项键：相机高度偏移
        /// </summary>
        public const string OffsetHeightKey = "FirstPersonCamera_OffsetHeight";
        
        /// <summary>
        /// 选项键：相机前向偏移
        /// </summary>
        public const string OffsetForwardKey = "FirstPersonCamera_OffsetForward";
        
        /// <summary>
        /// 选项键：相机右向偏移
        /// </summary>
        public const string OffsetRightKey = "FirstPersonCamera_OffsetRight";
        
        /// <summary>
        /// 相机偏移最小值
        /// </summary>
        public const float OffsetMin = -0.5f;
        
        /// <summary>
        /// 相机偏移最大值
        /// </summary>
        public const float OffsetMax = 0.5f;
        
        /// <summary>
        /// 相机高度偏移默认值
        /// </summary>
        public const float OffsetHeightDefault = 0.15f;
        
        /// <summary>
        /// 相机前向偏移默认值
        /// </summary>
        public const float OffsetForwardDefault = 0.12f;
        
        /// <summary>
        /// 相机右向偏移默认值
        /// </summary>
        public const float OffsetRightDefault = 0.0f;
        #endregion

        #region 渲染距离选项键
        /// <summary>
        /// 选项键：渲染距离
        /// </summary>
        public const string RenderDistanceKey = "FirstPersonCamera_RenderDistance";
        
        /// <summary>
        /// 渲染距离最小值（米）
        /// </summary>
        public const float RenderDistanceMin = 30f;
        
        /// <summary>
        /// 渲染距离最大值（米）
        /// </summary>
        public const float RenderDistanceMax = 200f;
        
        /// <summary>
        /// 渲染距离默认值（米）
        /// </summary>
        public const float RenderDistanceDefault = 70f;
        #endregion

        #region 防抖强度选项键
        /// <summary>
        /// 选项键：防抖强度（0.0-1.0）
        /// </summary>
        public const string AntiBobStrengthKey = "FirstPersonCamera_AntiBobStrength";
        
        /// <summary>
        /// 防抖强度最小值
        /// </summary>
        public const float AntiBobMin = 0f;
        
        /// <summary>
        /// 防抖强度最大值
        /// </summary>
        public const float AntiBobMax = 1f;
        
        /// <summary>
        /// 防抖强度默认值
        /// </summary>
        public const float AntiBobDefault = 0f;
        #endregion

        #region 视野角度选项键
        /// <summary>
        /// 选项键：视野角度（FOV）
        /// </summary>
        public const string FovKey = "FirstPersonCamera_FOV";
        
        /// <summary>
        /// 视野角度最小值
        /// </summary>
        public const float FovMin = 60f;
        
        /// <summary>
        /// 视野角度最大值
        /// </summary>
        public const float FovMax = 100f;
        
        /// <summary>
        /// 视野角度默认值
        /// </summary>
        public const float FovDefault = 70f;
        #endregion

        #region 受击特效选项键
        /// <summary>
        /// 选项键：受击特效开关
        /// </summary>
        public const string HitFxEnableKey = "FirstPersonCamera_HitFx_Enable";
        
        /// <summary>
        /// 选项键：受击后处理强度
        /// </summary>
        public const string HitFxPostStrengthKey = "FirstPersonCamera_HitFx_PostStrength";
        
        /// <summary>
        /// 选项键：受击UI强度
        /// </summary>
        public const string HitFxUiStrengthKey = "FirstPersonCamera_HitFx_UiStrength";
        
        /// <summary>
        /// 选项键：受击方向指示
        /// </summary>
        public const string HitFxShowDirKey = "FirstPersonCamera_HitFx_ShowDirection";
        #endregion

        #region 功能开关选项键
        /// <summary>
        /// 选项键：启用后坐力
        /// </summary>
        public const string EnableRecoilKey = "FirstPersonCamera_EnableRecoil";
        
        /// <summary>
        /// 选项键：启用ADS武器位移
        /// </summary>
        public const string EnableAdsOffsetKey = "FirstPersonCamera_EnableAdsOffset";
        
        /// <summary>
        /// 选项键：禁用模糊（后处理）
        /// </summary>
        public const string DisableBlurKey = "FirstPersonCamera_DisableBlur";
        
        /// <summary>
        /// 选项键：禁用准星虚化
        /// </summary>
        public const string DisableAimOcclusionFadeKey = "FirstPersonCamera_DisableAimOcclusionFade";
        
        /// <summary>
        /// 选项键：禁用遮挡透视
        /// </summary>
        public const string DisableSeeThroughKey = "FirstPersonCamera_DisableSeeThrough";
        
        /// <summary>
        /// 选项键：第一人称下去除战争迷雾
        /// </summary>
        public const string DisableFogOfWarKey = "FirstPersonCamera_DisableFogOfWar";
        
        /// <summary>
        /// 选项键：后坐力强度
        /// </summary>
        public const string RecoilStrengthKey = "FirstPersonCamera_RecoilStrength";
        
        /// <summary>
        /// 后坐力强度最小值
        /// </summary>
        public const float RecoilStrengthMin = 0f;
        
        /// <summary>
        /// 后坐力强度最大值
        /// </summary>
        public const float RecoilStrengthMax = 0.25f;
        
        /// <summary>
        /// 后坐力强度默认值
        /// </summary>
        public const float RecoilStrengthDefault = 0.1f;
        
        /// <summary>
        /// 选项键：激光开关快捷键
        /// </summary>
        public const string LaserToggleKeyCodeKey = "FirstPersonCamera_LaserToggleKeyCode";
        
        /// <summary>
        /// 选项键：武器检视快捷键（已废弃，使用InspectKeyCodeKey）
        /// </summary>
        public const string WeaponInspectKeyCodeKey = "FirstPersonCamera_WeaponInspectKeyCode";
        
        /// <summary>
        /// 选项键：统一检视快捷键（枪械和近战共用）
        /// </summary>
        public const string InspectKeyCodeKey = "FirstPersonCamera_InspectKeyCode";
        
        /// <summary>
        /// 选项键：启用跳跃功能
        /// </summary>
        public const string EnableJumpKey = "FirstPersonCamera_EnableJump";
        
        /// <summary>
        /// 选项键：跳跃途中允许翻滚
        /// </summary>
        public const string AllowDashDuringJumpKey = "FirstPersonCamera_AllowDashDuringJump";
        
        /// <summary>
        /// 选项键：允许奔跑时检视武器
        /// </summary>
        public const string AllowInspectWhileRunningKey = "FirstPersonCamera_AllowInspectWhileRunning";
        
        /// <summary>
        /// 选项键：UI检视时间（秒）
        /// </summary>
        public const string UIInspectDurationKey = "FirstPersonCamera_UIInspectDuration";
        
        /// <summary>
        /// 选项键：UI检视大小（UI图标大小倍数）
        /// </summary>
        public const string UIInspectSizeKey = "FirstPersonCamera_UIInspectSize";
        
        /// <summary>
        /// 选项键：3D模型检视距离（相机到模型的距离，米）
        /// </summary>
        public const string ModelInspectDistanceKey = "FirstPersonCamera_ModelInspectDistance";
        
        /// <summary>
        /// UI检视时间最小值（秒）
        /// </summary>
        public const float UIInspectDurationMin = 0.5f;
        
        /// <summary>
        /// UI检视时间最大值（秒）
        /// </summary>
        public const float UIInspectDurationMax = 10f;
        
        /// <summary>
        /// UI检视时间默认值（秒）
        /// </summary>
        public const float UIInspectDurationDefault = 3f;
        
        /// <summary>
        /// UI检视大小最小值（倍数）
        /// </summary>
        public const float UIInspectSizeMin = 0.1f;
        
        /// <summary>
        /// UI检视大小最大值（倍数）
        /// </summary>
        public const float UIInspectSizeMax = 3f;
        
        /// <summary>
        /// UI检视大小默认值（倍数）
        /// </summary>
        public const float UIInspectSizeDefault = 1f;
        
        /// <summary>
        /// 3D模型检视距离最小值（米）
        /// </summary>
        public const float ModelInspectDistanceMin = 0.3f;
        
        /// <summary>
        /// 3D模型检视距离最大值（米）
        /// </summary>
        public const float ModelInspectDistanceMax = 2f;
        
        /// <summary>
        /// 3D模型检视距离默认值（米）
        /// </summary>
        public const float ModelInspectDistanceDefault = 0.6f;
        #endregion

        #region 部位隐藏选项键
        /// <summary>
        /// 选项键：隐藏头盔
        /// </summary>
        public const string HideHelmetKey = "FirstPersonCamera_HideHelmet";
        
        /// <summary>
        /// 选项键：隐藏面罩
        /// </summary>
        public const string HideFaceMaskKey = "FirstPersonCamera_HideFaceMask";
        
        /// <summary>
        /// 选项键：隐藏护甲
        /// </summary>
        public const string HideArmorKey = "FirstPersonCamera_HideArmor";
        
        /// <summary>
        /// 选项键：隐藏脸部
        /// </summary>
        public const string HideFaceKey = "FirstPersonCamera_HideFace";
        
        /// <summary>
        /// 选项键：隐藏头发
        /// </summary>
        public const string HideHairKey = "FirstPersonCamera_HideHair";
        
        /// <summary>
        /// 选项键：隐藏背包
        /// </summary>
        public const string HideBackpackKey = "FirstPersonCamera_HideBackpack";
        
        /// <summary>
        /// 选项键：隐藏近战武器
        /// </summary>
        public const string HideMeleeKey = "FirstPersonCamera_HideMelee";
        
        /// <summary>
        /// 选项键：隐藏耳机
        /// </summary>
        public const string HideHeadsetKey = "FirstPersonCamera_HideHeadset";
        
        /// <summary>
        /// 选项键：隐藏左手
        /// </summary>
        public const string HideLeftHandKey = "FirstPersonCamera_HideLeftHand";
        
        /// <summary>
        /// 选项键：隐藏右手
        /// </summary>
        public const string HideRightHandKey = "FirstPersonCamera_HideRightHand";
        
        /// <summary>
        /// 选项键：隐藏眼睛部件
        /// </summary>
        public const string HideFaceEyesKey = "FirstPersonCamera_HideFaceEyes";
        
        /// <summary>
        /// 选项键：隐藏眉毛部件
        /// </summary>
        public const string HideFaceEyebrowsKey = "FirstPersonCamera_HideFaceEyebrows";
        
        /// <summary>
        /// 选项键：隐藏嘴部部件
        /// </summary>
        public const string HideFaceMouthKey = "FirstPersonCamera_HideFaceMouth";
        
        /// <summary>
        /// 选项键：隐藏尾巴部件
        /// </summary>
        public const string HideFaceTailKey = "FirstPersonCamera_HideFaceTail";
        
        /// <summary>
        /// 选项键：隐藏脚部部件
        /// </summary>
        public const string HideFaceFeetKey = "FirstPersonCamera_HideFaceFeet";
        
        /// <summary>
        /// 选项键：隐藏翅膀部件
        /// </summary>
        public const string HideFaceWingsKey = "FirstPersonCamera_HideFaceWings";
        
        /// <summary>
        /// 选项键：强制显示持握武器
        /// </summary>
        public const string ForceShowHeldWeaponKey = "FirstPersonCamera_ForceShowHeldWeapon";
        #endregion

        #region UI文本标签
        /// <summary>
        /// 标签文本：第一人称设置
        /// </summary>
        public const string TabTitleFirstPerson = "第一人称设置";
        
        /// <summary>
        /// 标签文本：FP人物显示设置
        /// </summary>
        public const string TabTitleDisplay = "FP人物显示设置";
        
        /// <summary>
        /// 标签文本：水平灵敏度
        /// </summary>
        public const string LabelSensitivityX = "水平灵敏度";
        
        /// <summary>
        /// 标签文本：垂直灵敏度
        /// </summary>
        public const string LabelSensitivityY = "垂直灵敏度";
        
        /// <summary>
        /// 标签文本：后坐力强度
        /// </summary>
        public const string LabelRecoilStrength = "后坐力强度";
        
        /// <summary>
        /// 标签文本：高度偏移
        /// </summary>
        public const string LabelOffsetHeight = "高度偏移";
        
        /// <summary>
        /// 标签文本：前向偏移
        /// </summary>
        public const string LabelOffsetForward = "前向偏移";
        
        /// <summary>
        /// 标签文本：右向偏移
        /// </summary>
        public const string LabelOffsetRight = "右向偏移";
        
        /// <summary>
        /// 标签文本：上下偏移（同步时使用）
        /// </summary>
        public const string LabelOffsetHeightSync = "上下偏移";
        
        /// <summary>
        /// 标签文本：前后偏移（同步时使用）
        /// </summary>
        public const string LabelOffsetForwardSync = "前后偏移";
        
        /// <summary>
        /// 标签文本：左右偏移（同步时使用）
        /// </summary>
        public const string LabelOffsetRightSync = "左右偏移";
        
        /// <summary>
        /// 标签文本：渲染距离
        /// </summary>
        public const string LabelRenderDistance = "渲染距离(m)";
        
        /// <summary>
        /// 标签文本：相机防抖强度
        /// </summary>
        public const string LabelAntiBob = "相机防抖强度";
        
        /// <summary>
        /// 标签文本：相机FOV
        /// </summary>
        public const string LabelFov = "相机FOV(视野)";
        
        /// <summary>
        /// 标签文本：受击效果开关
        /// </summary>
        public const string LabelHitFxEnable = "受击效果开关(0/1)";
        
        /// <summary>
        /// 标签文本：受击后处理强度
        /// </summary>
        public const string LabelHitFxPost = "受击后处理强度";
        
        /// <summary>
        /// 标签文本：受击屏幕蒙版强度
        /// </summary>
        public const string LabelHitFxUi = "受击屏幕蒙版强度";
        
        /// <summary>
        /// 标签文本：受击方向指示
        /// </summary>
        public const string LabelHitFxDir = "受击方向指示(0/1)";
        #endregion

        #region 受击特效默认值
        /// <summary>
        /// 受击特效开关默认值
        /// </summary>
        public const float HitFxEnableDefault = 1f;
        
        /// <summary>
        /// 受击后处理强度默认值
        /// </summary>
        public const float HitFxPostDefault = 0.75f;
        
        /// <summary>
        /// 受击UI强度默认值
        /// </summary>
        public const float HitFxUiDefault = 0.60f;
        
        /// <summary>
        /// 受击方向指示默认值
        /// </summary>
        public const float HitFxDirDefault = 1f;
        #endregion

        #region 数值格式
        /// <summary>
        /// 默认数值格式（两位小数）
        /// </summary>
        public const string ValueFormatDefault = "0.00";
        
        /// <summary>
        /// 整数数值格式
        /// </summary>
        public const string ValueFormatInteger = "0";
        #endregion
    }
}

