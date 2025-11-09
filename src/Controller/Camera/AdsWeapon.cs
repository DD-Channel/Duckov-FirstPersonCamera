using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - ADS武器位移系统模块
    /// 负责处理ADS状态下武器的位置调整、武器高度偏移管理和ADS提示UI
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
        [SerializeField] private float adsWeaponLerpSpeed = 24f;
        
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
        
        /// <summary>
        /// Y轴偏移调整步长（每次滚轮调整的增量）
        /// </summary>
        [SerializeField] private float adsOffsetYStep = 0.005f;
        
        /// <summary>
        /// Y轴偏移最小值（米）
        /// </summary>
        [SerializeField] private float adsOffsetYDeltaMin = -0.25f;
        
        /// <summary>
        /// Y轴偏移最大值（米）
        /// </summary>
        [SerializeField] private float adsOffsetYDeltaMax = 0.25f;
        
        /// <summary>
        /// 当前Y轴偏移增量（用于调整武器高度）
        /// </summary>
        private float adsYOffsetDelta = 0f;
        
        /// <summary>
        /// 每把枪的独立高度偏移值映射（TypeID -> 偏移值）
        /// </summary>
        private System.Collections.Generic.Dictionary<int, float> gunYOffsetMap = 
            new System.Collections.Generic.Dictionary<int, float>();
        
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
        private float currentAdsSpeed = 12f;
        
        /// <summary>
        /// ADS速度加速倍数（用于加快ADS过渡速度）
        /// </summary>
        private const float ADS_SPEED_MULTIPLIER = 6.25f;
        
        /// <summary>
        /// 默认ADS速度（当无法获取武器速度时使用）
        /// </summary>
        private const float DEFAULT_ADS_SPEED = 12f;
        
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

        #region 武器高度偏移系统
        /// <summary>
        /// 保存指定武器的Y轴偏移值到本地
        /// </summary>
        /// <param name="gunTypeID">武器TypeID</param>
        /// <param name="offset">Y轴偏移值</param>
        private void SaveGunYOffset(int gunTypeID, float offset)
        {
            try
            {
                // 更新内存中的值
                gunYOffsetMap[gunTypeID] = offset;
                
                // 保存到本地
                string key = $"FirstPersonCamera_GunYOffset_{gunTypeID}";
                FirstPersonCamera.Utilities.ConfigManager.Save<float>(key, offset);
            }
            catch
            {
                // 保存失败时静默处理
            }
        }
        
        /// <summary>
        /// 从本地加载指定武器的Y轴偏移值
        /// </summary>
        /// <param name="gunTypeID">武器TypeID</param>
        /// <returns>Y轴偏移值，如果不存在则返回0</returns>
        private float LoadGunYOffset(int gunTypeID)
        {
            try
            {
                // 先检查内存中是否有值
                if (gunYOffsetMap.TryGetValue(gunTypeID, out float cachedValue))
                {
                    return cachedValue;
                }
                
                // 从本地加载
                string key = $"FirstPersonCamera_GunYOffset_{gunTypeID}";
                float savedValue = FirstPersonCamera.Utilities.ConfigManager.Load<float>(key, 0f, keepCurrentOnError: true);
                
                // 更新内存中的值
                gunYOffsetMap[gunTypeID] = savedValue;
                
                return savedValue;
            }
            catch
            {
                return 0f;
            }
        }
        
        /// <summary>
        /// 初始化武器高度偏移系统
        /// </summary>
        private void InitializeGunYOffsetSystem()
        {
            try
            {
                gunYOffsetMap.Clear();
                currentGunTypeID = -1;
            }
            catch
            {
                // 初始化失败时静默处理
            }
        }
        #endregion

        #region ADS武器位移管理
        /// <summary>
        /// 更新ADS武器位置
        /// 当进入ADS状态时，将武器移动到相机下方，保持与相机同朝向
        /// 支持滚轮调整武器高度，每把武器独立保存偏移值
        /// </summary>
        private void UpdateAdsWeaponPlacement()
        {
            if (mainCharacter == null || mainCamera == null) return;
            
            // 如果正在检视武器，不更新ADS位置（让检视动画控制武器位置）
            // 武器切换检测在Update循环中的CheckWeaponSwitchAndStopInspect()处理
            if (isInspectingWeapon || isInspectingMelee)
            {
                return;
            }
            
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
            
            // 检测武器切换，加载对应的高度偏移值
            int gunTypeID = -1;
            try
            {
                if (gun.Item != null)
                {
                    gunTypeID = gun.Item.TypeID;
                }
            }
            catch
            {
                // 获取武器TypeID失败时使用默认值
            }
            
            // 如果武器切换了，加载对应的偏移值
            if (gunTypeID != -1 && gunTypeID != currentGunTypeID)
            {
                // 保存当前武器的偏移值（如果有）
                if (currentGunTypeID != -1)
                {
                    SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                }
                
                // 切换到新武器，加载对应的偏移值
                currentGunTypeID = gunTypeID;
                adsYOffsetDelta = LoadGunYOffset(gunTypeID);
            }
            else if (gunTypeID == -1 && currentGunTypeID != -1)
            {
                // 武器被移除，保存当前武器的偏移值
                SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                currentGunTypeID = -1;
            }

            // 获取ADS状态
            float ads = 0f;
            try
            {
                ads = gun.AdsValue;
            }
            catch
            {
                ads = 0f;
            }
            
            // 检测右键按下状态
            bool rmbPressed = false;
            try
            {
                if (useNewInputSystem)
                {
                    var m = Mouse.current;
                    if (m != null)
                    {
                        rmbPressed = m.rightButton.isPressed;
                    }
                }
                else
                {
                    rmbPressed = Input.GetMouseButton(1);
                }
            }
            catch
            {
                // 检测失败时使用默认值
            }

            // 更新ADS状态
            bool wasAdsEngaged = adsEngaged;
            if (rmbPressed || ads > ADS_ACTIVATION_THRESHOLD)
            {
                adsEngaged = true;
                adsReleaseTimer = adsReleaseGrace;
                
                // 如果刚进入ADS状态，开始平滑过渡
                if (!wasAdsEngaged)
                {
                    adsIsSmoothingToTarget = true;
                }
                
                // 获取武器的瞄准速度（用于ADS平滑和FOV平滑）
                try
                {
                    currentAdsSpeed = gun.AdsSpeed * ADS_SPEED_MULTIPLIER;
                    if (currentAdsSpeed <= 0f)
                    {
                        // 防止除零，默认速度也加快
                        currentAdsSpeed = DEFAULT_ADS_SPEED * ADS_SPEED_MULTIPLIER;
                    }
                }
                catch
                {
                    currentAdsSpeed = DEFAULT_ADS_SPEED * ADS_SPEED_MULTIPLIER;
                }
                
                // 检查倍镜并更新FOV（设置目标值）
                UpdateScopeFOV(gun);
            }
            else if (adsReleaseTimer > 0f)
            {
                // 在释放延迟期间，继续更新
                adsReleaseTimer -= Time.unscaledDeltaTime;
                if (adsReleaseTimer <= 0f)
                {
                    adsEngaged = false;
                    adsIsSmoothingToTarget = false; // 取消ADS时重置平滑状态
                    // 退出ADS时恢复基础FOV（平滑过渡）
                    RestoreBaseFOV();
                }
                else
                {
                    // 在延迟期间继续平滑更新FOV（跟随AdsValue）
                    if (gun != null)
                    {
                        UpdateScopeFOVSmooth(gun);
                    }
                }
            }
            else
            {
                adsIsSmoothingToTarget = false; // 不在ADS状态时重置
                // 不在ADS状态时恢复基础FOV（跟随AdsValue）
                RestoreBaseFOV();
                if (gun != null)
                {
                    UpdateScopeFOVSmooth(gun);
                }
                else
                {
                    // 如果没有枪械，直接恢复基础FOV
                    if (mainCamera != null)
                    {
                        mainCamera.fieldOfView = baseFov;
                        currentScopeFovMultiplier = 1f;
                        scopeFovMultiplier = 1f;
                    }
                }
            }
            
            // 无论是否在ADS状态，都更新FOV缩放（跟随AdsValue）
            if (gun != null)
            {
                UpdateScopeFOVSmooth(gun);
            }

            bool shouldAdsPlace = adsEngaged;

            // 功能开关：ADS 武器位移
            bool adsOffsetEnabled = true;
            try
            {
                adsOffsetEnabled = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(
                    FirstPersonOptionsUI.EnableAdsOffsetKey, 1) == 1;
            }
            catch
            {
                adsOffsetEnabled = true;
            }
            
            // 更新ADS提示显示（仅当功能开启且真正开镜时显示）
            UpdateAdsHint(adsOffsetEnabled && shouldAdsPlace && 
                         (ads > ADS_ACTIVATION_THRESHOLD || rmbPressed));
            
            if (!adsOffsetEnabled)
            {
                RestoreAdsWeaponIfNeeded();
                return;
            }
            
            if (!shouldAdsPlace)
            {
                RestoreAdsWeaponIfNeeded();
                return;
            }

            // 缓存武器的原始位姿（如果还没有缓存或武器切换了）
            if (!adsCachedOrig || adsLastGunTransform != gunTf)
            {
                adsOriginalLocalPos = gunTf.localPosition;
                adsOriginalLocalRot = gunTf.localRotation;
                adsLastGunTransform = gunTf;
                adsCachedOrig = true;
            }

            // 滚轮调节Y偏移（仅ADS）
            float scroll = 0f;
            try
            {
                if (useNewInputSystem)
                {
                    var mouse = Mouse.current;
                    if (mouse != null)
                    {
                        // 通常每个刻度±120
                        scroll = mouse.scroll.ReadValue().y;
                    }
                }
                else
                {
                    scroll = Input.GetAxis("Mouse ScrollWheel") * MOUSE_SCROLL_MULTIPLIER;
                }
            }
            catch
            {
                // 获取滚轮输入失败时使用默认值
            }
            
            if (Mathf.Abs(scroll) > SCROLL_INPUT_THRESHOLD)
            {
                float delta = Mathf.Sign(scroll) * adsOffsetYStep;
                adsYOffsetDelta = Mathf.Clamp(adsYOffsetDelta + delta, 
                    adsOffsetYDeltaMin, adsOffsetYDeltaMax);
                
                // 保存当前武器的偏移值
                if (currentGunTypeID != -1)
                {
                    SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
                }
            }

            // 计算目标位姿（世界空间）：相机下方一点并前探一点，保持与相机同朝向
            Vector3 camPos = mainCamera.transform.position;
            Quaternion camRot = mainCamera.transform.rotation;
            Vector3 camSpaceOffset = adsWeaponCamSpaceOffset;
            camSpaceOffset.y += adsYOffsetDelta;
            Vector3 targetWorldPos = camPos + camRot * camSpaceOffset;
            Quaternion targetWorldRot = camRot;

            // 将目标位姿转换到武器父节点的局部空间，避免骨骼动画/父节点驱动造成的偏移漂移
            Transform parentTf = gunTf.parent;
            if (parentTf != null)
            {
                Vector3 targetLocalPos = parentTf.InverseTransformPoint(targetWorldPos);
                Quaternion targetLocalRot = Quaternion.Inverse(parentTf.rotation) * targetWorldRot;
                
                // 举枪时先平滑移动到目标位置，到达后再直接设置（避免旋转抖动）
                if (adsIsSmoothingToTarget && adsWeaponUseDirectSet)
                {
                    // 平滑过渡阶段：使用武器的瞄准速度进行插值
                    // 使用指数衰减公式，速度基于武器的AdsSpeed
                    float lerpSpeed = currentAdsSpeed;
                    float t = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
                    gunTf.localPosition = Vector3.Lerp(gunTf.localPosition, targetLocalPos, t);
                    gunTf.localRotation = Quaternion.Slerp(gunTf.localRotation, targetLocalRot, t);
                    
                    // 检查是否已经到达目标位置
                    float posDist = Vector3.Distance(gunTf.localPosition, targetLocalPos);
                    float rotDist = Quaternion.Angle(gunTf.localRotation, targetLocalRot);
                    if (posDist < ADS_SMOOTH_THRESHOLD && rotDist < ADS_ROTATION_THRESHOLD)
                    {
                        // 到达目标，切换到直接设置模式
                        adsIsSmoothingToTarget = false;
                        gunTf.localPosition = targetLocalPos;
                        gunTf.localRotation = targetLocalRot;
                    }
                }
                else if (adsWeaponUseDirectSet)
                {
                    // 直接设置位置和旋转（已到达目标或不需要平滑）
                    gunTf.localPosition = targetLocalPos;
                    gunTf.localRotation = targetLocalRot;
                }
                else
                {
                    // 使用平滑插值（使用武器的瞄准速度）
                    float lerpSpeed = currentAdsSpeed;
                    float t = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
                    gunTf.localPosition = Vector3.Lerp(gunTf.localPosition, targetLocalPos, t);
                    gunTf.localRotation = Quaternion.Slerp(gunTf.localRotation, targetLocalRot, t);
                }
            }
            else
            {
                // 无父节点时，也使用相同的逻辑
                if (adsIsSmoothingToTarget && adsWeaponUseDirectSet)
                {
                    // 平滑过渡阶段
                    float t = 1f - Mathf.Exp(-adsWeaponLerpSpeed * Time.unscaledDeltaTime);
                    gunTf.position = Vector3.Lerp(gunTf.position, targetWorldPos, t);
                    gunTf.rotation = Quaternion.Slerp(gunTf.rotation, targetWorldRot, t);
                    
                    // 检查是否已经到达目标位置
                    float posDist = Vector3.Distance(gunTf.position, targetWorldPos);
                    float rotDist = Quaternion.Angle(gunTf.rotation, targetWorldRot);
                    if (posDist < ADS_SMOOTH_THRESHOLD && rotDist < ADS_ROTATION_THRESHOLD)
                    {
                        // 到达目标，切换到直接设置模式
                        adsIsSmoothingToTarget = false;
                        gunTf.position = targetWorldPos;
                        gunTf.rotation = targetWorldRot;
                    }
                }
                else if (adsWeaponUseDirectSet)
                {
                    // 直接设置位置和旋转
                    gunTf.position = targetWorldPos;
                    gunTf.rotation = targetWorldRot;
                }
                else
                {
                    // 使用平滑插值
                    float t = 1f - Mathf.Exp(-adsWeaponLerpSpeed * Time.unscaledDeltaTime);
                    gunTf.position = Vector3.Lerp(gunTf.position, targetWorldPos, t);
                    gunTf.rotation = Quaternion.Slerp(gunTf.rotation, targetWorldRot, t);
                }
            }
        }

        /// <summary>
        /// 恢复ADS武器到原始位置（如果需要）
        /// </summary>
        /// <param name="resetOffset">是否重置Y偏移值</param>
        private void RestoreAdsWeaponIfNeeded(bool resetOffset = false)
        {
            if (!adsCachedOrig || adsLastGunTransform == null) return;
            
            // 平滑恢复到原本的局部位姿
            float t = 1f - Mathf.Exp(-adsWeaponLerpSpeed * Time.unscaledDeltaTime);
            adsLastGunTransform.localPosition = Vector3.Lerp(adsLastGunTransform.localPosition, 
                adsOriginalLocalPos, t);
            adsLastGunTransform.localRotation = Quaternion.Slerp(adsLastGunTransform.localRotation, 
                adsOriginalLocalRot, t);

            // 若已经非常接近原位，则清理缓存
            if ((adsLastGunTransform.localPosition - adsOriginalLocalPos).sqrMagnitude < 1e-6f)
            {
                adsCachedOrig = false;
                adsLastGunTransform = null;
                if (resetOffset)
                {
                    adsYOffsetDelta = 0f; // 只在明确要求时重置Y偏移
                    
                    // 保存重置后的值
                    if (currentGunTypeID != -1)
                    {
                        SaveGunYOffset(currentGunTypeID, adsYOffsetDelta);
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
            // 功能开关：ADS 武器位移
            bool adsOffsetEnabled = true;
            try
            {
                adsOffsetEnabled = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(
                    FirstPersonOptionsUI.EnableAdsOffsetKey, 1) == 1;
            }
            catch
            {
                adsOffsetEnabled = true;
            }
            
            // 创建UI（如果还没有，且在第一人称模式下）
            if (adsHintCanvas == null && isFirstPersonMode)
            {
                CreateAdsHintUI();
            }
            
            if (adsHintCanvas == null || adsHintText == null) return;
            
            // 显示或隐藏提示文字
            bool shouldShow = adsOffsetEnabled && show && isFirstPersonMode;
            if (adsHintText.gameObject.activeSelf != shouldShow)
            {
                adsHintText.gameObject.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// 创建ADS提示UI
        /// </summary>
        private void CreateAdsHintUI()
        {
            if (adsHintCanvas != null) return; // 如果已经创建，不再创建

            try
            {
                // 创建Canvas
                adsHintCanvas = new GameObject("AdsHintCanvas");
                Object.DontDestroyOnLoad(adsHintCanvas); // 防止场景切换时被销毁
                Canvas canvas = adsHintCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // 确保在最上层
                CanvasScaler scaler = adsHintCanvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                adsHintCanvas.AddComponent<GraphicRaycaster>();

                // 创建文本对象
                GameObject textGO = new GameObject("AdsHintText");
                textGO.transform.SetParent(adsHintCanvas.transform, false);
                RectTransform rectTransform = textGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.anchoredPosition = new Vector2(0f, 120f); // 距离底部120像素
                rectTransform.sizeDelta = new Vector2(600f, 40f);

                adsHintText = textGO.AddComponent<TextMeshProUGUI>();
                adsHintText.text = "滚轮可以调整枪械高度";
                adsHintText.fontSize = 24f;
                adsHintText.color = new Color(1f, 1f, 1f, 0.8f);
                adsHintText.alignment = TextAlignmentOptions.Center;
                adsHintText.enableWordWrapping = false;

                // 添加阴影效果以提高可读性
                Shadow shadow = textGO.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(2f, -2f);

                adsHintCanvas.SetActive(true); // 创建后默认激活，由UpdateAdsHint控制显示
                if (adsHintText != null)
                {
                    adsHintText.gameObject.SetActive(true);
                }
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

