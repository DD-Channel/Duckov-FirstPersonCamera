using UnityEngine;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 选项管理模块
    /// 负责加载和管理所有第一人称相机的选项设置，
    /// 包括灵敏度、偏移、性能参数、可视性设置等
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 选项键常量定义（引用OptionsUIConstants）
        // 使用OptionsUIConstants中定义的常量，避免重复定义
        private const string SensitivityLegacyKey = OptionsUIConstants.SensitivityLegacyKey;
        private const string SensitivityXKey = OptionsUIConstants.SensitivityXKey;
        private const string SensitivityYKey = OptionsUIConstants.SensitivityYKey;
        private const string Scope1_2xSensitivityKey = OptionsUIConstants.Scope1_2xSensitivityKey;
        private const string Scope2xSensitivityKey = OptionsUIConstants.Scope2xSensitivityKey;
        private const string Scope4xSensitivityKey = OptionsUIConstants.Scope4xSensitivityKey;
        private const string Scope8xSensitivityKey = OptionsUIConstants.Scope8xSensitivityKey;
        private const string OffsetHeightKey = OptionsUIConstants.OffsetHeightKey;
        private const string OffsetForwardKey = OptionsUIConstants.OffsetForwardKey;
        private const string OffsetRightKey = OptionsUIConstants.OffsetRightKey;
        private const string AntiBobStrengthKey = OptionsUIConstants.AntiBobStrengthKey;
        private const string FovKey = OptionsUIConstants.FovKey;
        
        // 按键相关常量
        private const string ToggleKeyCodeSaveKey = "FirstPersonCamera_ToggleKeyCode";
        public const string PeekLeftKeyCodeSaveKey = "FirstPersonCamera_PeekLeftKeyCode";
        public const string PeekRightKeyCodeSaveKey = "FirstPersonCamera_PeekRightKeyCode";
        private const string ToggleKeyLegacyKey = "FirstPersonCamera_ToggleKey";
        private const string PeekLeftKeyLegacyKey = "FirstPersonCamera_PeekLeftKey";
        private const string PeekRightKeyLegacyKey = "FirstPersonCamera_PeekRightKey";
        
        // 性能相关常量
        private const string PerfAimIntervalKey = "FirstPersonCamera_Perf_AimInterval";
        private const string PerfAimAngleKey = "FirstPersonCamera_Perf_AimAngleThreshold";
        private const string PerfBodyAngleKey = "FirstPersonCamera_Perf_BodyAlignAngleThreshold";
        private const string PerfRaycastDistKey = "FirstPersonCamera_Perf_RaycastDistance";
        
        // 可视性选项键（引用OptionsUIConstants）
        private const string HideHelmetKey = OptionsUIConstants.HideHelmetKey;
        private const string HideFaceMaskKey = OptionsUIConstants.HideFaceMaskKey;
        private const string HideArmorKey = OptionsUIConstants.HideArmorKey;
        private const string HideFaceKey = OptionsUIConstants.HideFaceKey;
        private const string HideHairKey = OptionsUIConstants.HideHairKey;
        private const string HideBackpackKey = OptionsUIConstants.HideBackpackKey;
        private const string HideMeleeKey = OptionsUIConstants.HideMeleeKey;
        private const string HideLeftHandKey = OptionsUIConstants.HideLeftHandKey;
        private const string HideRightHandKey = OptionsUIConstants.HideRightHandKey;
        private const string HideHeadsetKey = OptionsUIConstants.HideHeadsetKey;
        private const string HideFaceEyesKey = OptionsUIConstants.HideFaceEyesKey;
        private const string HideFaceEyebrowsKey = OptionsUIConstants.HideFaceEyebrowsKey;
        private const string HideFaceMouthKey = OptionsUIConstants.HideFaceMouthKey;
        private const string HideFaceTailKey = OptionsUIConstants.HideFaceTailKey;
        private const string HideFaceFeetKey = OptionsUIConstants.HideFaceFeetKey;
        private const string HideFaceWingsKey = OptionsUIConstants.HideFaceWingsKey;
        private const string ForceShowHeldWeaponKey = OptionsUIConstants.ForceShowHeldWeaponKey;
        #endregion

        #region 默认值常量定义（引用OptionsUIConstants）
        private const float DefaultSensitivity = OptionsUIConstants.SensitivityDefault;
        private const float SensitivityMin = OptionsUIConstants.SensitivityMin;
        private const float SensitivityMax = OptionsUIConstants.SensitivityMax;
        private const float ScopeSensitivityMultiplierMin = OptionsUIConstants.ScopeSensitivityMultiplierMin;
        private const float ScopeSensitivityMultiplierMax = OptionsUIConstants.ScopeSensitivityMultiplierMax;
        private const float DefaultScope1_2xSensitivity = OptionsUIConstants.Scope1_2xSensitivityDefault;
        private const float DefaultScope2xSensitivity = OptionsUIConstants.Scope2xSensitivityDefault;
        private const float DefaultScope4xSensitivity = OptionsUIConstants.Scope4xSensitivityDefault;
        private const float DefaultScope8xSensitivity = OptionsUIConstants.Scope8xSensitivityDefault;
        private const float DefaultOffsetHeight = OptionsUIConstants.OffsetHeightDefault;
        private const float DefaultOffsetForward = OptionsUIConstants.OffsetForwardDefault;
        private const float DefaultOffsetRight = OptionsUIConstants.OffsetRightDefault;
        private const float OffsetMin = OptionsUIConstants.OffsetMin;
        private const float OffsetMax = OptionsUIConstants.OffsetMax;
        private const float DefaultAntiBobStrength = OptionsUIConstants.AntiBobDefault;
        private const float DefaultFov = OptionsUIConstants.FovDefault;
        private const float FovMin = OptionsUIConstants.FovMin;
        private const float FovMax = OptionsUIConstants.FovMax;
        
        // 性能相关默认值
        private const float DefaultAimInterval = 0.02f;
        private const float AimIntervalMin = 0.0f;
        private const float AimIntervalMax = 0.2f;
        private const float DefaultAimAngleThreshold = 0.25f;
        private const float AimAngleThresholdMin = 0.0f;
        private const float AimAngleThresholdMax = 5f;
        private const float DefaultBodyAlignAngleThreshold = 1.5f;
        private const float BodyAlignAngleThresholdMin = 0.0f;
        private const float BodyAlignAngleThresholdMax = 10f;
        private const float DefaultRaycastDistance = 250f;
        private const float RaycastDistanceMin = 30f;
        private const float RaycastDistanceMax = 600f;
        
        // 其他默认值
        private const KeyCode DefaultToggleKey = KeyCode.F5;
        private const float AdsValueThreshold = 0.05f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 防抖强度（0.0-1.0，0为关闭）
        /// </summary>
        private float antiBobStrength;
        #endregion

        #region 选项变更回调
        /// <summary>
        /// 选项变更回调
        /// 当任何选项被修改时调用，根据选项键更新相应的设置
        /// </summary>
        /// <param name="key">变更的选项键</param>
        private void OnOptionsChanged(string key)
        {
            // 处理鼠标灵敏度选项
            if (key == SensitivityLegacyKey || key == SensitivityXKey || key == SensitivityYKey || 
                key == Scope1_2xSensitivityKey || key == Scope2xSensitivityKey ||
                key == Scope4xSensitivityKey || key == Scope8xSensitivityKey)
            {
                LoadSensitivityFromOptions();
            }

            // 处理相机偏移选项
            if (key == OffsetHeightKey || key == OffsetForwardKey || key == OffsetRightKey)
            {
                LoadOffsetsFromOptions();
            }

            // 处理性能选项
            if (key == PerfAimIntervalKey || key == PerfAimAngleKey || key == PerfBodyAngleKey || key == PerfRaycastDistKey)
            {
                LoadPerfOptions();
            }

            // 处理防抖强度选项
            if (key == AntiBobStrengthKey)
            {
                LoadAntiBobFromOptions();
            }

            // 处理视野角度选项
            if (key == FovKey)
            {
                LoadFovFromOptions();
            }

            // 处理切换键选项
            if (key == ToggleKeyLegacyKey)
            {
                LoadToggleKeyFromOptions();
            }

            // 处理偏头键选项
            if (key == PeekLeftKeyLegacyKey || key == PeekRightKeyLegacyKey)
            {
                LoadPeekKeysFromOptions();
            }

            // 处理后处理模糊选项
            if (key == FirstPersonOptionsUI.DisableBlurKey)
            {
                HandleBlurOptionChange();
            }

            // 处理遮挡/透视选项
            if (key == FirstPersonOptionsUI.DisableSeeThroughKey)
            {
                HandleSeeThroughOptionChange();
            }

            // 处理准星虚化选项
            if (key == FirstPersonOptionsUI.DisableAimOcclusionFadeKey)
            {
                HandleAimOcclusionFadeOptionChange();
            }

            // 处理可视性选项
            if (IsVisibilityOptionKey(key))
            {
                HandleVisibilityOptionChange();
            }
            
        }

        /// <summary>
        /// 判断是否为可视性选项键
        /// </summary>
        /// <param name="key">选项键</param>
        /// <returns>如果是可视性选项键则返回true，否则返回false</returns>
        private bool IsVisibilityOptionKey(string key)
        {
            return key == HideHelmetKey || key == HideFaceMaskKey || key == HideArmorKey || 
                   key == HideFaceKey || key == HideHairKey || key == HideBackpackKey || 
                   key == HideMeleeKey || key == HideLeftHandKey || key == HideRightHandKey || 
                   key == HideHeadsetKey || key == HideFaceEyesKey || key == HideFaceEyebrowsKey || 
                   key == HideFaceMouthKey || key == HideFaceTailKey || key == HideFaceFeetKey || 
                   key == HideFaceWingsKey || key == ForceShowHeldWeaponKey;
        }

        /// <summary>
        /// 处理后处理模糊选项变更
        /// </summary>
        private void HandleBlurOptionChange()
        {
            // 在第一人称模式下立即应用/恢复模糊效果
            if (!isFirstPersonMode) return;

            SafeExecution.Execute(() =>
            {
                int optionValue = OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableBlurKey, 1);
                if (optionValue == 1)
                {
                    DisablePostProcessingBlurEffects();
                }
                else
                {
                    RestorePostProcessingBlurEffects();
                }
            });
        }

        /// <summary>
        /// 处理遮挡/透视选项变更
        /// </summary>
        private void HandleSeeThroughOptionChange()
        {
            if (!isFirstPersonMode) return;

            SafeExecution.Execute(() =>
            {
                int optionValue = OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableSeeThroughKey, 1);
                if (optionValue == 1)
                {
                    DisableObstructionSeeThroughEffects();
                }
                else
                {
                    RestoreObstructionSeeThroughEffects();
                }
            });
        }

        /// <summary>
        /// 处理准星虚化选项变更
        /// </summary>
        private void HandleAimOcclusionFadeOptionChange()
        {
            if (!isFirstPersonMode) return;

            SafeExecution.Execute(() =>
            {
                int optionValue = OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableAimOcclusionFadeKey, 1);
                if (optionValue == 1)
                {
                    DisableAimOcclusionFade();
                }
                else
                {
                    RestoreAimOcclusionFade();
                }
            });
        }

        /// <summary>
        /// 处理可视性选项变更
        /// </summary>
        private void HandleVisibilityOptionChange()
        {
            // 在第一人称模式下实时重新应用隐藏逻辑
            if (!isFirstPersonMode) return;

            SafeExecution.Execute(() =>
            {
                RestoreFirstPersonObstructions();
                HideFirstPersonObstructions();
            });
        }
        #endregion

        #region 选项加载方法
        /// <summary>
        /// 从选项加载鼠标灵敏度
        /// </summary>
        private void LoadSensitivityFromOptions()
        {
            // 加载旧版灵敏度（用于向后兼容）
            float legacySensitivity = OptionsHelper.LoadFloat(SensitivityLegacyKey, DefaultSensitivity);
            
            // 加载X和Y轴灵敏度（如果未设置则使用旧版值）
            float sensitivityX = OptionsHelper.LoadFloat(SensitivityXKey, legacySensitivity);
            float sensitivityY = OptionsHelper.LoadFloat(SensitivityYKey, legacySensitivity);
            
            // 限制在合理范围内
            mouseSensitivityX = OptionsHelper.LoadFloatClamped(SensitivityXKey, sensitivityX, SensitivityMin, SensitivityMax);
            mouseSensitivityY = OptionsHelper.LoadFloatClamped(SensitivityYKey, sensitivityY, SensitivityMin, SensitivityMax);
            
            // 加载1.2x倍镜灵敏度倍数（同时应用于水平和垂直）
            float scope1_2x = OptionsHelper.LoadFloat(Scope1_2xSensitivityKey, DefaultScope1_2xSensitivity);
            scope1_2xSensitivity = OptionsHelper.LoadFloatClamped(Scope1_2xSensitivityKey, scope1_2x, ScopeSensitivityMultiplierMin, ScopeSensitivityMultiplierMax);
            
            // 加载2x倍镜灵敏度倍数
            float scope2x = OptionsHelper.LoadFloat(Scope2xSensitivityKey, DefaultScope2xSensitivity);
            scope2xSensitivity = OptionsHelper.LoadFloatClamped(Scope2xSensitivityKey, scope2x, ScopeSensitivityMultiplierMin, ScopeSensitivityMultiplierMax);
            
            // 加载4x倍镜灵敏度倍数
            float scope4x = OptionsHelper.LoadFloat(Scope4xSensitivityKey, DefaultScope4xSensitivity);
            scope4xSensitivity = OptionsHelper.LoadFloatClamped(Scope4xSensitivityKey, scope4x, ScopeSensitivityMultiplierMin, ScopeSensitivityMultiplierMax);
            
            // 加载8x倍镜灵敏度倍数
            float scope8x = OptionsHelper.LoadFloat(Scope8xSensitivityKey, DefaultScope8xSensitivity);
            scope8xSensitivity = OptionsHelper.LoadFloatClamped(Scope8xSensitivityKey, scope8x, ScopeSensitivityMultiplierMin, ScopeSensitivityMultiplierMax);
        }

        /// <summary>
        /// 从选项加载相机偏移
        /// </summary>
        private void LoadOffsetsFromOptions()
        {
            cameraHeightOffset = OptionsHelper.LoadFloatClamped(OffsetHeightKey, DefaultOffsetHeight, OffsetMin, OffsetMax);
            cameraForwardOffset = OptionsHelper.LoadFloatClamped(OffsetForwardKey, DefaultOffsetForward, OffsetMin, OffsetMax);
            cameraRightOffset = OptionsHelper.LoadFloatClamped(OffsetRightKey, DefaultOffsetRight, OffsetMin, OffsetMax);

            // 屏蔽 0.13 ~ 0.22 区间，强制设为 0.12（问题区间的下限之外）
            if (cameraForwardOffset >= 0.13f && cameraForwardOffset <= 0.22f)
            {
                cameraForwardOffset = 0.12f;
                // 保存修正后的值到配置文件，避免下次加载时又读回问题值
                OptionsHelper.SaveFloat(OffsetForwardKey, cameraForwardOffset);
            }
        }

        /// <summary>
        /// 从选项加载性能参数
        /// </summary>
        private void LoadPerfOptions()
        {
            aimRecalcInterval = OptionsHelper.LoadFloatClamped(PerfAimIntervalKey, DefaultAimInterval, AimIntervalMin, AimIntervalMax);
            aimAngleThreshold = OptionsHelper.LoadFloatClamped(PerfAimAngleKey, DefaultAimAngleThreshold, AimAngleThresholdMin, AimAngleThresholdMax);
            bodyAlignAngleThreshold = OptionsHelper.LoadFloatClamped(PerfBodyAngleKey, DefaultBodyAlignAngleThreshold, BodyAlignAngleThresholdMin, BodyAlignAngleThresholdMax);
            raycastDistance = OptionsHelper.LoadFloatClamped(PerfRaycastDistKey, DefaultRaycastDistance, RaycastDistanceMin, RaycastDistanceMax);
        }

        /// <summary>
        /// 从选项加载防抖强度
        /// </summary>
        private void LoadAntiBobFromOptions()
        {
            antiBobStrength = OptionsHelper.LoadFloatClamped01(AntiBobStrengthKey, DefaultAntiBobStrength);
        }

        /// <summary>
        /// 从选项加载视野角度（FOV）
        /// </summary>
        private void LoadFovFromOptions()
        {
            float fovValue = OptionsHelper.LoadFloatClamped(FovKey, DefaultFov, FovMin, FovMax);
            fov = fovValue;
            
            // 更新基础FOV；若当前在ADS，避免立即覆盖相机FOV，交给倍镜平滑逻辑处理
            baseFov = fov;
            
            // 检查是否在瞄准状态（ADS）
            bool isInAds = IsInAdsState();
            
            // 如果不在ADS状态，立即更新相机FOV
            if (mainCamera != null && !isInAds)
            {
                mainCamera.fieldOfView = fov;
            }
        }

        /// <summary>
        /// 检查是否在瞄准状态（ADS）
        /// </summary>
        /// <returns>如果在ADS状态则返回true，否则返回false</returns>
        public bool IsInAdsState()
        {
            return SafeExecution.Execute(() =>
            {
                var gun = mainCharacter?.GetGun();
                return gun != null && gun.AdsValue > AdsValueThreshold;
            }, false);
        }

        /// <summary>
        /// 从选项加载切换键
        /// </summary>
        private void LoadToggleKeyFromOptions()
        {
            toggleKey = OptionsHelper.LoadKeyCode(ToggleKeyCodeSaveKey, DefaultToggleKey);
        }
        #endregion

        #region 外部鼠标释放API
        /// <summary>
        /// 外部鼠标释放计数器
        /// 用于与其他mod协作，允许其他mod临时释放鼠标控制
        /// </summary>
        private static int externalMouseReleaseCounter;
        
        /// <summary>
        /// 是否鼠标被外部释放
        /// 用于判断是否有其他mod正在控制鼠标
        /// </summary>
        public static bool IsMouseReleasedExternally => externalMouseReleaseCounter > 0;
        
        /// <summary>
        /// 开始外部鼠标释放
        /// 其他mod可以调用此方法以临时释放鼠标控制
        /// </summary>
        public static void BeginExternalMouseRelease()
        {
            externalMouseReleaseCounter++;
            if (Instance != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        /// <summary>
        /// 结束外部鼠标释放
        /// 其他mod在完成操作后应调用此方法以恢复鼠标控制
        /// </summary>
        public static void EndExternalMouseRelease()
        {
            externalMouseReleaseCounter = Mathf.Max(0, externalMouseReleaseCounter - 1);
        }
        #endregion
    }
}