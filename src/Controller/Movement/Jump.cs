using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.OptionsUI;
using ECM2;
using UnityEngine.InputSystem;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 跳跃相关字段（原有）
        private InputAction jumpAction;
        private string dashOriginalBinding;
        private int dashBindingIndex = 0;
        private bool dashBindingModified = false;
        #endregion

        #region ECM2 跳跃字段
        private Coroutine jumpECM2Coroutine;
        private bool jumpRequested;
        private float lastJumpTime;
        private const float JumpCooldown = 0.1f;

        private CharacterMovement characterMovement;
        private global::Movement movementCtrl; // 游戏原生的 Movement 组件
        private bool wasGrounded;

        private float jumpHeight = 0.75f; // 固定的跳跃高度
        private float gravityMultiplier = 1.6f;  // 固定的重力加速度
        private float jumpStaminaCost = 5f; // 固定的跳跃体力消耗

        private bool dashActionDisabledForJump = false;
        private Component dashActionComponent;
        private bool allowDashDuringJumpCache;
        private float lastDashLogTime;

        private static PropertyInfo runningPropCache;
        private static MethodInfo stopMethodCache;
        private static PropertyInfo enabledPropCache;
        private static bool reflectionInitialized = false;

        private int jumpDisableCount = 0;

        private InputAction dashInputAction;
        private bool dashInputDisabledForJump = false;

        private List<PetAIEventInfo> petAIEventInfos = new List<PetAIEventInfo>();

        // 空中速度减半相关字段（使用 SetForceMoveVelocity）
        private bool speedReducedForAir = false;
        private Vector3 originalHorizontalVelocity; // 用于恢复时参考（实际不需要存储）

        private class PetAIEventInfo
        {
            public object petAIInstance;
            public object masterCharacter;
            public MethodInfo onSetPositionMethod;
            public FieldInfo eventField;
            public EventInfo eventInfo;
            public object eventHandler;
        }
        #endregion

        #region 原有输入绑定方法
        private bool IsJumpEnabled()
        {
            try
            {
                return OptionsHelper.LoadInt(OptionsUIConstants.EnableJumpKey, 0) == 1;
            }
            catch
            {
                return false;
            }
        }

        private void SetupJumpInput()
        {
            if (!IsJumpEnabled() || !isFirstPersonMode)
                return;

            if (!useNewInputSystem)
            {
                // FPLogger.Log("使用旧输入系统，跳跃将通过Update()中的Input.GetKeyDown检测");
                StartJumpECM2();
                return;
            }

            try
            {
                var playerInput = GameManager.MainPlayerInput;
                if (playerInput == null || playerInput.actions == null)
                {
                    // FPLogger.LogWarning("无法设置跳跃输入: playerInput或actions为null，将使用旧输入系统兜底");
                }
                else
                {
                    var dashAction = playerInput.actions.FindAction("Dash", false);
                    if (dashAction == null)
                    {
                        // FPLogger.LogWarning("无法设置跳跃输入: Dash动作未找到，将使用旧输入系统兜底");
                    }
                    else
                    {
                        if (!dashBindingModified && dashAction.bindings.Count > dashBindingIndex)
                        {
                            var binding = dashAction.bindings[dashBindingIndex];
                            dashOriginalBinding = binding.overridePath;
                            if (string.IsNullOrEmpty(dashOriginalBinding))
                                dashOriginalBinding = binding.path;

                            for (int i = 0; i < dashAction.bindings.Count; i++)
                            {
                                var b = dashAction.bindings[i];
                                string bindingPath = !string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path;
                                if (bindingPath.Contains("space") || bindingPath.Contains("Space"))
                                {
                                    dashBindingIndex = i;
                                    dashAction.ApplyBindingOverride(i, "<Keyboard>/leftCtrl");
                                    dashBindingModified = true;
                                    // FPLogger.Log("Dash绑定已修改为Ctrl键");
                                    break;
                                }
                            }
                            
                            if (!dashBindingModified && dashAction.bindings.Count > 0)
                            {
                                dashBindingIndex = 0;
                                dashAction.ApplyBindingOverride(0, "<Keyboard>/leftCtrl");
                                dashBindingModified = true;
                                // FPLogger.Log("Dash绑定已修改为Ctrl键（使用第一个绑定）");
                            }
                        }
                    }
                }

                if (jumpAction == null)
                {
                    try
                    {
                        jumpAction = new InputAction("FirstPersonJump", InputActionType.Button);
                        jumpAction.AddBinding("<Keyboard>/space");
                        jumpAction.started += OnJumpInput;
                        jumpAction.Enable();
                        // FPLogger.Log("跳跃InputAction已创建并启用（新输入系统）");
                    }
                    catch (System.Exception ex2)
                    {
                        // FPLogger.LogWarning("创建jumpAction失败: {0}，将使用旧输入系统兜底", ex2.Message);
                        jumpAction = null;
                    }
                }

                StartJumpECM2();
            }
            catch (System.Exception ex)
            {
                // FPLogger.LogException(ex, "设置跳跃输入失败，将使用旧输入系统兜底");
                if (jumpAction != null)
                {
                    try
                    {
                        jumpAction.started -= OnJumpInput;
                        jumpAction.Disable();
                        jumpAction.Dispose();
                    }
                    catch { }
                    jumpAction = null;
                }
            }
        }

        private void RestoreJumpInput()
        {
            try
            {
                var playerInput = GameManager.MainPlayerInput;
                if (playerInput == null || playerInput.actions == null)
                    return;

                if (dashBindingModified)
                {
                    var dashAction = playerInput.actions.FindAction("Dash", false);
                    if (dashAction != null)
                    {
                        if (!string.IsNullOrEmpty(dashOriginalBinding))
                            dashAction.ApplyBindingOverride(dashBindingIndex, dashOriginalBinding);
                        else
                            dashAction.RemoveBindingOverride(dashBindingIndex);
                    }
                    dashBindingModified = false;
                    dashOriginalBinding = null;
                }

                if (jumpAction != null)
                {
                    jumpAction.started -= OnJumpInput;
                    jumpAction.Disable();
                    jumpAction.Dispose();
                    jumpAction = null;
                }
            }
            catch (System.Exception ex)
            {
                // FPLogger.LogException(ex, "恢复跳跃输入失败");
            }

            StopJumpECM2();
        }
        #endregion

        #region ECM2 跳跃核心方法
        private void StartJumpECM2()
        {
            if (jumpECM2Coroutine != null) return;
            jumpECM2Coroutine = StartCoroutine(JumpECM2Coroutine());
            // FPLogger.Log("ECM2 跳跃协程已启动");
        }

        private void StopJumpECM2()
        {
            if (jumpECM2Coroutine != null)
            {
                StopCoroutine(jumpECM2Coroutine);
                jumpECM2Coroutine = null;
                EnableDashAfterJump();
                if (jumpDisableCount > 0)
                {
                    jumpDisableCount = 1;
                    EnablePetTeleportAfterJump();
                }
                // 确保清除强制速度
                if (movementCtrl != null && mainCharacter != null)
                {
                    mainCharacter.SetForceMoveVelocity(Vector3.zero);
                }
                speedReducedForAir = false;
                // FPLogger.Log("ECM2 跳跃协程已停止");
            }
        }

        private void UpdateAllowDashCache()
        {
            allowDashDuringJumpCache = OptionsHelper.LoadInt(OptionsUIConstants.AllowDashDuringJumpKey, 1) == 1;
        }

        private void InitializeReflection(object dashAction)
        {
            if (reflectionInitialized || dashAction == null) return;

            var type = dashAction.GetType();

            string[] runningNames = { "Running", "IsRunning", "running", "isRunning", "m_IsRunning" };
            foreach (var name in runningNames)
            {
                runningPropCache = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (runningPropCache != null)
                {
                    // FPLogger.Log($"[翻滚] 缓存Running属性: {name}");
                    break;
                }
            }

            string[] stopNames = { "StopAction", "Stop", "EndAction", "Cancel", "Interrupt" };
            foreach (var name in stopNames)
            {
                stopMethodCache = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stopMethodCache != null)
                {
                    // FPLogger.Log($"[翻滚] 缓存停止方法: {name}");
                    break;
                }
            }

            enabledPropCache = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            reflectionInitialized = true;
        }

        private void DisableDashDuringJump()
        {
            if (mainCharacter?.dashAction == null) return;

            var dashAction = mainCharacter.dashAction;
            InitializeReflection(dashAction);
            try
            {
                stopMethodCache?.Invoke(dashAction, null);
                if (enabledPropCache != null && enabledPropCache.CanWrite)
                {
                    enabledPropCache.SetValue(dashAction, false);
                    dashActionDisabledForJump = true;
                    dashActionComponent = dashAction as Component;
                }
            }
            catch (System.Exception ex)
            {
                // FPLogger.LogWarning($"禁用翻滚组件失败: {ex.Message}");
            }

            if (!dashInputDisabledForJump)
            {
                if (dashInputAction == null)
                {
                    var playerInput = GameManager.MainPlayerInput;
                    if (playerInput?.actions != null)
                        dashInputAction = playerInput.actions.FindAction("Dash", false);
                }

                if (dashInputAction != null)
                {
                    dashInputAction.Disable();
                    dashInputDisabledForJump = true;
                    // FPLogger.Log("[翻滚] 已禁用翻滚输入");
                }
            }
        }

        private void EnableDashAfterJump()
        {
            if (dashActionDisabledForJump)
            {
                try
                {
                    if (enabledPropCache != null && enabledPropCache.CanWrite && dashActionComponent != null)
                    {
                        enabledPropCache.SetValue(dashActionComponent, true);
                        dashActionDisabledForJump = false;
                    }
                }
                catch (System.Exception ex)
                {
                    // FPLogger.LogWarning($"恢复翻滚组件失败: {ex.Message}");
                }
            }

            if (dashInputDisabledForJump)
            {
                if (dashInputAction != null)
                {
                    dashInputAction.Enable();
                    dashInputDisabledForJump = false;
                    // FPLogger.Log("[翻滚] 已恢复翻滚输入");
                }
            }
        }

        private void DisablePetTeleportDuringJump()
        {
            if (jumpDisableCount == 0)
                DisablePetTeleportCore();
            jumpDisableCount++;
        }

        private void EnablePetTeleportAfterJump()
        {
            jumpDisableCount--;
            if (jumpDisableCount == 0)
                EnablePetTeleportCore();
        }

        private void DisablePetTeleportCore()
        {
            try
            {
                petAIEventInfos.Clear();

                System.Type petAIType = null;
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (System.Reflection.Assembly assembly in assemblies)
                {
                    try
                    {
                        // petAIType = assembly.GetType("PetAI");
                        if (petAIType != null) break;
                    }
                    catch { }
                }

                if (petAIType == null)
                {
                    // FPLogger.LogWarning("无法找到PetAI类型，跳过禁用狗传送");
                    return;
                }

                MonoBehaviour[] allMonoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();
                int foundCount = 0;

                foreach (MonoBehaviour mb in allMonoBehaviours)
                {
                    if (mb == null || mb.GetType() != petAIType) continue;

                    try
                    {
                        object petAI = mb;
                        FieldInfo masterField = petAIType.GetField("master", BindingFlags.Public | BindingFlags.Instance);
                        if (masterField == null) continue;

                        object masterCharacter = masterField.GetValue(petAI);
                        if (masterCharacter == null) continue;

                        System.Type characterType = masterCharacter.GetType();
                        PropertyInfo isMainCharacterProp = characterType.GetProperty("IsMainCharacter");
                        if (isMainCharacterProp != null)
                        {
                            bool isMain = (bool)isMainCharacterProp.GetValue(masterCharacter);
                            if (!isMain) continue;
                        }

                        MethodInfo onSetPositionMethod = petAIType.GetMethod("OnMainCharacterSetPosition",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (onSetPositionMethod == null) continue;

                        System.Type characterMainControlType = masterCharacter.GetType();
                        EventInfo eventInfo = characterMainControlType.GetEvent("OnSetPositionEvent");
                        if (eventInfo == null) continue;

                        FieldInfo eventField = characterMainControlType.GetField("OnSetPositionEvent",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                        if (eventField == null)
                        {
                            eventField = characterMainControlType.GetField("OnSetPositionEvent",
                                BindingFlags.NonPublic | BindingFlags.Instance);
                        }

                        if (eventField == null)
                        {
                            try
                            {
                                System.Type actionType = typeof(System.Action<,>).MakeGenericType(
                                    characterMainControlType, typeof(Vector3));
                                System.Delegate handler = System.Delegate.CreateDelegate(actionType, petAI, onSetPositionMethod);

                                eventInfo.RemoveEventHandler(masterCharacter, handler);

                                PetAIEventInfo info = new PetAIEventInfo
                                {
                                    petAIInstance = petAI,
                                    masterCharacter = masterCharacter,
                                    onSetPositionMethod = onSetPositionMethod,
                                    eventInfo = eventInfo,
                                    eventHandler = handler
                                };
                                petAIEventInfos.Add(info);

                                foundCount++;
                                // FPLogger.Log("已禁用PetAI传送: PetAI={0}, Master={1}", petAI, masterCharacter);
                            }
                            catch (System.Exception ex)
                            {
                                // FPLogger.LogWarning("移除PetAI事件失败: {0}", ex.Message);
                            }
                        }
                        else
                        {
                            try
                            {
                                object eventHandler = eventField.GetValue(masterCharacter);

                                System.Type actionType = typeof(System.Action<,>).MakeGenericType(
                                    characterMainControlType, typeof(Vector3));
                                System.Delegate handler = System.Delegate.CreateDelegate(actionType, petAI, onSetPositionMethod);

                                if (eventHandler != null)
                                {
                                    System.Delegate combinedDelegate = System.Delegate.Remove((System.Delegate)eventHandler, handler);
                                    eventField.SetValue(masterCharacter, combinedDelegate);

                                    PetAIEventInfo info = new PetAIEventInfo
                                    {
                                        petAIInstance = petAI,
                                        masterCharacter = masterCharacter,
                                        onSetPositionMethod = onSetPositionMethod,
                                        eventField = eventField,
                                        eventHandler = eventHandler
                                    };
                                    petAIEventInfos.Add(info);

                                    foundCount++;
                                    // FPLogger.Log("已禁用PetAI传送（通过字段）: PetAI={0}, Master={1}", petAI, masterCharacter);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                // FPLogger.LogWarning("操作PetAI事件字段失败: {0}", ex.Message);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // FPLogger.LogWarning("处理PetAI时出错: {0}", ex.Message);
                    }
                }

                // if (foundCount > 0)
                //     FPLogger.Log("已禁用 {0} 个PetAI的传送功能", foundCount);
                // else
                //     FPLogger.Log("未找到需要禁用的PetAI组件");
            }
            catch (System.Exception ex)
            {
                // FPLogger.LogException(ex, "禁用PetAI传送时发生异常");
            }
        }

        private void EnablePetTeleportCore()
        {
            try
            {
                if (petAIEventInfos == null || petAIEventInfos.Count == 0) return;

                int restoredCount = 0;

                foreach (PetAIEventInfo info in petAIEventInfos)
                {
                    try
                    {
                        if (info.petAIInstance == null || info.masterCharacter == null || info.onSetPositionMethod == null) continue;

                        if (info.eventInfo != null && info.eventHandler != null)
                        {
                            try
                            {
                                info.eventInfo.AddEventHandler(info.masterCharacter, (System.Delegate)info.eventHandler);
                                restoredCount++;
                                // FPLogger.Log("已恢复PetAI传送（通过EventInfo）: PetAI={0}", info.petAIInstance);
                                continue;
                            }
                            catch (System.Exception ex)
                            {
                                // FPLogger.LogWarning("通过EventInfo恢复失败: {0}", ex.Message);
                            }
                        }

                        if (info.eventField != null)
                        {
                            try
                            {
                                object currentHandler = info.eventField.GetValue(info.masterCharacter);

                                System.Type characterMainControlType = info.masterCharacter.GetType();
                                System.Type actionType = typeof(System.Action<,>).MakeGenericType(
                                    characterMainControlType, typeof(Vector3));
                                System.Delegate handler = System.Delegate.CreateDelegate(actionType, info.petAIInstance, info.onSetPositionMethod);

                                if (currentHandler == null)
                                {
                                    info.eventField.SetValue(info.masterCharacter, handler);
                                }
                                else
                                {
                                    System.Delegate combinedDelegate = System.Delegate.Combine((System.Delegate)currentHandler, handler);
                                    info.eventField.SetValue(info.masterCharacter, combinedDelegate);
                                }

                                restoredCount++;
                                // FPLogger.Log("已恢复PetAI传送（通过字段）: PetAI={0}", info.petAIInstance);
                            }
                            catch (System.Exception ex)
                            {
                                // FPLogger.LogWarning("通过字段恢复失败: {0}", ex.Message);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // FPLogger.LogWarning("恢复PetAI传送时出错: {0}", ex.Message);
                    }
                }

                if (restoredCount > 0)
                    // FPLogger.Log("已恢复 {0} 个PetAI的传送功能", restoredCount);

                petAIEventInfos.Clear();
            }
            catch (System.Exception ex)
            {
                // FPLogger.LogException(ex, "恢复PetAI传送时发生异常");
            }
        }

        private IEnumerator JumpECM2Coroutine()
        {
            while (mainCharacter == null)
                yield return new WaitForSeconds(0.1f);

            movementCtrl = null;
            while (movementCtrl == null)
            {
                movementCtrl = mainCharacter.movementControl;
                yield return new WaitForSeconds(0.1f);
            }

            while (characterMovement == null)
            {
                characterMovement = movementCtrl.GetComponent<CharacterMovement>();
                yield return new WaitForSeconds(0.1f);
            }

            // FPLogger.Log("ECM2 跳跃协程初始化完成");
            UpdateAllowDashCache();

            while (true)
            {
                yield return null;

                if (!isFirstPersonMode || !IsJumpEnabled() || mainCharacter == null)
                    continue;

                movementCtrl = mainCharacter.movementControl;
                if (movementCtrl == null) continue;

                bool isGrounded = movementCtrl.IsOnGround;
                float currentVelY = characterMovement.velocity.y;

                if (!wasGrounded && isGrounded && currentVelY <= 0)
                {
                    NotifyLandedECM2();
                    wasGrounded = true;
                    EnablePetTeleportAfterJump();
                    EnableDashAfterJump();

                    // 落地后清除强制速度
                    if (speedReducedForAir)
                    {
                        mainCharacter.SetForceMoveVelocity(Vector3.zero);
                        speedReducedForAir = false;
                    }
                }
                else if (wasGrounded && !isGrounded)
                {
                    wasGrounded = false;
                }

                bool isInAir = !wasGrounded;

                // 空中速度减半逻辑：通过 SetForceMoveVelocity 实现
                if (isInAir)
                {
                    if (!speedReducedForAir)
                    {
                        // 进入空中时，设置强制速度减半
                        Vector3 currentVel = mainCharacter.Velocity; // 当前速度
                        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
                        // 保持当前水平方向，速度减半
                        if (horizontalVel.magnitude > 0.1f)
                        {
                            Vector3 reducedVel = horizontalVel.normalized * (horizontalVel.magnitude * 0.8f);
                            mainCharacter.SetForceMoveVelocity(reducedVel);
                        }
                        else
                        {
                            mainCharacter.SetForceMoveVelocity(Vector3.zero);
                        }
                        speedReducedForAir = true;
                    }
                    else
                    {
                        // 持续更新强制速度，确保输入变化时也保持减半
                        // 获取输入方向
                        Vector3 moveInput = movementCtrl.MoveInput;
                        if (moveInput.magnitude > 0.1f)
                        {
                            // 根据当前最大速度计算减半后的目标速度
                            float currentMaxSpeed = movementCtrl.Running ? movementCtrl.runSpeed : movementCtrl.walkSpeed;
                            float targetSpeed = currentMaxSpeed * 0.8f;
                            Vector3 desiredVelocity = moveInput.normalized * targetSpeed;
                            // 保持垂直分量为0
                            desiredVelocity.y = 0f;
                            mainCharacter.SetForceMoveVelocity(desiredVelocity);
                        }
                        else
                        {
                            // 无输入时速度逐渐衰减（由游戏物理处理），但我们不清除强制速度，让 SetForceMoveVelocity 保持上一次的值？
                            // 更好的做法是持续应用减半速度，但无输入时设为0，让角色自然停下
                            // 但 SetForceMoveVelocity 会覆盖当前速度，所以我们最好每帧根据输入重新设置
                            // 如果没有输入，我们可以设置一个很小的速度或者直接设0
                            // 为了平滑，可以根据当前速度逐渐减速，但简单起见设0
                            mainCharacter.SetForceMoveVelocity(Vector3.zero);
                        }
                    }
                }
                else
                {
                    if (speedReducedForAir)
                    {
                        // 落地后清除强制速度
                        mainCharacter.SetForceMoveVelocity(Vector3.zero);
                        speedReducedForAir = false;
                    }
                }

                if (!allowDashDuringJumpCache && isInAir)
                {
                    var dashAction = mainCharacter.dashAction;
                    if (dashAction != null)
                    {
                        InitializeReflection(dashAction);

                        if (!dashActionDisabledForJump)
                        {
                            DisableDashDuringJump();
                        }
                        else
                        {
                            if (enabledPropCache != null && enabledPropCache.CanWrite)
                            {
                                bool current = (bool)enabledPropCache.GetValue(dashAction);
                                if (current)
                                {
                                    enabledPropCache.SetValue(dashAction, false);
                                }
                            }
                        }

                        if (runningPropCache != null && stopMethodCache != null)
                        {
                            bool isRunning = runningPropCache.GetValue(dashAction) is bool b && b;
                            if (isRunning)
                            {
                                stopMethodCache.Invoke(dashAction, null);
                            }
                        }
                    }
                }
                else if (allowDashDuringJumpCache && dashActionDisabledForJump)
                {
                    EnableDashAfterJump();
                }

                if (jumpRequested)
                {
                    PerformJumpECM2(movementCtrl);
                    jumpRequested = false;
                }

                if (!useNewInputSystem && Input.GetKeyDown(KeyCode.Space) && Time.time - lastJumpTime > JumpCooldown)
                {
                    jumpRequested = true;
                }
            }
        }

        private void PerformJumpECM2(global::Movement movementCtrl)
        {
            if (!isFirstPersonMode || mainCharacter == null) return;
            if (movementCtrl == null || characterMovement == null) return;
            if (!movementCtrl.IsOnGround) return;
            if (Time.time - lastJumpTime < JumpCooldown) return;

            // 直接使用固定的体力消耗值（不再通过反射获取）
            float staminaCost = jumpStaminaCost;

            if (mainCharacter.CurrentStamina < staminaCost)
            {
                // FPLogger.Log("跳跃失败: 体力不足 (当前: {0}, 需要: {1})", mainCharacter.CurrentStamina, staminaCost);
                return;
            }
            mainCharacter.UseStamina(staminaCost);
            // FPLogger.Log("ECM2跳跃，消耗体力: {0}", staminaCost);

            if (Instance != null) Instance.NotifyJumping();

            DisablePetTeleportDuringJump();

            UpdateAllowDashCache();
            if (!allowDashDuringJumpCache)
            {
                DisableDashDuringJump();
            }

            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            float initialVerticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            try
            {
                characterMovement.PauseGroundConstraint(0.05f);
                Vector3 vel = characterMovement.velocity;
                vel.y = initialVerticalVelocity;
                characterMovement.velocity = vel;
            }
            catch (System.Exception ex)
            {
                // FPLogger.LogException(ex, "ECM2跳跃执行失败");
                return;
            }

            lastJumpTime = Time.time;
            wasGrounded = false;
        }

        
        private void NotifyLandedECM2()
        {
            if (Instance != null)
            {
                var method = Instance.GetType().GetMethod("NotifyLanded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(Instance, null);
            }
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            // FPLogger.Log("新输入系统检测到跳跃输入");
            jumpRequested = true;
        }

        // 兼容旧版代码中的 DoJump 调用
        private void DoJump()
        {
            if (!isFirstPersonMode || !IsJumpEnabled()) return;
            if (jumpECM2Coroutine == null)
                StartJumpECM2();
            jumpRequested = true;
        }
        #endregion
    }
}