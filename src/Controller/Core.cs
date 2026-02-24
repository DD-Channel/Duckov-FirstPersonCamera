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
        // 例如：0.8表示开镜时灵敏度是普通灵敏度的80%
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
        /// <summary>
        /// 是否处于第一人称模式（内部状态）
        /// </summary>
        private bool isFirstPersonMode;
        
        /// <summary>
        /// 是否使用新的输入系统（Unity Input System）
        /// </summary>
        private bool useNewInputSystem;

        /// <summary>
        /// 游戏引用：主角色控制器
        /// </summary>
        private CharacterMainControl mainCharacter;
        
        /// <summary>
        /// 游戏引用：角色模型
        /// </summary>
        private CharacterModel characterModel;
        
        /// <summary>
        /// 游戏引用：头部插槽位置
        /// </summary>
        private Transform headSocket;
        
        /// <summary>
        /// 游戏引用：游戏相机系统
        /// </summary>
        private GameCamera gameCamera;
        
        /// <summary>
        /// 游戏引用：主相机
        /// </summary>
        private Camera mainCamera;
        
        /// <summary>
        /// 游戏引用：Cinemachine虚拟相机
        /// </summary>
        private CinemachineVirtualCamera cinemachineVCam;
        
        /// <summary>
        /// 游戏引用：相机臂
        /// </summary>
        private CameraArm cameraArm;
        
        /// <summary>
        /// 游戏引用：Cinemachine大脑
        /// </summary>
        private CinemachineBrain cinemachineBrain;

        /// <summary>
        /// 旋转状态：俯仰角（pitch）
        /// </summary>
        private float pitch;
        
        /// <summary>
        /// 旋转状态：偏航角（yaw）
        /// </summary>
        private float yaw;

        /// <summary>
        /// 待处理的鼠标X增量
        /// </summary>
        private float pendingMouseX;
        
        /// <summary>
        /// 待处理的鼠标Y增量
        /// </summary>
        private float pendingMouseY;
        
        /// <summary>
        /// 是否抑制下一次鼠标增量（用于避免模式切换时的跳跃）
        /// </summary>
        private bool suppressNextMouseDelta;

        /// <summary>
        /// Peek偏头状态：偏移量（-1到+1，左到右）
        /// </summary>
        private float peekOffset;

        /// <summary>
        /// 光标/UI状态：上次是否被UI阻挡
        /// </summary>
        private bool wasUIBlocking;

        /// <summary>
        /// 近裁剪面恢复：之前保存的近裁剪面值
        /// </summary>
        private float prevNearClip = -1f;
        
        /// <summary>
        /// 系统鼠标灵敏度恢复：之前保存的系统鼠标灵敏度
        /// </summary>
        private float prevSystemMouseSensitivity = -1f;
        
        /// <summary>
        /// 系统鼠标灵敏度是否已修补
        /// </summary>
        private bool systemSensitivityPatched;

        /// <summary>
        /// 协程：瞄准对齐协程
        /// </summary>
        private Coroutine alignAimRoutine;
        
        /// <summary>
        /// 协程：延迟初始化协程
        /// </summary>
        private Coroutine deferredInitRoutine;
        
        /// <summary>
        /// 每N帧执行一次帧末对齐（用于降低开销）
        /// </summary>
        private int endOfFrameAlignEveryNFrames = 2;
        
        /// <summary>
        /// 每N帧更新一次准星UI（用于降低开销）
        /// </summary>
        private int aimMarkerUpdateEveryNFrames = 2;
        
        /// <summary>
        /// 缓存的射线检测掩码（用于瞄准射线检测）
        /// </summary>
        private int raycastMask;
        
        /// <summary>
        /// 轻量扫描结束时间（用于轻量级血条扫描窗口）
        /// </summary>
        private float lightSweepEndTime;
        
        /// <summary>
        /// 轻量扫描持续时间（秒）
        /// </summary>
        private const float lightSweepDuration = 1.0f;
        
        /// <summary>
        /// 遮挡检查帧计数器（用于降低检查频率）
        /// </summary>
        private int obstructionCheckFrameCounter = 0;
        
        /// <summary>
        /// 遮挡检查间隔（每N帧检查一次是否需要隐藏）
        /// </summary>
        private const int obstructionCheckInterval = 10;
        #endregion

        #region Unity生命周期方法
        /// <summary>
        /// Unity Awake方法：初始化单例
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Unity Start方法：初始化系统并加载配置
        /// </summary>
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
            
            // 初始化静态API委托
            InitializeAPI();
            
            // 预创建指南针，使其在游戏开始时立即出现
            try { EnsureCompass(); } catch { }
            
            // 初始化对话气泡兼容性支持
            try
            {
                // 延迟初始化兼容性组件（等待其他mod加载）
                StartCoroutine(InitializeDialogueBubbleCompatibilityDelayed());
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "初始化对话气泡兼容性支持失败");
            }
            
            // 侧头键位在Camera/Peek.cs中加载，这里不需要调用
            
            // 安全措施：确保在非第一人称模式下光标默认解锁，避免异常关闭/卸载/重装流程后遗留锁定状态
            if (!isFirstPersonMode)
            {
                try { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } catch { }
            }
        }

        /// <summary>
        /// Unity OnDestroy方法：清理资源并恢复状态
        /// </summary>
        private void OnDestroy()
        {
            OptionsManager.OnOptionsChanged -= OnOptionsChanged;
            UnhookHealthBarEvents();
            if (isFirstPersonMode) DisableFirstPerson();
            // 确保恢复跳跃输入绑定
            try { RestoreJumpInput(); } catch { }
            
            // 清理对话气泡兼容性支持
            try
            {
                // 取消订阅事件
                ScavDialogueBubbleCompatibility.OnDialogueBubbleReceived -= OnDialogueBubbleReceived;
                
                // 清理兼容性组件
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

            // 保存所有配置数据（包括动态保存的数据，如武器偏移等）
            try
            {
                FirstPersonCamera.Utilities.ConfigManager.SaveAll();
                FPLogger.Log("控制器销毁前已保存所有配置数据");
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "保存配置数据失败");
            }

            // 安全恢复：如果未恢复则在此恢复
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
        /// <summary>
        /// 重新初始化：获取游戏引用并设置初始状态
        /// </summary>
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

            // 使用命名图层构建保守的射线检测掩码；如果失败则使用默认值
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
        /// <summary>
        /// Unity Update方法：处理输入和状态更新
        /// </summary>
        private void Update()
        {
            // 退出保护：当不在游戏玩法中时（例如返回主菜单）
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
                // 注意：鼠标增量现在在LateUpdate中直接读取，不再在Update中捕获
                // 这样可以避免Unity Input System更新时机与帧率不同步导致的跳帧问题
                // CaptureMouseDelta(uiBlocking); // 已移除，改为在LateUpdate中直接读取
                
                // 检测武器切换并清除检视状态（必须在其他更新之前执行）
                CheckWeaponSwitchAndStopInspect();
                
                // 更新偏头输入
                UpdatePeekInput(uiBlocking);
                
                // 检测统一检视按键（根据武器类型自动选择枪械或近战检视）
                UpdateUnifiedInspectInput(uiBlocking);
                
                // 检查跳跃设置变化并更新输入绑定
                try
                {
                    if (IsJumpEnabled())
                    {
                        // 如果启用了跳跃但还没有设置输入，则设置（仅新输入系统）
                        if (useNewInputSystem && (jumpAction == null || !dashBindingModified))
                        {
                            SetupJumpInput();
                        }
                    }
                    else
                    {
                        // 如果禁用了跳跃但已经设置了输入，则恢复
                        if (jumpAction != null || dashBindingModified)
                        {
                            RestoreJumpInput();
                        }
                    }
                    
                    // 旧输入系统的跳跃检测（作为兜底）
                    // 关键修复：即使新输入系统可用，如果jumpAction未创建成功，也要使用旧输入系统检测
                    // 这样可以确保即使SetupJumpInput()失败（如playerInput为null、Dash动作未找到等），跳跃功能仍然可用
                    if (IsJumpEnabled() && !uiBlocking)
                    {
                        // 如果使用新输入系统但jumpAction未创建，或者使用旧输入系统，都使用旧输入系统检测
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
                
                // 检测鼠标左键状态，判断是否停止射击
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
                
                // 如果松开鼠标，标记停止射击并重置回弹力（调用Combat/Recoil.cs中的方法）
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
                
                // 启用FPS后的轻量窗口扫描；避免繁重的定期全局扫描
                if (Time.unscaledTime < lightSweepEndTime)
                {
                    try { LightSweepTransparentSomeBars(32); } catch { }
                }
                else if (Time.unscaledTime >= nextHealthBarSweepTime)
                {
                    // 低预算定期扫描，以捕获第一人称模式下新生成的血条
                    nextHealthBarSweepTime = Time.unscaledTime + 1f;
                    try { LightSweepTransparentSomeBars(24); } catch { }
                }
            }
            wasUIBlocking = uiBlocking;
        }

        /// <summary>
        /// Unity LateUpdate方法：更新相机位置和旋转
        /// </summary>
        private void LateUpdate()
        {
            // 在非第一人称模式下，确保指南针和体力条被隐藏
            if (!isFirstPersonMode)
            {
                try
                {
                    // 强制隐藏指南针
                    if (compassCanvas != null && compassCanvas.gameObject != null && compassCanvas.gameObject.activeSelf)
                    {
                        compassCanvas.gameObject.SetActive(false);
                    }
                    
                    // 强制隐藏体力条
                    if (staminaCanvas != null && staminaCanvas.gameObject != null && staminaCanvas.gameObject.activeSelf)
                    {
                        staminaCanvas.gameObject.SetActive(false);
                    }
                }
                catch { }
                return;
            }
            
            // 使用Harmony补丁控制准星虚化，不修改任何着色器属性
            try
            {
                int v = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableAimOcclusionFadeKey, 1);

                if (v == 1)
                {
                    DisableAimOcclusionFade();
                }
                else
                {
                    RestoreAimOcclusionFade();
                }
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
            
            // 在FPS下更新瞄准时的武器位置（ADS会将武器拉到相机下方）
            // 注意：即使UI阻挡，也需要更新ADS提示，所以放在uiBlocking检查之外
            if (!uiBlocking)
            {
                UpdateAimAndCrosshair();
                AlignBodyToCamera(); // 保持身体大致对齐（内部通过角度检查降低频率）
            }
            UpdateAdsWeaponPlacement(); // ADS武器位移和提示始终更新（不受UI阻挡影响）
            
            // 定期检查装备变化并重新应用隐藏逻辑（降低频率以提高性能）
            obstructionCheckFrameCounter++;
            if (obstructionCheckFrameCounter >= obstructionCheckInterval)
            {
                obstructionCheckFrameCounter = 0;
                try { HideFirstPersonObstructions(); } catch { }
            }
            
            // 定期更新战争迷雾状态（降低频率以提高性能）
            // if (obstructionCheckFrameCounter == 0) // 复用计数器，每N帧更新一次
            // {
            //     try { UpdateFogOfWarState(); } catch { }
            // }

            // 每帧更新指南针叠加层（安全保护）
            try { LateUpdateCompass(); } catch { }
            try { LateUpdateStaminaBar(); } catch { }
            
            // 更新对话气泡UI（安全保护）
            try { LateUpdateDialogueBubble(); } catch { }
            
            // 强制更新激光红点位置，确保在所有其他更新之后设置
            try { LaserPatch.LateUpdateHitMarkers(); } catch { }
        }
        #endregion

        #region 视图切换方法
        /// <summary>
        /// 切换视图：在第一人称和第三人称之间切换
        /// </summary>
        public void ToggleView()
        {
            if (!isFirstPersonMode) EnableFirstPerson(); else DisableFirstPerson();
            isFirstPersonMode = !isFirstPersonMode; // 在操作后翻转，确保启用路径看到旧状态
        }

        /// <summary>
        /// 设置第一人称模式：启用或禁用第一人称模式
        /// </summary>
        /// <param name="enable">是否启用第一人称模式</param>
        public void SetFirstPerson(bool enable)
        {
            if (enable && !isFirstPersonMode) { EnableFirstPerson(); isFirstPersonMode = true; }
            else if (!enable && isFirstPersonMode) { DisableFirstPerson(); isFirstPersonMode = false; }
        }

        /// <summary>
        /// 设置切换键：设置切换第一人称模式的按键
        /// </summary>
        /// <param name="key">按键码</param>
        public void SetToggleKey(KeyCode key) => toggleKey = key;
        
        /// <summary>
        /// 获取切换键：获取当前切换第一人称模式的按键
        /// 用于与其他mod兼容
        /// </summary>
        /// <returns>切换键的KeyCode</returns>
        public KeyCode GetToggleKey()
        {
            return toggleKey;
        }
        
        /// <summary>
        /// 切换到第三人称俯视角：从第一人称切换到第三人称俯视角
        /// 用于与其他mod兼容
        /// </summary>
        public void SwitchToThirdPersonTopDown()
        {
            if (isFirstPersonMode)
            {
                DisableFirstPerson();
                isFirstPersonMode = false;
                
                // 触发事件，通知其他mod
                try
                {
                    FirstPersonCameraAPI.InvokeSwitchToThirdPersonTopDown();
                }
                catch { }
            }
        }
        
        /// <summary>
        /// 初始化静态API委托
        /// </summary>
        private void InitializeAPI()
        {
            // 注册委托到静态API
            FirstPersonCameraAPI.GetIsFirstPersonMode = () => isFirstPersonMode;
            FirstPersonCameraAPI.GetToggleKey = () => toggleKey;
            FirstPersonCameraAPI.SwitchToThirdPersonTopDown = SwitchToThirdPersonTopDown;
        }
        #endregion

        #region UI状态检查
        /// <summary>
        /// 检查UI是否阻挡：判断当前是否有UI阻挡输入
        /// </summary>
        /// <returns>如果UI阻挡则返回true</returns>
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
        /// <summary>
        /// 启用第一人称模式：设置相机、光标、后处理效果等
        /// </summary>
        private void EnableFirstPerson()
        {
            if (cinemachineVCam != null) cinemachineVCam.enabled = false;
            if (cameraArm != null) cameraArm.enabled = false;
            if (cinemachineBrain != null) cinemachineBrain.enabled = false;

            if (mainCamera != null)
            {
                // 保存基础FOV（用于倍镜缩放）
                // 注意：如果倍镜已经激活，这里不应该覆盖倍镜FOV
                // 倍镜FOV会在UpdateScopeFOV中设置
                baseFov = fov; // 始终更新基础FOV（用于倍镜缩放）
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
            
            // 更新战争迷雾状态（去除战争迷雾）
            try { UpdateFogOfWarState(); } catch { }
            
            // 预先创建ADS提示UI
            try { CreateAdsHintUI(); } catch { }
            
            // 延迟血条处理到轻量、时间限制的扫描
            lightSweepEndTime = Time.unscaledTime + lightSweepDuration;

            if (mainCharacter != null)
            {
                yaw = mainCharacter.transform.eulerAngles.y;
                pitch = 0f;
                
                // 订阅射击事件用于施加后坐力（调用Combat/Recoil.cs中的OnCharacterShoot方法）
                try { mainCharacter.OnShootEvent += OnCharacterShoot; } catch { }
            }

            // 重置防抖内部状态
            antiBobInitialized = false;

            // 创建偏头碰撞代理（调用Camera/Peek.cs中的方法）
            CreatePeekColliderProxy();

            // 在第一人称模式下修补全局鼠标灵敏度
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

            // 启动辅助协程
            if (alignAimRoutine == null) alignAimRoutine = StartCoroutine(AlignAimRoutine());
            if (deferredInitRoutine == null) deferredInitRoutine = StartCoroutine(DeferredFirstPersonInit());

            // 设置跳跃输入绑定
            try { SetupJumpInput(); } catch { }
        }

        /// <summary>
        /// 禁用第一人称模式：恢复相机、光标、后处理效果等
        /// </summary>
        private void DisableFirstPerson()
        {
            // 停止武器检视（如果正在检视）
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
            
            // 清理战争迷雾控制（恢复原始状态）
            try { CleanupFogOfWarControl(); } catch { }

            try { if (mainCamera != null && prevNearClip > 0f) { mainCamera.nearClipPlane = prevNearClip; prevNearClip = -1f; } } catch { }
            RestoreFirstPersonObstructions();
            RestoreEnemyHealthBarTransparency();
            RestoreEnemyHealthBarFlags();
            
            // 取消订阅射击事件（取消订阅Combat/Recoil.cs中的OnCharacterShoot方法）
            try { if (mainCharacter != null) mainCharacter.OnShootEvent -= OnCharacterShoot; } catch { }

            // 销毁偏头碰撞代理（调用Camera/Peek.cs中的方法）
            DestroyPeekColliderProxy();

            // 销毁ADS提示UI
            try { DestroyAdsHintUI(); } catch { }

            // 停止辅助协程
            if (alignAimRoutine != null) { try { StopCoroutine(alignAimRoutine); } catch { } alignAimRoutine = null; }
            if (deferredInitRoutine != null) { try { StopCoroutine(deferredInitRoutine); } catch { } deferredInitRoutine = null; }

            // 恢复全局鼠标灵敏度
            try
            {
                if (systemSensitivityPatched)
                {
                    OptionsManager.MouseSensitivity = (prevSystemMouseSensitivity >= 0f ? prevSystemMouseSensitivity : 10f);
                    systemSensitivityPatched = false;
                }
            }
            catch { }

            // 恢复跳跃输入绑定
            try { RestoreJumpInput(); } catch { }
        }
        #endregion

        #region 对话气泡兼容性支持
        /// <summary>
        /// 延迟初始化对话气泡兼容性支持（等待其他mod加载）
        /// </summary>
        private System.Collections.IEnumerator InitializeDialogueBubbleCompatibilityDelayed()
        {
            // 等待几帧，确保其他mod已经加载
            yield return new WaitForSeconds(1f);
            
            try
            {
                // 初始化兼容性组件
                ScavDialogueBubbleCompatibility.Instance.Initialize();
                
                // 订阅事件
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
        
        /// <summary>
        /// 对话气泡消息接收事件处理
        /// </summary>
        /// <param name="message">消息内容</param>
        private void OnDialogueBubbleReceived(string message)
        {
            try
            {
                // 仅在第一人称模式下显示对话气泡
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
        /// <summary>
        /// 瞄准对齐协程：确保缓存并强制屏幕中心瞄准；UI跟随中心
        /// </summary>
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
                        // 确保缓存并强制屏幕中心瞄准；UI跟随中心
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

        /// <summary>
        /// 延迟第一人称初始化协程：将繁重操作分散到多帧以避免峰值
        /// </summary>
        private System.Collections.IEnumerator DeferredFirstPersonInit()
        {
            // 将繁重操作分散到多帧以避免峰值
            yield return null;
            if (!isFirstPersonMode) yield break;
            try { HideFirstPersonObstructions(); } catch { }

            yield return null;
            if (!isFirstPersonMode) yield break;
            // 从轻量扫描预算开始，而不是完整场景扫描
            lightSweepEndTime = Time.unscaledTime + lightSweepDuration;

            yield return null;
            if (!isFirstPersonMode) yield break;
            try { DisablePostProcessingBlurEffects(); } catch { }
        }
        
        /// <summary>
        /// 检测武器切换并停止检视（如果武器切换了）
        /// 这个方法在Update循环中每帧调用，确保武器切换时立即清除检视状态
        /// 这是主要的武器切换检测点，确保状态及时清除
        /// </summary>
        private void CheckWeaponSwitchAndStopInspect()
        {
            // 如果不在第一人称模式或没有角色，不需要检测
            if (!isFirstPersonMode || mainCharacter == null) return;
            
            // 获取当前武器状态
            var currentGun = mainCharacter.GetGun();
            var currentMelee = mainCharacter.GetMeleeWeapon();
            
            // 如果正在检视枪械武器，检查武器是否切换
            if (isInspectingWeapon)
            {
                // 如果检视开始时的武器引用不为空
                if (inspectingWeapon != null)
                {
                    // 检查当前武器与检视开始时的武器是否不同
                    // 包括：当前武器为null（切换到非枪械）、或当前武器对象不同（切换到其他枪械）
                    if (currentGun != inspectingWeapon)
                    {
                        // 立即停止检视
                        StopWeaponInspect();
                    }
                }
                else
                {
                    // 如果inspectingWeapon为null但isInspectingWeapon为true，说明状态不一致
                    // 这可能是由于某些异常情况导致的，需要清除状态
                    isInspectingWeapon = false;
                    if (weaponInspectCoroutine != null)
                    {
                        StopCoroutine(weaponInspectCoroutine);
                        weaponInspectCoroutine = null;
                    }
                }
            }
            
            // 如果正在检视近战武器，检查是否切换到枪械
            if (isInspectingMelee)
            {
                // 如果当前有枪械（切换到枪械），停止近战检视
                if (currentGun != null)
                {
                    StopMeleeInspect();
                }
                // 如果检视开始时的近战武器引用不为空
                else if (inspectingMelee != null)
                {
                    // 检查当前近战武器与检视开始时的近战武器是否不同
                    if (currentMelee != inspectingMelee)
                    {
                        // 立即停止检视
                        StopMeleeInspect();
                    }
                }
                else
                {
                    // 如果inspectingMelee为null但isInspectingMelee为true，说明状态不一致
                    // 这可能是由于某些异常情况导致的，需要清除状态
                    isInspectingMelee = false;
                    if (meleeInspectCoroutine != null)
                    {
                        StopCoroutine(meleeInspectCoroutine);
                        meleeInspectCoroutine = null;
                    }
                }
            }
        }
        
        /// <summary>
        /// 统一的检视输入检测（根据武器类型自动选择枪械或近战检视）
        /// </summary>
        private void UpdateUnifiedInspectInput(bool uiBlocking)
        {
            if (uiBlocking) return;
            
            // 加载统一的检视按键
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
            
            // 避免同一帧重复检测
            if (lastInspectKeyFrame == Time.frameCount) return;
            
            // 检测按键按下
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
                
                // 根据当前武器类型决定执行哪个检视
                if (mainCharacter != null)
                {
                    // 优先检查近战武器
                    var melee = mainCharacter.GetMeleeWeapon();
                    if (melee != null)
                    {
                        StartMeleeInspect();
                        return;
                    }
                    
                    // 如果没有近战武器，检查枪械
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
