using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Duckov.Options;
using Duckov.UI;
using Duckov.Scenes;
using Dialogues;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.Compatibility;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 核心模块
    /// 负责第一人称模式的启用/禁用、生命周期管理、鼠标输入捕获、UI状态管理
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public partial class FirstPersonCameraController : MonoBehaviour
    {
        #region 序列化字段和公共属性
        [Header("Camera Settings")]
        [SerializeField] private float mouseSensitivityX = 0.10f;
        [SerializeField] private float mouseSensitivityY = 0.10f;

        // 倍镜灵敏度倍数（相对于普通灵敏度的倍数，基准为1，同时应用于水平和垂直）
        [SerializeField] private float scope1_2xSensitivity = 0.8f;
        [SerializeField] private float scope2xSensitivity = 0.6f;
        [SerializeField] private float scope4xSensitivity = 0.4f;
        [SerializeField] private float scope8xSensitivity = 0.2f;
        [SerializeField] private float fov = 70f;
        [SerializeField] private float cameraHeightOffset = 0.15f;
        [SerializeField] private float cameraForwardOffset = 0.12f;
        [SerializeField] private float cameraRightOffset = 0f;
        [SerializeField] private float nearClip = 0.05f;

        [Header("Performance")]
        [SerializeField] private float aimRecalcInterval = 0.02f;
        [SerializeField] private float aimAngleThreshold = 0.25f; // degrees
        [SerializeField] private float bodyAlignAngleThreshold = 1.5f; // degrees
        [SerializeField] private float raycastDistance = 250f;

        [Header("Controls")]
        public KeyCode toggleKey = KeyCode.F5;

        /// <summary>
        /// 单例实例（用于全局访问）
        /// </summary>
        public static FirstPersonCameraController Instance { get; private set; }

        /// <summary>
        /// 是否处于第一人称模式
        /// </summary>
        public bool IsFirstPersonMode => isFirstPersonMode;
        #endregion

        #region 私有字段
        private bool isFirstPersonMode;

        // 记录上一帧的武器引用，用于检测切换
        private ItemAgent_Gun lastFrameGun;
        private ItemAgent_MeleeWeapon lastFrameMelee;

        private bool useNewInputSystem;
        private CharacterMainControl mainCharacter;
        private CharacterModel characterModel;
        private Transform headSocket;
        private GameCamera gameCamera;
        private Camera mainCamera;
        private CinemachineVirtualCamera cinemachineVCam;
        private CameraArm cameraArm;
        private CinemachineBrain cinemachineBrain;

        private float pitch;
        private float yaw;
        private float pendingMouseX;
        private float pendingMouseY;
        private bool suppressNextMouseDelta;
        private float peekOffset;
        private bool wasUIBlocking;
        private float prevNearClip = -1f;
        private float prevSystemMouseSensitivity = -1f;
        private bool systemSensitivityPatched;

        private Coroutine alignAimRoutine;
        private Coroutine deferredInitRoutine;
        private int endOfFrameAlignEveryNFrames = 2;
        private int aimMarkerUpdateEveryNFrames = 2;
        private int raycastMask;
        private float lightSweepEndTime;
        private const float lightSweepDuration = 1.0f;
        private int obstructionCheckFrameCounter = 0;
        private const int obstructionCheckInterval = 10;
        #endregion

        #region 公共属性/方法（供补丁访问）
        public Camera MainCamera => mainCamera;
        public ItemAgent_Gun GetCurrentGun() => mainCharacter?.GetGun();
        #endregion

        #region Unity生命周期方法
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Reinitialize();
            LoadSensitivityFromOptions();
            LoadOffsetsFromOptions();
            LoadAntiBobFromOptions();
            LoadFovFromOptions();
            OptionsManager.OnOptionsChanged += OnOptionsChanged;
            HookHealthBarEvents();
            LoadToggleKeyFromOptions();

            InitializeAPI();
            try { EnsureCompass(); } catch { }

            try
            {
                StartCoroutine(InitializeDialogueBubbleCompatibilityDelayed());
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "初始化对话气泡兼容性支持失败");
            }

            if (!isFirstPersonMode)
            {
                try { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } catch { }
            }
        }

        private void OnDestroy()
        {
            OptionsManager.OnOptionsChanged -= OnOptionsChanged;
            UnhookHealthBarEvents();
            if (isFirstPersonMode) DisableFirstPerson();
            try { RestoreJumpInput(); } catch { }

            try
            {
                ScavDialogueBubbleCompatibility.OnDialogueBubbleReceived -= OnDialogueBubbleReceived;
                if (ScavDialogueBubbleCompatibility.Instance != null)
                {
                    ScavDialogueBubbleCompatibility.Instance.Cleanup();
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "清理对话气泡兼容性支持失败");
            }

            if (ReferenceEquals(Instance, this)) Instance = null;

            try
            {
                FirstPersonCamera.Utilities.ConfigManager.SaveAll();
                FPLogger.Log("控制器销毁前已保存所有配置数据");
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "保存配置数据失败");
            }

            try
            {
                if (systemSensitivityPatched)
                {
                    OptionsManager.MouseSensitivity = (prevSystemMouseSensitivity >= 0f ? prevSystemMouseSensitivity : 10f);
                    systemSensitivityPatched = false;
                }
            }
            catch { }
        }
        #endregion

        #region 初始化方法
        public void Reinitialize()
        {
            mainCharacter = CharacterMainControl.Main;
            characterModel = mainCharacter != null ? mainCharacter.GetComponentInChildren<CharacterModel>() : null;
            headSocket = characterModel != null ? characterModel.HelmatSocket : null;

            gameCamera = GameCamera.Instance;
            if (gameCamera != null)
            {
                mainCamera = gameCamera.renderCamera;
                cinemachineVCam = gameCamera.mainVCam;
                cameraArm = gameCamera.mianCameraArm;
                cinemachineBrain = gameCamera.brain;
            }

            useNewInputSystem = Keyboard.current != null && Mouse.current != null;

            if (mainCamera != null)
            {
                var rot = mainCamera.transform.eulerAngles;
                pitch = rot.x > 180 ? rot.x - 360f : rot.x;
                yaw = rot.y;
            }

            int mask = 0;
            int lDamage = LayerMask.NameToLayer("DamageReceiver"); if (lDamage >= 0) mask |= (1 << lDamage);
            int lEnemy = LayerMask.NameToLayer("Enemy"); if (lEnemy >= 0) mask |= (1 << lEnemy);
            int lNPC = LayerMask.NameToLayer("NPC"); if (lNPC >= 0) mask |= (1 << lNPC);
            int lWall = LayerMask.NameToLayer("Wall"); if (lWall >= 0) mask |= (1 << lWall);
            int lGround = LayerMask.NameToLayer("Ground"); if (lGround >= 0) mask |= (1 << lGround);
            int lEnvironment = LayerMask.NameToLayer("Environment"); if (lEnvironment >= 0) mask |= (1 << lEnvironment);
            raycastMask = (mask == 0 ? Physics.DefaultRaycastLayers : mask);
        }
        #endregion

        #region Update循环
        private void Update()
        {
            if (isFirstPersonMode)
            {
                bool notInGameplay = false;
                try
                {
                    if (CharacterMainControl.Main == null) notInGameplay = true;
                    else if (GameCamera.Instance == null) notInGameplay = true;
                }
                catch { notInGameplay = true; }

                if (notInGameplay)
                {
                    try { DisableFirstPerson(); } catch { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                    isFirstPersonMode = false;
                }
            }

            bool uiBlocking = IsUiBlocking();
            if (!uiBlocking && Input.GetKeyDown(toggleKey)) ToggleView();

            if (isFirstPersonMode)
            {
                // 武器切换检测（必须在最前，确保准星及时更新）
                CheckWeaponSwitchAndStopInspect();

                UpdatePeekInput(uiBlocking);
                UpdateUnifiedInspectInput(uiBlocking);

                // 跳跃处理
                try
                {
                    if (IsJumpEnabled())
                    {
                        if (useNewInputSystem && (jumpAction == null || !dashBindingModified))
                        {
                            SetupJumpInput();
                        }
                    }
                    else
                    {
                        if (jumpAction != null || dashBindingModified)
                        {
                            RestoreJumpInput();
                        }
                    }

                    if (IsJumpEnabled() && !uiBlocking)
                    {
                        bool shouldUseOldInputSystem = !useNewInputSystem || (useNewInputSystem && jumpAction == null);
                        if (shouldUseOldInputSystem && Input.GetKeyDown(KeyCode.Space))
                        {
                            if (useNewInputSystem && jumpAction == null)
                            {
                                FPLogger.LogWarning("新输入系统下jumpAction未创建，使用旧输入系统兜底检测跳跃输入（空格键）");
                            }
                            else
                            {
                                FPLogger.Log("旧输入系统检测到跳跃输入（空格键）");
                            }
                            DoJump();
                        }
                    }
                }
                catch { }

                bool mousePressed = false;
                try
                {
                    if (useNewInputSystem)
                    {
                        var m = Mouse.current;
                        if (m != null) mousePressed = m.leftButton.isPressed;
                    }
                    else
                    {
                        mousePressed = Input.GetMouseButton(0);
                    }
                }
                catch { }

                if (!mousePressed)
                {
                    HandleMouseReleaseRecoilReset();
                }

                // 光标管理
                if (uiBlocking)
                {
                    if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    if (wasUIBlocking && Cursor.lockState != CursorLockMode.Locked)
                    { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; suppressNextMouseDelta = true; }
                    else if (Input.GetKeyDown(KeyCode.Escape) && Cursor.lockState == CursorLockMode.Locked)
                    { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                    else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
                    { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; suppressNextMouseDelta = true; }
                }

                if (Time.unscaledTime < lightSweepEndTime)
                {
                    try { LightSweepTransparentSomeBars(32); } catch { }
                }
                else if (Time.unscaledTime >= nextHealthBarSweepTime)
                {
                    nextHealthBarSweepTime = Time.unscaledTime + 1f;
                    try { LightSweepTransparentSomeBars(24); } catch { }
                }
            }
            wasUIBlocking = uiBlocking;
        }

        private void LateUpdate()
        {
            if (!isFirstPersonMode)
            {
                try
                {
                    if (compassCanvas != null && compassCanvas.gameObject != null && compassCanvas.gameObject.activeSelf)
                    {
                        compassCanvas.gameObject.SetActive(false);
                    }
                    if (staminaCanvas != null && staminaCanvas.gameObject != null && staminaCanvas.gameObject.activeSelf)
                    {
                        staminaCanvas.gameObject.SetActive(false);
                    }
                }
                catch { }
                return;
            }

            try
            {
                int v = OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableAimOcclusionFadeKey, 1);
                if (v == 1) DisableAimOcclusionFade(); else RestoreAimOcclusionFade();
            }
            catch { }

            if (mainCharacter == null || mainCamera == null)
            {
                Reinitialize();
                if (mainCamera == null) return;
            }

            bool uiBlocking = IsUiBlocking();
            UpdateCameraPosition();
            UpdateCameraRotation(uiBlocking);

            if (!uiBlocking)
            {
                UpdateAimAndCrosshair();
                AlignBodyToCamera();
            }

            UpdateAdsWeaponPlacement();
            UpdateGunShake(); // 枪械抖动

            obstructionCheckFrameCounter++;
            if (obstructionCheckFrameCounter >= obstructionCheckInterval)
            {
                obstructionCheckFrameCounter = 0;
                try { HideFirstPersonObstructions(); } catch { }
            }

            try { LateUpdateCompass(); } catch { }
            try { LateUpdateStaminaBar(); } catch { }
            try { LateUpdateDialogueBubble(); } catch { }
            try { LaserPatch.LateUpdateHitMarkers(); } catch { }
        }
        #endregion

        #region 视图切换方法
        public void ToggleView()
        {
            if (!isFirstPersonMode) EnableFirstPerson(); else DisableFirstPerson();
            isFirstPersonMode = !isFirstPersonMode;
        }

        public void SetFirstPerson(bool enable)
        {
            if (enable && !isFirstPersonMode) { EnableFirstPerson(); isFirstPersonMode = true; }
            else if (!enable && isFirstPersonMode) { DisableFirstPerson(); isFirstPersonMode = false; }
        }

        public void SetToggleKey(KeyCode key) => toggleKey = key;
        public KeyCode GetToggleKey() => toggleKey;

        public void SwitchToThirdPersonTopDown()
        {
            if (isFirstPersonMode)
            {
                DisableFirstPerson();
                isFirstPersonMode = false;
                try { FirstPersonCameraAPI.InvokeSwitchToThirdPersonTopDown(); } catch { }
            }
        }

        private void InitializeAPI()
        {
            FirstPersonCameraAPI.GetIsFirstPersonMode = () => isFirstPersonMode;
            FirstPersonCameraAPI.GetToggleKey = () => toggleKey;
            FirstPersonCameraAPI.SwitchToThirdPersonTopDown = SwitchToThirdPersonTopDown;
        }
        #endregion

        #region UI状态检查
        private bool IsUiBlocking()
        {
            try
            {
                if (GameManager.Paused) return true;
                if (View.ActiveView != null) return true;
                if (DialogueUI.Active) return true;
                if (SceneLoader.IsSceneLoading) return true;
                if (IsMouseReleasedExternally) return true;
            }
            catch { }
            return false;
        }
        #endregion

        #region 第一人称模式启用/禁用
        private void EnableFirstPerson()
        {
            if (cinemachineVCam != null) cinemachineVCam.enabled = false;
            if (cameraArm != null) cameraArm.enabled = false;
            if (cinemachineBrain != null) cinemachineBrain.enabled = false;

            if (mainCamera != null)
            {
                baseFov = fov;
                if (currentScopeTypeID == -1)
                {
                    mainCamera.fieldOfView = fov;
                }
                try
                {
                    if (prevNearClip < 0f) prevNearClip = mainCamera.nearClipPlane;
                    mainCamera.nearClipPlane = Mathf.Clamp(nearClip, 0.03f, 0.1f);
                }
                catch { }
            }

            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; suppressNextMouseDelta = true;
            DisablePostProcessingBlurEffects();
            HideFirstPersonObstructions();
            ApplyFpsCullingProfile();
            try { UpdateFogOfWarState(); } catch { }
            try { CreateAdsHintUI(); } catch { }

            lightSweepEndTime = Time.unscaledTime + lightSweepDuration;

            if (mainCharacter != null)
            {
                yaw = mainCharacter.transform.eulerAngles.y;
                pitch = 0f;
                try { mainCharacter.OnShootEvent += OnCharacterShoot; } catch { }
            }

            antiBobInitialized = false;
            CreatePeekColliderProxy();

            try
            {
                if (!systemSensitivityPatched)
                {
                    prevSystemMouseSensitivity = OptionsManager.MouseSensitivity;
                    OptionsManager.MouseSensitivity = 0f;
                    systemSensitivityPatched = true;
                }
            }
            catch { }

            if (alignAimRoutine == null) alignAimRoutine = StartCoroutine(AlignAimRoutine());
            if (deferredInitRoutine == null) deferredInitRoutine = StartCoroutine(DeferredFirstPersonInit());
            try { SetupJumpInput(); } catch { }
        }

        private void DisableFirstPerson()
        {
            try { StopWeaponInspect(); } catch { }
            try { StopMeleeInspect(); } catch { }
            try { StopInventoryItemInspect(); } catch { }
            try { StopUIItemInspect(); } catch { }

            if (cinemachineVCam != null) cinemachineVCam.enabled = true;
            if (cameraArm != null) cameraArm.enabled = true;
            if (cinemachineBrain != null) cinemachineBrain.enabled = true;

            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            RestorePostProcessingBlurEffects();
            RestoreCullingProfile();
            RestoreAimOcclusionFade();
            try { CleanupFogOfWarControl(); } catch { }

            try { if (mainCamera != null && prevNearClip > 0f) { mainCamera.nearClipPlane = prevNearClip; prevNearClip = -1f; } } catch { }
            RestoreFirstPersonObstructions();
            RestoreEnemyHealthBarTransparency();
            RestoreEnemyHealthBarFlags();

            try { if (mainCharacter != null) mainCharacter.OnShootEvent -= OnCharacterShoot; } catch { }

            DestroyPeekColliderProxy();
            try { DestroyAdsHintUI(); } catch { }

            if (alignAimRoutine != null) { try { StopCoroutine(alignAimRoutine); } catch { } alignAimRoutine = null; }
            if (deferredInitRoutine != null) { try { StopCoroutine(deferredInitRoutine); } catch { } deferredInitRoutine = null; }

            try
            {
                if (systemSensitivityPatched)
                {
                    OptionsManager.MouseSensitivity = (prevSystemMouseSensitivity >= 0f ? prevSystemMouseSensitivity : 10f);
                    systemSensitivityPatched = false;
                }
            }
            catch { }

            try { RestoreJumpInput(); } catch { }
            RestoreAdsCrosshair();
        }
        #endregion

        #region 对话气泡兼容性支持
        private System.Collections.IEnumerator InitializeDialogueBubbleCompatibilityDelayed()
        {
            yield return new WaitForSeconds(1f);
            try
            {
                ScavDialogueBubbleCompatibility.Instance.Initialize();
                ScavDialogueBubbleCompatibility.OnDialogueBubbleReceived += OnDialogueBubbleReceived;
                if (ScavDialogueBubbleCompatibility.Instance.IsAvailable)
                {
                    FPLogger.Log("对话气泡兼容性支持已启用");
                }
                else
                {
                    FPLogger.Log("对话气泡兼容性支持不可用（RandomNpc mod未安装）");
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "初始化对话气泡兼容性支持时出错");
            }
        }

        private void OnDialogueBubbleReceived(string message)
        {
            try
            {
                if (isFirstPersonMode)
                {
                    ShowDialogueBubble(message);
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "处理对话气泡消息时出错");
            }
        }
        #endregion

        #region 辅助协程
        private System.Collections.IEnumerator AlignAimRoutine()
        {
            while (isFirstPersonMode)
            {
                yield return new WaitForEndOfFrame();
                if (!isFirstPersonMode) break;
                if (endOfFrameAlignEveryNFrames <= 1 || (Time.frameCount % endOfFrameAlignEveryNFrames) == 0)
                {
                    try
                    {
                        EnsureCaches();
                        SetAimScreenCenterOnly();
                        if (cachedAimMarker == null) cachedAimMarker = Object.FindObjectOfType<AimMarker>();
                        if (cachedAimMarker != null)
                        {
                            var c = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                            cachedAimMarker.SetAimMarkerPosScreenSpace(c);
                        }
                    }
                    catch { }
                }
            }
        }

        private System.Collections.IEnumerator DeferredFirstPersonInit()
        {
            yield return null;
            if (!isFirstPersonMode) yield break;
            try { HideFirstPersonObstructions(); } catch { }

            yield return null;
            if (!isFirstPersonMode) yield break;
            lightSweepEndTime = Time.unscaledTime + lightSweepDuration;

            yield return null;
            if (!isFirstPersonMode) yield break;
            try { DisablePostProcessingBlurEffects(); } catch { }
        }

        /// <summary>
        /// 检测武器切换，停止检视并强制重建准星
        /// </summary>
        private void CheckWeaponSwitchAndStopInspect()
        {
            if (!isFirstPersonMode || mainCharacter == null) return;

            var currentGun = mainCharacter.GetGun();
            var currentMelee = mainCharacter.GetMeleeWeapon();

            // 检视停止逻辑
            if (isInspectingWeapon)
            {
                if (inspectingWeapon != null)
                {
                    if (currentGun != inspectingWeapon)
                        StopWeaponInspect();
                }
                else
                {
                    isInspectingWeapon = false;
                    if (weaponInspectCoroutine != null)
                    {
                        StopCoroutine(weaponInspectCoroutine);
                        weaponInspectCoroutine = null;
                    }
                }
            }

            if (isInspectingMelee)
            {
                if (currentGun != null)
                {
                    StopMeleeInspect();
                }
                else if (inspectingMelee != null)
                {
                    if (currentMelee != inspectingMelee)
                        StopMeleeInspect();
                }
                else
                {
                    isInspectingMelee = false;
                    if (meleeInspectCoroutine != null)
                    {
                        StopCoroutine(meleeInspectCoroutine);
                        meleeInspectCoroutine = null;
                    }
                }
            }

            // 武器切换检测，强制重建准星
            if (lastFrameGun != currentGun || lastFrameMelee != currentMelee)
            {
                ForceRecreateAdsMarker();
                lastFrameGun = currentGun;
                lastFrameMelee = currentMelee;
            }
        }

        private void UpdateUnifiedInspectInput(bool uiBlocking)
        {
            if (uiBlocking) return;

            KeyCode inspectKey = KeyCode.H;
            try
            {
                inspectKey = OptionsHelper.LoadKeyCode(
                    OptionsUIConstants.InspectKeyCodeKey,
                    OptionsHelper.LoadKeyCode(OptionsUIConstants.WeaponInspectKeyCodeKey, KeyCode.H));
            }
            catch
            {
                inspectKey = KeyCode.H;
            }

            if (lastInspectKeyFrame == Time.frameCount) return;

            bool keyPressed = false;
            try
            {
                if (useNewInputSystem)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        keyPressed = GetKeyPressedThisFrame(keyboard, inspectKey);
                    }
                }
                else
                {
                    keyPressed = Input.GetKeyDown(inspectKey);
                }
            }
            catch { }

            if (keyPressed)
            {
                lastInspectKeyFrame = Time.frameCount;
                if (mainCharacter != null)
                {
                    var melee = mainCharacter.GetMeleeWeapon();
                    if (melee != null)
                    {
                        StartMeleeInspect();
                        return;
                    }
                    var gun = mainCharacter.GetGun();
                    if (gun != null)
                    {
                        StartWeaponInspect();
                        return;
                    }
                }
            }
        }
        #endregion
    }
}