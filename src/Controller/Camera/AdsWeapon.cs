using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - ADS武器位移系统模块
    /// 负责处理ADS状态下武器的位置调整、武器高度/左右/前后偏移管理和ADS提示UI
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region ADS武器位移系统字段和配置
        /// <summary>
        /// ADS提示UI的Canvas对象
        /// </summary>
        private GameObject adsHintCanvas;
        
        /// <summary>
        /// ADS提示UI的文本组件
        /// </summary>
        private TextMeshProUGUI adsHintText;
        
        /// <summary>
        /// ADS武器在相机空间的偏移量（相机坐标系下的偏移）
        /// </summary>
        [SerializeField] private Vector3 adsWeaponCamSpaceOffset = new Vector3(0f, -0.21f, 0.25f);
        
        /// <summary>
        /// ADS武器平滑插值速度（每秒）
        /// </summary>
        [SerializeField] private float adsWeaponLerpSpeed = 36f;
        
        /// <summary>
        /// 是否直接设置武器位置（推荐开启以避免旋转抖动）
        /// 直接设置位置可以避免父节点旋转时的抖动，但可能不够平滑
        /// 使用平滑插值时，当父节点旋转会导致目标局部位置每帧变化，产生抖动
        /// </summary>
        [SerializeField] private bool adsWeaponUseDirectSet = true;
        
        /// <summary>
        /// 上次缓存的武器Transform（用于恢复原始位置）
        /// </summary>
        private Transform adsLastGunTransform;
        
        /// <summary>
        /// 武器的原始局部位置（缓存，用于恢复）
        /// </summary>
        private Vector3 adsOriginalLocalPos;
        
        /// <summary>
        /// 武器的原始局部旋转（缓存，用于恢复）
        /// </summary>
        private Quaternion adsOriginalLocalRot;
        
        /// <summary>
        /// 是否已缓存武器的原始位姿
        /// </summary>
        private bool adsCachedOrig;
        
        #region 调节参数（Y轴 - 上下）
        /// <summary>
        /// Y轴偏移调整步长（每次滚轮调整的增量）
        /// </summary>
        [SerializeField] private float adsOffsetYStep = 0.001f;
        
        /// <summary>
        /// Y轴偏移最小值（米）
        /// </summary>
        [SerializeField] private float adsOffsetYDeltaMin = -0.25f;
        
        /// <summary>
        /// Y轴偏移最大值（米）
        /// </summary>
        [SerializeField] private float adsOffsetYDeltaMax = 0.5f;
        
        /// <summary>
        /// 当前Y轴偏移增量（用于调整武器高度）
        /// </summary>
        private float adsYOffsetDelta = 0f;
        
        /// <summary>
        /// 每把枪的独立高度偏移值映射（TypeID -> 偏移值）
        /// </summary>
        private System.Collections.Generic.Dictionary<int, float> gunYOffsetMap = 
            new System.Collections.Generic.Dictionary<int, float>();
        #endregion

        #region 调节参数（X轴 - 左右）
        /// <summary>
        /// X轴偏移调整步长（每次滚轮调整的增量）
        /// </summary>
        [SerializeField] private float adsOffsetXStep = 0.001f;
        
        /// <summary>
        /// X轴偏移最小值（米） - 已减少一半（原 -0.25f）
        /// </summary>
        [SerializeField] private float adsOffsetXDeltaMin = -0.25f;
        
        /// <summary>
        /// X轴偏移最大值（米）
        /// </summary>
        [SerializeField] private float adsOffsetXDeltaMax = 0.5f;
        
        /// <summary>
        /// 当前X轴偏移增量（用于调整武器左右）
        /// </summary>
        private float adsXOffsetDelta = 0f;
        
        /// <summary>
        /// 每把枪的独立左右偏移值映射（TypeID -> 偏移值）
        /// </summary>
        private System.Collections.Generic.Dictionary<int, float> gunXOffsetMap = 
            new System.Collections.Generic.Dictionary<int, float>();
        #endregion

        #region 调节参数（Z轴 - 前后）
        /// <summary>
        /// Z轴偏移调整步长（每次滚轮调整的增量）
        /// </summary>
        [SerializeField] private float adsOffsetZStep = 0.001f;
        
        /// <summary>
        /// Z轴偏移最小值（米）
        /// </summary>
        [SerializeField] private float adsOffsetZDeltaMin = -0.25f;
        
        /// <summary>
        /// Z轴偏移最大值（米）
        /// </summary>
        [SerializeField] private float adsOffsetZDeltaMax = 0.5f;
        
        /// <summary>
        /// 当前Z轴偏移增量（用于调整武器前后）
        /// </summary>
        private float adsZOffsetDelta = 0f;
        
        /// <summary>
        /// 每把枪的独立前后偏移值映射（TypeID -> 偏移值）
        /// </summary>
        private System.Collections.Generic.Dictionary<int, float> gunZOffsetMap = 
            new System.Collections.Generic.Dictionary<int, float>();
        #endregion
        
        /// <summary>
        /// 调节模式枚举
        /// </summary>
        private enum AdsAdjustMode
        {
            X,  // 左右
            Y,  // 上下
            Z   // 前后
        }
        
        /// <summary>
        /// 当前调节模式
        /// </summary>
        private AdsAdjustMode adsAdjustMode = AdsAdjustMode.Y; // 默认Y轴（高度）
        
        /// <summary>
        /// 模式切换快捷键（旧输入系统）
        /// </summary>
        private const KeyCode ADS_MODE_TOGGLE_KEY_LEGACY = KeyCode.Minus;
        
        /// <summary>
        /// 模式切换快捷键（新输入系统）
        /// </summary>
        private const Key ADS_MODE_TOGGLE_KEY_NEW = Key.Minus;
        
        /// <summary>
        /// 当前武器的TypeID（用于检测武器切换）
        /// </summary>
        private int currentGunTypeID = -1;
        
        /// <summary>
        /// ADS释放延迟时间（秒），松开右键后延迟一段时间再允许复位，避免开火/动画造成ADS抖动
        /// </summary>
        [SerializeField] private float adsReleaseGrace = 0.05f;
        
        /// <summary>
        /// ADS释放计时器（秒）
        /// </summary>
        private float adsReleaseTimer = 0f;
        
        /// <summary>
        /// 是否正在ADS状态
        /// </summary>
        private bool adsEngaged = false;
        
        /// <summary>
        /// 是否正在平滑移动到目标位置（举枪平滑过渡）
        /// 跟踪是否刚进入ADS，需要先平滑移动，到达后再直接设置以避免旋转抖动
        /// </summary>
        private bool adsIsSmoothingToTarget = false;
        
        /// <summary>
        /// ADS平滑阈值（米），当距离目标小于这个值时，切换到直接设置模式
        /// </summary>
        private const float ADS_SMOOTH_THRESHOLD = 0.001f;
        
        /// <summary>
        /// ADS旋转阈值（度），当旋转角度小于这个值时，切换到直接设置模式
        /// </summary>
        private const float ADS_ROTATION_THRESHOLD = 0.5f;
        
        /// <summary>
        /// 当前武器的瞄准速度（用于ADS平滑和FOV平滑）
        /// </summary>
        private float currentAdsSpeed = 36f;
        
        /// <summary>
        /// ADS速度加速倍数（用于加快ADS过渡速度）
        /// </summary>
        private const float ADS_SPEED_MULTIPLIER = 25f;
        
        /// <summary>
        /// 默认ADS速度（当无法获取武器速度时使用）
        /// </summary>
        private const float DEFAULT_ADS_SPEED = 36f;
        
        /// <summary>
        /// ADS激活阈值，当AdsValue大于此值时认为进入ADS状态
        /// </summary>
        private const float ADS_ACTIVATION_THRESHOLD = 0.05f;
        
        /// <summary>
        /// 鼠标滚轮灵敏度（用于转换滚轮输入）
        /// </summary>
        private const float MOUSE_SCROLL_MULTIPLIER = 120f;
        
        /// <summary>
        /// 滚轮输入阈值，小于此值时不响应滚轮输入
        /// </summary>
        private const float SCROLL_INPUT_THRESHOLD = 0.01f;
        #endregion

        #region 武器三轴偏移系统
        // Y轴偏移保存/加载
        private void SaveGunYOffset(int gunTypeID, float offset)
        {
            try
            {
                gunYOffsetMap[gunTypeID] = offset;
                string key = $"FirstPersonCamera_GunYOffset_{gunTypeID}";
                ConfigManager.Save<float>(key, offset);
            }
            catch { }
        }
        
        private float LoadGunYOffset(int gunTypeID)
        {
            try
            {
                if (gunYOffsetMap.TryGetValue(gunTypeID, out float cached))
                    return cached;
                string key = $"FirstPersonCamera_GunYOffset_{gunTypeID}";
                float saved = ConfigManager.Load<float>(key, 0f, keepCurrentOnError: true);
                gunYOffsetMap[gunTypeID] = saved;
                return saved;
            }
            catch { return 0f; }
        }

        // X轴偏移保存/加载
        private void SaveGunXOffset(int gunTypeID, float offset)
        {
            try
            {
                gunXOffsetMap[gunTypeID] = offset;
                string key = $"FirstPersonCamera_GunXOffset_{gunTypeID}";
                ConfigManager.Save<float>(key, offset);
            }
            catch { }
        }
        
        private float LoadGunXOffset(int gunTypeID)
        {
            try
            {
                if (gunXOffsetMap.TryGetValue(gunTypeID, out float cached))
                    return cached;
                string key = $"FirstPersonCamera_GunXOffset_{gunTypeID}";
                float saved = ConfigManager.Load<float>(key, 0f, keepCurrentOnError: true);
                gunXOffsetMap[gunTypeID] = saved;
                return saved;
            }
            catch { return 0f; }
        }

        // Z轴偏移保存/加载
        private void SaveGunZOffset(int gunTypeID, float offset)
        {
            try
            {
                gunZOffsetMap[gunTypeID] = offset;
                string key = $"FirstPersonCamera_GunZOffset_{gunTypeID}";
                ConfigManager.Save<float>(key, offset);
            }
            catch { }
        }
        
        private float LoadGunZOffset(int gunTypeID)
        {
            try
            {
                if (gunZOffsetMap.TryGetValue(gunTypeID, out float cached))
                    return cached;
                string key = $"FirstPersonCamera_GunZOffset_{gunTypeID}";
                float saved = ConfigManager.Load<float>(key, 0f, keepCurrentOnError: true);
                gunZOffsetMap[gunTypeID] = saved;
                return saved;
            }
            catch { return 0f; }
        }

        /// <summary>
        /// 初始化武器偏移系统
        /// </summary>
        private void InitializeGunOffsetSystem()
        {
            try
            {
                gunYOffsetMap.Clear();
                gunXOffsetMap.Clear();
                gunZOffsetMap.Clear();
                currentGunTypeID = -1;
                adsAdjustMode = AdsAdjustMode.Y; // 默认Y轴
            }
            catch { }
        }
        #endregion

        #region ADS武器位移管理
        /// <summary>
        /// 更新ADS武器位置
        /// 当进入ADS状态时，将武器移动到相机下方，保持与相机同朝向
        /// 支持滚轮调整武器高度/左右/前后，每把武器独立保存偏移值
        /// 按【-】键切换调节模式（左右→上下→前后循环）
        /// </summary>
        private void UpdateAdsWeaponPlacement()
        {
            if (mainCharacter == null || mainCamera == null) return;
            
            if (isInspectingWeapon || isInspectingMelee)
                return;
            
            var gun = mainCharacter.GetGun();
            if (gun == null)
            {
                RestoreAdsWeaponIfNeeded();
                UpdateAdsHint(false);
                return;
            }
            
            var gunTf = gun.transform;
            if (gunTf == null)
            {
                RestoreAdsWeaponIfNeeded();
                UpdateAdsHint(false);
                return;
            }
            
            // 获取武器TypeID
            int gunTypeID = -1;
            try
            {
                if (gun.Item != null)
                    gunTypeID = gun.Item.TypeID;
            }
            catch { }

            // 武器切换时加载对应的X/Y/Z偏移
            if (gunTypeID != -1 && gunTypeID != currentGunTypeID)
            {
                // 保存当前武器的偏移
                if (currentGunTypeID != -1)
                {
                    SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                    SaveGunXOffset(currentGunTypeID, adsXOffsetDelta);
                    SaveGunZOffset(currentGunTypeID, adsZOffsetDelta);
                }
                
                currentGunTypeID = gunTypeID;
                adsYOffsetDelta = LoadGunYOffset(gunTypeID);
                adsXOffsetDelta = LoadGunXOffset(gunTypeID);
                adsZOffsetDelta = LoadGunZOffset(gunTypeID);
                
                adsCachedOrig = false;
                adsLastGunTransform = null;
            }
            else if (gunTypeID == -1 && currentGunTypeID != -1)
            {
                // 武器被移除，保存当前偏移
                SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                SaveGunXOffset(currentGunTypeID, adsXOffsetDelta);
                SaveGunZOffset(currentGunTypeID, adsZOffsetDelta);
                currentGunTypeID = -1;
                adsCachedOrig = false;
                adsLastGunTransform = null;
            }

            // 获取ADS状态
            float ads = 0f;
            try { ads = gun.AdsValue; } catch { ads = 0f; }
            
            bool rmbPressed = false;
            try
            {
                if (useNewInputSystem)
                {
                    var m = Mouse.current;
                    if (m != null) rmbPressed = m.rightButton.isPressed;
                }
                else
                {
                    rmbPressed = Input.GetMouseButton(1);
                }
            }
            catch { }

            // 更新ADS状态
            bool wasAdsEngaged = adsEngaged;
            if (rmbPressed || ads > ADS_ACTIVATION_THRESHOLD)
            {
                adsEngaged = true;
                adsReleaseTimer = adsReleaseGrace;
                if (!wasAdsEngaged) adsIsSmoothingToTarget = true;
                
                try
                {
                    currentAdsSpeed = gun.AdsSpeed * ADS_SPEED_MULTIPLIER;
                    if (currentAdsSpeed <= 0f) currentAdsSpeed = DEFAULT_ADS_SPEED * ADS_SPEED_MULTIPLIER;
                }
                catch { currentAdsSpeed = DEFAULT_ADS_SPEED * ADS_SPEED_MULTIPLIER; }
                FPLogger.Log("[UpdateAdsWeaponPlacement] about to call UpdateScopeFOV");
                UpdateScopeFOV(gun);
                FPLogger.Log("[UpdateAdsWeaponPlacement] back from UpdateScopeFOV");
            }
            else if (adsReleaseTimer > 0f)
            {
                adsReleaseTimer -= Time.unscaledDeltaTime;
                if (adsReleaseTimer <= 0f)
                {
                    adsEngaged = false;
                    adsIsSmoothingToTarget = false;
                    RestoreBaseFOV();
                }
                else
                {
                    if (gun != null) UpdateScopeFOVSmooth(gun);
                }
            }
            else
            {
                adsIsSmoothingToTarget = false;
                RestoreBaseFOV();
                if (gun != null) UpdateScopeFOVSmooth(gun);
                else if (mainCamera != null) mainCamera.fieldOfView = baseFov;
            }
            
            if (gun != null) UpdateScopeFOVSmooth(gun);

            bool shouldAdsPlace = adsEngaged;

            bool adsOffsetEnabled = true;
            try
            {
                adsOffsetEnabled = OptionsHelper.LoadInt(FirstPersonOptionsUI.EnableAdsOffsetKey, 1) == 1;
            }
            catch { adsOffsetEnabled = true; }
            
            // 更新提示显示
            UpdateAdsHint(adsOffsetEnabled && shouldAdsPlace && (ads > ADS_ACTIVATION_THRESHOLD || rmbPressed));
            
            if (!adsOffsetEnabled)
            {
                RestoreAdsWeaponIfNeeded();
                return;
            }
            
            if (!shouldAdsPlace)
            {
                RestoreAdsWeaponIfNeeded();
                RestoreAdsCrosshair(); // 退出瞄准时恢复准星
                return;
            }

            // 缓存原始位姿
            if (!adsCachedOrig || adsLastGunTransform != gunTf)
            {
                adsOriginalLocalPos = gunTf.localPosition;
                adsOriginalLocalRot = gunTf.localRotation;
                adsLastGunTransform = gunTf;
                adsCachedOrig = true;
            }

            // 检测模式切换键【=】（适配新旧输入系统）
            try
            {
                if (useNewInputSystem)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null && keyboard[ADS_MODE_TOGGLE_KEY_NEW].wasPressedThisFrame)
                    {
                        // 循环切换模式：X -> Y -> Z -> X
                        adsAdjustMode = adsAdjustMode switch
                        {
                            AdsAdjustMode.X => AdsAdjustMode.Y,
                            AdsAdjustMode.Y => AdsAdjustMode.Z,
                            AdsAdjustMode.Z => AdsAdjustMode.X,
                            _ => AdsAdjustMode.Y
                        };
                    }
                }
                else
                {
                    if (Input.GetKeyDown(ADS_MODE_TOGGLE_KEY_LEGACY))
                    {
                        adsAdjustMode = adsAdjustMode switch
                        {
                            AdsAdjustMode.X => AdsAdjustMode.Y,
                            AdsAdjustMode.Y => AdsAdjustMode.Z,
                            AdsAdjustMode.Z => AdsAdjustMode.X,
                            _ => AdsAdjustMode.Y
                        };
                    }
                }
            }
            catch { }

            // 滚轮调节
            float scroll = 0f;
            try
            {
                if (useNewInputSystem)
                {
                    var mouse = Mouse.current;
                    if (mouse != null) scroll = mouse.scroll.ReadValue().y;
                }
                else
                {
                    scroll = Input.GetAxis("Mouse ScrollWheel") * MOUSE_SCROLL_MULTIPLIER;
                }
            }
            catch { }
            
            if (Mathf.Abs(scroll) > SCROLL_INPUT_THRESHOLD)
            {
                float delta = Mathf.Sign(scroll);
                switch (adsAdjustMode)
                {
                    case AdsAdjustMode.X:
                        delta *= adsOffsetXStep;
                        adsXOffsetDelta = Mathf.Clamp(adsXOffsetDelta + delta, adsOffsetXDeltaMin, adsOffsetXDeltaMax);
                        if (currentGunTypeID != -1) SaveGunXOffset(currentGunTypeID, adsXOffsetDelta);
                        break;
                    case AdsAdjustMode.Y:
                        delta *= adsOffsetYStep;
                        adsYOffsetDelta = Mathf.Clamp(adsYOffsetDelta + delta, adsOffsetYDeltaMin, adsOffsetYDeltaMax);
                        if (currentGunTypeID != -1) SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                        break;
                    case AdsAdjustMode.Z:
                        delta *= adsOffsetZStep;
                        adsZOffsetDelta = Mathf.Clamp(adsZOffsetDelta + delta, adsOffsetZDeltaMin, adsOffsetZDeltaMax);
                        if (currentGunTypeID != -1) SaveGunZOffset(currentGunTypeID, adsZOffsetDelta);
                        break;
                }
            }

            // 计算目标位姿：相机偏移 + 累计X/Y/Z偏移
            Vector3 camPos = mainCamera.transform.position;
            Quaternion camRot = mainCamera.transform.rotation;
            Vector3 camSpaceOffset = adsWeaponCamSpaceOffset;
            camSpaceOffset.x += adsXOffsetDelta;
            camSpaceOffset.y += adsYOffsetDelta;
            camSpaceOffset.z += adsZOffsetDelta;
            Vector3 targetWorldPos = camPos + camRot * camSpaceOffset;
            Quaternion targetWorldRot = camRot;

            Transform parentTf = gunTf.parent;
            if (parentTf != null)
            {
                Vector3 targetLocalPos = parentTf.InverseTransformPoint(targetWorldPos);
                Quaternion targetLocalRot = Quaternion.Inverse(parentTf.rotation) * targetWorldRot;
                
                if (adsIsSmoothingToTarget && adsWeaponUseDirectSet)
                {
                    float t = 1f - Mathf.Exp(-currentAdsSpeed * Time.unscaledDeltaTime);
                    gunTf.localPosition = Vector3.Lerp(gunTf.localPosition, targetLocalPos, t);
                    gunTf.localRotation = Quaternion.Slerp(gunTf.localRotation, targetLocalRot, t);
                    
                    if (Vector3.Distance(gunTf.localPosition, targetLocalPos) < ADS_SMOOTH_THRESHOLD &&
                        Quaternion.Angle(gunTf.localRotation, targetLocalRot) < ADS_ROTATION_THRESHOLD)
                    {
                        adsIsSmoothingToTarget = false;
                        gunTf.localPosition = targetLocalPos;
                        gunTf.localRotation = targetLocalRot;
                    }
                }
                else if (adsWeaponUseDirectSet)
                {
                    gunTf.localPosition = targetLocalPos;
                    gunTf.localRotation = targetLocalRot;
                }
                else
                {
                    float t = 1f - Mathf.Exp(-currentAdsSpeed * Time.unscaledDeltaTime);
                    gunTf.localPosition = Vector3.Lerp(gunTf.localPosition, targetLocalPos, t);
                    gunTf.localRotation = Quaternion.Slerp(gunTf.localRotation, targetLocalRot, t);
                }
            }
            else
            {
                if (adsIsSmoothingToTarget && adsWeaponUseDirectSet)
                {
                    float t = 1f - Mathf.Exp(-currentAdsSpeed * Time.unscaledDeltaTime);
                    gunTf.position = Vector3.Lerp(gunTf.position, targetWorldPos, t);
                    gunTf.rotation = Quaternion.Slerp(gunTf.rotation, targetWorldRot, t);
                    
                    if (Vector3.Distance(gunTf.position, targetWorldPos) < ADS_SMOOTH_THRESHOLD &&
                        Quaternion.Angle(gunTf.rotation, targetWorldRot) < ADS_ROTATION_THRESHOLD)
                    {
                        adsIsSmoothingToTarget = false;
                        gunTf.position = targetWorldPos;
                        gunTf.rotation = targetWorldRot;
                    }
                }
                else if (adsWeaponUseDirectSet)
                {
                    gunTf.position = targetWorldPos;
                    gunTf.rotation = targetWorldRot;
                }
                else
                {
                    float t = 1f - Mathf.Exp(-adsWeaponLerpSpeed * Time.unscaledDeltaTime);
                    gunTf.position = Vector3.Lerp(gunTf.position, targetWorldPos, t);
                    gunTf.rotation = Quaternion.Slerp(gunTf.rotation, targetWorldRot, t);
                }
            }
        }

        /// <summary>
        /// 恢复ADS武器到原始位置（如果需要）
        /// </summary>
        /// <param name="resetOffset">是否重置X/Y/Z偏移值</param>
        private void RestoreAdsWeaponIfNeeded(bool resetOffset = false)
        {
            if (!adsCachedOrig || adsLastGunTransform == null) return;
            
            float t = 1f - Mathf.Exp(-adsWeaponLerpSpeed * Time.unscaledDeltaTime);
            adsLastGunTransform.localPosition = Vector3.Lerp(adsLastGunTransform.localPosition, adsOriginalLocalPos, t);
            adsLastGunTransform.localRotation = Quaternion.Slerp(adsLastGunTransform.localRotation, adsOriginalLocalRot, t);

            if ((adsLastGunTransform.localPosition - adsOriginalLocalPos).sqrMagnitude < 1e-6f)
            {
                adsCachedOrig = false;
                adsLastGunTransform = null;
                if (resetOffset)
                {
                    adsYOffsetDelta = 0f;
                    adsXOffsetDelta = 0f;
                    adsZOffsetDelta = 0f;
                    if (currentGunTypeID != -1)
                    {
                        SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                        SaveGunXOffset(currentGunTypeID, adsXOffsetDelta);
                        SaveGunZOffset(currentGunTypeID, adsZOffsetDelta);
                    }
                }
            }
        }

        /// <summary>
        /// 更新ADS提示UI显示状态
        /// </summary>
        /// <param name="show">是否显示提示</param>
        private void UpdateAdsHint(bool show)
        {
            bool adsOffsetEnabled = true;
            try
            {
                adsOffsetEnabled = OptionsHelper.LoadInt(FirstPersonOptionsUI.EnableAdsOffsetKey, 1) == 1;
            }
            catch { adsOffsetEnabled = true; }
            
            if (adsHintCanvas == null && isFirstPersonMode)
                CreateAdsHintUI();
            
            if (adsHintCanvas == null || adsHintText == null) return;
            
            bool shouldShow = adsOffsetEnabled && show && isFirstPersonMode;
            if (adsHintText.gameObject.activeSelf != shouldShow)
                adsHintText.gameObject.SetActive(shouldShow);
            
            // 更新提示文字根据当前模式
            if (shouldShow)
            {
                string modeText = adsAdjustMode switch
                {
                    AdsAdjustMode.X => "左右",
                    AdsAdjustMode.Y => "上下",
                    AdsAdjustMode.Z => "前后",
                    _ => "上下"
                };
                adsHintText.text = $"滚轮调整枪械{modeText}|[-]切换方向";
            }
        }

        /// <summary>
        /// 创建ADS提示UI
        /// </summary>
        private void CreateAdsHintUI()
        {
            if (adsHintCanvas != null) return;

            try
            {
                adsHintCanvas = new GameObject("AdsHintCanvas");
                DontDestroyOnLoad(adsHintCanvas);
                Canvas canvas = adsHintCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                CanvasScaler scaler = adsHintCanvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                adsHintCanvas.AddComponent<GraphicRaycaster>();

                GameObject textGO = new GameObject("AdsHintText");
                textGO.transform.SetParent(adsHintCanvas.transform, false);
                RectTransform rectTransform = textGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.anchoredPosition = new Vector2(0f, 120f);
                rectTransform.sizeDelta = new Vector2(600f, 40f);

                adsHintText = textGO.AddComponent<TextMeshProUGUI>();
                adsHintText.text = "滚轮调整枪械高度"; // 初始文字
                adsHintText.fontSize = 24f;
                adsHintText.color = new Color(1f, 1f, 1f, 0.8f);
                adsHintText.alignment = TextAlignmentOptions.Center;
                adsHintText.enableWordWrapping = false;

                Shadow shadow = textGO.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(2f, -2f);

                adsHintCanvas.SetActive(true);
                if (adsHintText != null) adsHintText.gameObject.SetActive(true);
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "Failed to create ADS hint UI");
                if (adsHintCanvas != null)
                {
                    Object.Destroy(adsHintCanvas);
                    adsHintCanvas = null;
                    adsHintText = null;
                }
            }
        }

        /// <summary>
        /// 销毁ADS提示UI
        /// </summary>
        private void DestroyAdsHintUI()
        {
            if (adsHintCanvas != null)
            {
                Object.Destroy(adsHintCanvas);
                adsHintCanvas = null;
                adsHintText = null;
            }
        }
        #endregion
    }
}