using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 跳跃功能模块
    /// 负责管理跳跃输入绑定和跳跃动作执行
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 跳跃相关字段
        /// <summary>
        /// 跳跃输入动作
        /// </summary>
        private InputAction jumpAction;

        /// <summary>
        /// Dash动作的原始绑定（用于恢复）
        /// </summary>
        private string dashOriginalBinding;

        /// <summary>
        /// Dash动作的原始绑定索引
        /// </summary>
        private int dashBindingIndex = 0;

        /// <summary>
        /// 是否已经修改了Dash绑定
        /// </summary>
        private bool dashBindingModified = false;

        /// <summary>
        /// 当前跳跃协程（用于防止重复跳跃）
        /// </summary>
        private Coroutine jumpCoroutine = null;

        /// <summary>
        /// 存储宠物AI的事件处理信息，用于在跳跃期间禁用传送
        /// </summary>
        private List<PetAIEventInfo> petAIEventInfos = new List<PetAIEventInfo>();

        /// <summary>
        /// 宠物AI事件信息（用于临时禁用传送）
        /// </summary>
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

        #region 跳跃功能方法
        /// <summary>
        /// 检查是否启用了跳跃功能
        /// </summary>
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

        /// <summary>
        /// 设置跳跃输入绑定
        /// 当启用跳跃时，将Dash绑定改为Ctrl键，并创建空格键的跳跃输入（仅新输入系统）
        /// </summary>
        private void SetupJumpInput()
        {
            if (!IsJumpEnabled() || !isFirstPersonMode)
            {
                return;
            }

            // 只有在新输入系统下才设置InputAction
            if (!useNewInputSystem)
            {
                FPLogger.Log("使用旧输入系统，跳跃将通过Update()中的Input.GetKeyDown检测");
                return;
            }

            try
            {
                var playerInput = GameManager.MainPlayerInput;
                if (playerInput == null || playerInput.actions == null)
                {
                    FPLogger.LogWarning("无法设置跳跃输入: playerInput或actions为null，将使用旧输入系统兜底");
                    // 注意：这里不return，允许继续尝试创建jumpAction，即使可能失败
                    // 如果失败，Update()中的旧输入系统兜底会处理
                }
                else
                {
                    // 获取Dash动作
                    var dashAction = playerInput.actions.FindAction("Dash", false);
                    if (dashAction == null)
                    {
                        FPLogger.LogWarning("无法设置跳跃输入: Dash动作未找到，将使用旧输入系统兜底");
                        // 注意：即使Dash动作未找到，仍然尝试创建jumpAction（不依赖Dash动作）
                    }
                    else
                    {
                        // 只有在找到Dash动作时才修改绑定
                        // 如果还没有修改过绑定，保存原始绑定并修改
                        if (!dashBindingModified && dashAction.bindings.Count > dashBindingIndex)
                        {
                            // 保存原始绑定（优先使用overridePath，如果没有则使用path）
                            var binding = dashAction.bindings[dashBindingIndex];
                            dashOriginalBinding = binding.overridePath;
                            if (string.IsNullOrEmpty(dashOriginalBinding))
                            {
                                dashOriginalBinding = binding.path;
                            }

                            // 修改Dash绑定为Ctrl键（同时支持左右Ctrl）
                            // 注意：ApplyBindingOverride只能覆盖一个绑定，我们需要找到空格键的绑定索引
                            for (int i = 0; i < dashAction.bindings.Count; i++)
                            {
                                var b = dashAction.bindings[i];
                                string bindingPath = !string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path;
                                if (bindingPath.Contains("space") || bindingPath.Contains("Space"))
                                {
                                    // 找到空格键绑定，修改为Ctrl键
                                    dashBindingIndex = i;
                                    dashAction.ApplyBindingOverride(i, "<Keyboard>/leftCtrl");
                                    dashBindingModified = true;
                                    FPLogger.Log("Dash绑定已修改为Ctrl键");
                                    break;
                                }
                            }
                            
                            // 如果没找到空格键绑定，尝试修改第一个绑定
                            if (!dashBindingModified && dashAction.bindings.Count > 0)
                            {
                                dashBindingIndex = 0;
                                dashAction.ApplyBindingOverride(0, "<Keyboard>/leftCtrl");
                                dashBindingModified = true;
                                FPLogger.Log("Dash绑定已修改为Ctrl键（使用第一个绑定）");
                            }
                        }
                    }
                }

                // 创建跳跃输入动作（绑定到空格键）
                // 注意：jumpAction的创建不依赖Dash动作，即使Dash动作未找到也可以创建
                if (jumpAction == null)
                {
                    try
                    {
                        jumpAction = new InputAction("FirstPersonJump", InputActionType.Button);
                        jumpAction.AddBinding("<Keyboard>/space");
                        jumpAction.started += OnJumpInput;
                        jumpAction.Enable();
                        FPLogger.Log("跳跃InputAction已创建并启用（新输入系统）");
                    }
                    catch (System.Exception ex2)
                    {
                        FPLogger.LogWarning("创建jumpAction失败: {0}，将使用旧输入系统兜底", ex2.Message);
                        jumpAction = null; // 确保为null，以便使用旧输入系统兜底
                    }
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "设置跳跃输入失败，将使用旧输入系统兜底");
                // 确保jumpAction为null，以便使用旧输入系统兜底
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

        /// <summary>
        /// 恢复跳跃输入绑定
        /// 恢复Dash的原始绑定，并移除跳跃输入
        /// </summary>
        private void RestoreJumpInput()
        {
            try
            {
                var playerInput = GameManager.MainPlayerInput;
                if (playerInput == null || playerInput.actions == null)
                {
                    return;
                }

                // 恢复Dash绑定
                if (dashBindingModified)
                {
                    var dashAction = playerInput.actions.FindAction("Dash", false);
                    if (dashAction != null)
                    {
                        if (!string.IsNullOrEmpty(dashOriginalBinding))
                        {
                            dashAction.ApplyBindingOverride(dashBindingIndex, dashOriginalBinding);
                        }
                        else
                        {
                            // 如果原始绑定为空，移除override以恢复默认绑定
                            dashAction.RemoveBindingOverride(dashBindingIndex);
                        }
                    }
                    dashBindingModified = false;
                    dashOriginalBinding = null;
                }

                // 移除跳跃输入动作
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
                FPLogger.LogException(ex, "恢复跳跃输入失败");
            }
        }

        /// <summary>
        /// 执行跳跃的核心逻辑
        /// 从OnJumpInput和Update中的旧输入系统检测中调用
        /// 使用协程和SetPosition方式实现跳跃（参考BetterJump的实现）
        /// </summary>
        private void DoJump()
        {
            if (!isFirstPersonMode || !IsJumpEnabled())
            {
                return;
            }

            if (mainCharacter == null)
            {
                FPLogger.LogWarning("跳跃失败: mainCharacter为null");
                return;
            }

            // 如果正在跳跃，不允许重复跳跃（通过检查协程是否存在）
            if (jumpCoroutine != null)
            {
                FPLogger.Log("跳跃失败: 正在跳跃中");
                return;
            }

            // 获取翻滚动作以获取体力消耗
            var dashAction = mainCharacter.dashAction;
            if (dashAction == null)
            {
                FPLogger.LogWarning("跳跃失败: dashAction为null");
                return;
            }

            // 获取翻滚的体力消耗
            float staminaCost = 10f; // 默认值
            try
            {
                var staminaCostField = dashAction.GetType().GetField("staminaCost",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (staminaCostField != null)
                {
                    staminaCost = (float)staminaCostField.GetValue(dashAction);
                }
            }
            catch { }

            // 检查体力是否足够
            if (mainCharacter.CurrentStamina < staminaCost)
            {
                FPLogger.Log("跳跃失败: 体力不足 (当前: {0}, 需要: {1})", mainCharacter.CurrentStamina, staminaCost);
                return; // 体力不足，不执行跳跃
            }

            // 获取移动控制器
            var movementControl = mainCharacter.movementControl;
            if (movementControl == null)
            {
                FPLogger.LogWarning("跳跃失败: movementControl为null");
                return;
            }

            // 检查是否在地面上（只有在地面上才能跳跃）
            // 现代FPS允许在刚落地时立即跳跃，所以需要更宽松的地面检测
            bool canJump = false;
            
            // 方法1：检查IsOnGround（标准地面检测）
            if (movementControl.IsOnGround)
            {
                canJump = true;
            }
            else
            {
                // 方法2：检查垂直速度，如果垂直速度接近0，说明刚落地或在地面上
                Vector3 velocity = movementControl.Velocity;
                float verticalVelocity = velocity.y;
                
                // 如果垂直速度接近0（在-0.5到0.5之间），认为可以跳跃（刚落地状态）
                if (Mathf.Abs(verticalVelocity) < 0.5f)
                {
                    canJump = true;
                    FPLogger.Log("允许跳跃: 垂直速度接近0 ({0:F2}m/s)，认为刚落地", verticalVelocity);
                }
            }
            
            if (!canJump)
            {
                FPLogger.Log("跳跃失败: 不在地面上且垂直速度不为0 (垂直速度: {0:F2}m/s)", movementControl.Velocity.y);
                return; // 不在地面上时不能跳跃
            }

            // 消耗体力
            mainCharacter.UseStamina(staminaCost);
            FPLogger.Log("开始跳跃，消耗体力: {0}", staminaCost);

            // 通知防抖动系统正在跳跃（禁用Y轴平滑）
            if (Instance != null)
            {
                Instance.NotifyJumping();
                FPLogger.Log("已通知防抖动系统：正在跳跃");
            }
            else
            {
                FPLogger.LogWarning("无法通知防抖动系统：Instance 为 null");
            }

            // 禁用狗的传送（在跳跃期间）
            DisablePetTeleportDuringJump();

            // 启动跳跃协程
            if (jumpCoroutine != null)
            {
                StopCoroutine(jumpCoroutine);
            }
            jumpCoroutine = StartCoroutine(DoJumpCoroutine(movementControl));
        }

        /// <summary>
        /// 跳跃协程 - 使用速度系统实现真实的物理跳跃（防止穿墙）
        /// 使用速度系统让游戏的运动系统处理碰撞检测
        /// </summary>
        private IEnumerator DoJumpCoroutine(global::Movement movementControl)
        {
            // 使用 try-finally 确保即使跳跃被中断，也能恢复狗的传送
            try
            {
                // 跳跃参数 - 现代FPS风格设计（已优化：减慢速度，防止穿墙）
                // 参考：CS:GO, Valorant, Apex Legends等现代FPS游戏
                float jumpHeight = 1.2f; // 跳跃高度（米）- 稍微提高高度，保持合理的跳跃感觉
                float maxJumpDuration = 1f; // 最大跳跃持续时间（秒）- 增加持续时间，减慢跳跃
                float gravityMultiplier = 1.6f; // 重力倍数 - 降低重力，让跳跃更慢更平滑
                
                // 高帧率优化：使用实际帧时间而非固定时间步长，避免与游戏循环不同步导致的抖动
                // 使用固定时间步长会导致在144fps等高帧率下位置更新频率与游戏物理更新不匹配
                
                Vector3 startPosition = Vector3.zero;
                Vector3 horizontalVelocity = Vector3.zero;
                float gravity = 0f;
                float initialVerticalVelocity = 0f;
                float timeToPeak = 0f; // 到达最高点的时间
                
                try
                {
                // 获取初始位置
                startPosition = mainCharacter.transform.position;
                
                // 获取重力加速度（Unity中重力是负数，向下）
                gravity = Mathf.Abs(Physics.gravity.y);
                if (gravity < 0.1f)
                {
                    gravity = 9.81f; // 默认重力值
                }
                
                // 应用重力倍数 - 现代FPS使用较大的重力，让跳跃快速有力
                gravity = gravity * gravityMultiplier;
                
                // 计算初始垂直速度（使用物理公式：v0 = sqrt(2 * g * h)）
                // 现代FPS的跳跃特点：快速上升，快速下落
                // 直接使用新重力值计算，保持精确的跳跃高度
                initialVerticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                
                // 计算到达最高点的时间（v = v0 - g*t = 0 => t = v0/g）
                timeToPeak = initialVerticalVelocity / gravity;
                
                FPLogger.Log("跳跃物理参数: 高度={0}m, 重力={1}m/s², 初始速度={2:F2}m/s, 到达最高点时间={3:F3}s", 
                    jumpHeight, gravity, initialVerticalVelocity, timeToPeak);
                
                // 获取当前水平速度（跳跃不衰减速度）
            Vector3 currentVelocity = movementControl.Velocity;
                float horizontalSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
                
                // 跳跃水平速度计算：不衰减，保持100%速度
                Vector3 moveInput = movementControl.MoveInput;
                if (moveInput.magnitude > 0.1f)
                {
                    Vector3 horizontalDir = new Vector3(moveInput.x, 0f, moveInput.z).normalized;
                    
                    // 判断当前是跑步还是行走状态
                    bool isRunning = movementControl.Running;
                    float runSpeed = movementControl.runSpeed;
                    float walkSpeed = movementControl.walkSpeed;
                    
                    if (isRunning)
                    {
                        // 跑步状态下跳跃：降低到80%跑步速度（减慢速度，防止穿墙）
                        horizontalSpeed = runSpeed * 0.8f;
                    }
                    else
                    {
                        // 行走状态下往前跳跃：速度为跑步状态的50%（减慢速度）
                        horizontalSpeed = runSpeed * 0.5f;
                    }
                    
                    // 增加跳跃距离：水平速度增加25%（1.25倍）
                    horizontalSpeed = horizontalSpeed * 1.25f;
                    
                    horizontalVelocity = horizontalDir * horizontalSpeed;
                }
                else if (horizontalSpeed > 0.1f)
                {
                    // 如果没有移动输入但有速度，保持当前速度的100%（不衰减）
                    horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                }
                else
                {
                    // 没有水平速度，只进行垂直跳跃
                    horizontalVelocity = Vector3.zero;
                }
                
                FPLogger.Log("开始跳跃协程: 高度={0}m, 初始垂直速度={1}m/s, 重力={2}m/s², 水平速度={3}m/s, 预计水平距离={4}m", 
                    jumpHeight, initialVerticalVelocity, gravity, horizontalVelocity.magnitude, 
                    horizontalVelocity.magnitude * maxJumpDuration);
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "跳跃协程初始化异常");
                jumpCoroutine = null;
                // yield break 会正常执行 finally 块，确保恢复狗的传送
                yield break;
            }
            
            // 检查设置：是否允许在跳跃途中翻滚
            bool allowDashDuringJump = OptionsHelper.LoadInt(OptionsUIConstants.AllowDashDuringJumpKey, 1) == 1;
            
            // 执行跳跃动画 - 使用速度系统防止穿墙
            float elapsedTime = 0f;
            float lastVerticalVelocity = float.MinValue; // 用于检测到达最高点
            bool hasReachedPeak = false;
            bool hasStartedRising = false; // 标记是否已经开始上升
            float minJumpTime = 0.05f; // 最小跳跃时间（秒）
            
            // 高帧率优化：记录上一帧的位置，用于平滑插值
            Vector3 lastFramePosition = startPosition;
            float lastUpdateTime = 0f;
            
            // 立即设置初始速度，避免延迟
            // 在跳跃开始时立即应用初始垂直速度，让跳跃立即开始
            try
            {
                // 立即设置水平速度
                movementControl.SetForceMoveVelocity(horizontalVelocity);
                
                // 立即设置初始垂直位置（稍微向上，开始跳跃）
                Vector3 initialPos = mainCharacter.transform.position;
                Vector3 initialJumpPos = initialPos + Vector3.up * 0.05f; // 立即向上移动一点，确保跳跃开始
                mainCharacter.SetPosition(initialJumpPos);
                lastFramePosition = initialJumpPos;
                
                FPLogger.Log("跳跃立即开始: 初始位置={0}, 水平速度={1}m/s", initialJumpPos, horizontalVelocity.magnitude);
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "跳跃初始化异常");
            }
            
            // 高帧率优化：使用基于实际时间的循环，而不是固定帧数
            // 这样可以确保在不同帧率下都有相同的物理行为
            float startRealTime = Time.unscaledTime;
            
            while (elapsedTime < maxJumpDuration)
            {
                // 等待下一帧，确保与游戏循环同步（高帧率优化）
                // 使用yield return null保持更新频率，同时基于实际时间计算物理状态
                yield return null;
                
                try
                {
                    // 检查是否还在第一人称模式
                    if (!isFirstPersonMode || mainCharacter == null || movementControl == null)
                    {
                        FPLogger.Log("跳跃中断: 退出第一人称模式或角色为空");
                        break;
                    }
                    
                    // 如果设置为不允许翻滚，检查并阻止翻滚动作
                    if (!allowDashDuringJump && mainCharacter.dashAction != null)
                    {
                        try
                        {
                            // 检查翻滚是否正在运行
                            var runningProperty = mainCharacter.dashAction.GetType().GetProperty("Running");
                            if (runningProperty != null)
                            {
                                bool isDashRunning = (bool)runningProperty.GetValue(mainCharacter.dashAction);
                                if (isDashRunning)
                                {
                                    // 如果正在翻滚，停止它
                                    var stopMethod = mainCharacter.dashAction.GetType().GetMethod("StopAction");
                                    if (stopMethod != null)
                                    {
                                        stopMethod.Invoke(mainCharacter.dashAction, null);
                                        FPLogger.Log("跳跃中阻止翻滚动作");
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            // 忽略反射错误，不影响跳跃
                            FPLogger.LogWarning("检查翻滚状态时出错: {0}", ex.Message);
                        }
                    }
                    
                    // 高帧率优化：使用实际经过的时间，而不是固定时间步长
                    // 这样可以确保在不同帧率下物理计算一致
                    float currentRealTime = Time.unscaledTime;
                    elapsedTime = currentRealTime - startRealTime;
                    
                    // 计算deltaTime（用于物理计算，但不是用于位置更新频率）
                    float deltaTime = elapsedTime - lastUpdateTime;
                    lastUpdateTime = elapsedTime;
                    
                    // 限制deltaTime，防止异常大的时间步长（比如游戏暂停后恢复）
                    // 同时也限制最小值，避免在高帧率下deltaTime过小导致的数值不稳定
                    if (deltaTime > 0.1f)
                    {
                        deltaTime = 0.016f; // 限制为约60fps的时间步长
                    }
                    else if (deltaTime < 0.001f)
                    {
                        deltaTime = 0.001f; // 限制最小时间步长，避免数值不稳定
                    }
                    
                    // 计算当前垂直速度：v = v0 - g*t
                    float currentVerticalVelocity = initialVerticalVelocity - gravity * elapsedTime;
                    
                    // 检查是否已经开始上升（垂直速度大于0）
                    if (!hasStartedRising && currentVerticalVelocity > 0.1f)
                    {
                        hasStartedRising = true;
                        FPLogger.Log("开始上升: 时间={0:F3}s, 垂直速度={1:F2}m/s", elapsedTime, currentVerticalVelocity);
                    }
                    
                    // 检查是否到达最高点（垂直速度从正变为负）
                    if (!hasReachedPeak && hasStartedRising && lastVerticalVelocity != float.MinValue && lastVerticalVelocity > 0f && currentVerticalVelocity <= 0f)
                    {
                        hasReachedPeak = true;
                        FPLogger.Log("到达跳跃最高点: 时间={0:F3}s, 垂直速度={1:F2}m/s", elapsedTime, currentVerticalVelocity);
                    }
                    lastVerticalVelocity = currentVerticalVelocity;
                    
                    // 混合方案：水平速度用速度系统（防止水平穿墙），垂直位置用物理计算
                    // SetForceMoveVelocity会忽略Y值，所以我们需要分别处理
                    
                    // 1. 设置水平速度（防止水平方向穿墙）
                    movementControl.SetForceMoveVelocity(horizontalVelocity);
                    
                    // 2. 获取当前位置（已经由速度系统更新了水平位置）
                    Vector3 currentPos = mainCharacter.transform.position;
                    
                    // 3. 计算垂直位置偏移（使用物理公式）
                    float verticalOffset = initialVerticalVelocity * elapsedTime - 0.5f * gravity * elapsedTime * elapsedTime;
                    
                    // 4. 落地检测：优化检测逻辑，避免过早触发导致卡顿
                    bool shouldLand = false;
                    
                    if (hasStartedRising && elapsedTime > minJumpTime)
                    {
                        // 方法1：检查垂直偏移是否已经回到地面附近（物理计算）
                        // 只有当垂直偏移小于等于0，并且已经到达最高点，才认为可以落地
                        if (hasReachedPeak && verticalOffset <= 0f && elapsedTime > timeToPeak * 1.1f)
                        {
                            // 使用更严格的地面检测，避免在接近地面时就触发
                            // 只有当真正在地面上时才落地
                            if (movementControl.IsOnGround)
                            {
                                shouldLand = true;
                                FPLogger.Log("跳跃完成: 已落地 (时间 {0:F3}s, 垂直偏移 {1:F3}m, 地面检测)", elapsedTime, verticalOffset);
                            }
                            // 如果垂直偏移已经明显低于起始位置，也认为已落地
                            else if (verticalOffset < -0.05f && currentVerticalVelocity < -1f)
                            {
                                shouldLand = true;
                                FPLogger.Log("跳跃完成: 已落地 (时间 {0:F3}s, 垂直偏移 {1:F3}m, 物理计算)", elapsedTime, verticalOffset);
                            }
                        }
                        // 方法2：如果垂直速度向下且很小，并且已经明显超过最高点时间，检查是否在地面上
                        else if (hasReachedPeak && currentVerticalVelocity < -3f && elapsedTime > timeToPeak * 1.5f)
                        {
                            // 只有在垂直速度较大且确实在地面上时才落地
                            if (movementControl.IsOnGround && verticalOffset < 0.1f)
                            {
                                shouldLand = true;
                                FPLogger.Log("跳跃完成: 已落地 (时间 {0:F3}s, 垂直速度 {1:F2}m/s, 延迟检测)", elapsedTime, currentVerticalVelocity);
                            }
                        }
                    }
                    
                    if (shouldLand)
                    {
                        // 平滑落地：设置到精确的地面位置
                        Vector3 landPos = mainCharacter.transform.position;
                        landPos.y = startPosition.y; // 精确回到起始高度
                        mainCharacter.SetPosition(landPos);
                        
                        // 停止强制移动，让游戏正常处理
                        movementControl.SetForceMoveVelocity(Vector3.zero);
                        break;
                    }
                    
                    // 4. 只更新垂直位置（水平位置由速度系统处理，不会穿墙）
                    Vector3 targetPos = new Vector3(currentPos.x, startPosition.y + verticalOffset, currentPos.z);
                    
                    // 5. 碰撞检测：检查垂直方向是否有障碍物（只在上升阶段或明显移动时检测）
                    // 高帧率优化：使用上一帧的垂直偏移，而不是计算lastVerticalOffset
                    float lastVerticalOffset = lastFramePosition.y - startPosition.y;
                    float verticalDelta = verticalOffset - lastVerticalOffset;
                    
                    // 只在有明显垂直移动时进行碰撞检测，避免在接近地面时的频繁检测导致卡顿
                    if (Mathf.Abs(verticalDelta) > 0.02f && verticalOffset > -0.15f) // 接近地面时不检测，避免卡顿
                    {
                        Vector3 raycastStart = new Vector3(currentPos.x, startPosition.y + lastVerticalOffset, currentPos.z);
                        Vector3 raycastDirection = verticalDelta > 0f ? Vector3.up : Vector3.down;
                        float raycastDistance = Mathf.Abs(verticalDelta) + 0.15f;
                        
                        // 使用Raycast检查垂直方向碰撞
                        RaycastHit hit;
                        if (Physics.Raycast(raycastStart, raycastDirection, out hit, raycastDistance, ~0, QueryTriggerInteraction.Ignore))
                        {
                            // 碰到障碍物，调整目标位置
                            float hitHeight = hit.point.y;
                            if (verticalDelta > 0f)
                            {
                                // 上升阶段：不能超过障碍物
                                targetPos.y = Mathf.Min(targetPos.y, hitHeight - 0.15f);
                                FPLogger.Log("跳跃碰到上方障碍物，调整高度: {0:F2}m -> {1:F2}m", startPosition.y + verticalOffset, targetPos.y);
                            }
                            else if (verticalOffset > 0.1f) // 只在明显高于地面时才调整，避免在接近地面时卡顿
                            {
                                // 下降阶段：不能低于障碍物（但在接近地面时不调整，让落地检测处理）
                                targetPos.y = Mathf.Max(targetPos.y, hitHeight + 0.15f);
                            }
                        }
                    }
                    
                    // 高帧率优化：直接设置位置，但使用固定时间间隔减少更新频率
                    // 这样可以避免在高帧率下过于频繁的位置更新导致的抖动
                    // 同时保持物理计算的精确性（基于实际时间）
                    if (!shouldLand)
                    {
                        // 直接设置位置（保持响应性，不使用平滑插值避免延迟）
                        // 在高帧率下，位置更新频率会自然增加，但这不会导致抖动
                        // 抖动的原因通常是位置更新与相机更新不同步，这里我们已经使用了yield return null确保同步
                        mainCharacter.SetPosition(targetPos);
                        lastFramePosition = targetPos;
                    }
                    else
                    {
                        lastFramePosition = targetPos;
                    }
                    
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "跳跃协程帧更新异常");
                    break;
                }
            }
            
                // 确保停止强制移动
                try
                {
                    if (movementControl != null)
                    {
                        movementControl.SetForceMoveVelocity(Vector3.zero);
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "清除强制移动速度异常");
                }
                
                // 落地后立即清除协程引用，允许立即再次跳跃
                // 不需要等待，因为SetPosition已经设置了正确的位置
                jumpCoroutine = null;
                
                FPLogger.Log("跳跃协程完成: 总时间={0:F3}s", elapsedTime);
            }
            finally
            {
                // 无论跳跃是正常结束还是被中断（包括 yield break），都要恢复狗的传送
                EnablePetTeleportAfterJump();
            }
        }

        /// <summary>
        /// 跳跃输入回调（新输入系统）
        /// </summary>
        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (!context.started)
            {
                return;
            }

            FPLogger.Log("新输入系统检测到跳跃输入");
            DoJump();
        }

        /// <summary>
        /// 禁用狗的传送（在跳跃期间）
        /// 使用反射从OnSetPositionEvent事件中移除OnMainCharacterSetPosition方法
        /// </summary>
        private void DisablePetTeleportDuringJump()
        {
            try
            {
                // 清空之前的记录
                petAIEventInfos.Clear();

                // 获取PetAI类型（尝试从所有已加载的程序集中查找）
                System.Type petAIType = null;
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (System.Reflection.Assembly assembly in assemblies)
                {
                    try
                    {
                        petAIType = assembly.GetType("PetAI");
                        if (petAIType != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的程序集
                    }
                }

                if (petAIType == null)
                {
                    FPLogger.LogWarning("无法找到PetAI类型，跳过禁用狗传送");
                    return;
                }

                // 查找场景中所有的PetAI组件
                MonoBehaviour[] allMonoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();
                int foundCount = 0;

                foreach (MonoBehaviour mb in allMonoBehaviours)
                {
                    if (mb == null || mb.GetType() != petAIType)
                    {
                        continue;
                    }

                    try
                    {
                        object petAI = mb;
                        
                        // 获取master字段（CharacterMainControl类型）
                        FieldInfo masterField = petAIType.GetField("master", BindingFlags.Public | BindingFlags.Instance);
                        if (masterField == null)
                        {
                            continue;
                        }

                        object masterCharacter = masterField.GetValue(petAI);
                        if (masterCharacter == null)
                        {
                            continue;
                        }

                        // 检查是否是主角色
                        System.Type characterType = masterCharacter.GetType();
                        PropertyInfo isMainCharacterProp = characterType.GetProperty("IsMainCharacter");
                        if (isMainCharacterProp != null)
                        {
                            bool isMain = (bool)isMainCharacterProp.GetValue(masterCharacter);
                            if (!isMain)
                            {
                                continue; // 不是主角色，跳过
                            }
                        }

                        // 获取OnMainCharacterSetPosition方法
                        MethodInfo onSetPositionMethod = petAIType.GetMethod("OnMainCharacterSetPosition", 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (onSetPositionMethod == null)
                        {
                            continue;
                        }

                        // 获取CharacterMainControl的OnSetPositionEvent事件
                        System.Type characterMainControlType = masterCharacter.GetType();
                        EventInfo eventInfo = characterMainControlType.GetEvent("OnSetPositionEvent");
                        if (eventInfo == null)
                        {
                            continue;
                        }

                        // 获取事件的底层委托字段（C#事件实际上是私有的委托字段）
                        FieldInfo eventField = characterMainControlType.GetField("OnSetPositionEvent", 
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                        if (eventField == null)
                        {
                            // 尝试获取带下划线的私有字段（某些编译器会这样命名）
                            eventField = characterMainControlType.GetField("OnSetPositionEvent", 
                                BindingFlags.NonPublic | BindingFlags.Instance);
                        }

                        if (eventField == null)
                        {
                            // 使用RemoveEventHandler方法（这是标准的事件移除方法）
                            try
                            {
                                // 创建委托
                                System.Type actionType = typeof(System.Action<,>).MakeGenericType(
                                    characterMainControlType, typeof(Vector3));
                                System.Delegate handler = System.Delegate.CreateDelegate(actionType, petAI, onSetPositionMethod);
                                
                                // 从事件中移除
                                eventInfo.RemoveEventHandler(masterCharacter, handler);
                                
                                // 保存信息以便恢复
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
                                FPLogger.Log("已禁用PetAI传送: PetAI={0}, Master={1}", petAI, masterCharacter);
                            }
                            catch (System.Exception ex)
                            {
                                FPLogger.LogWarning("移除PetAI事件失败: {0}", ex.Message);
                            }
                        }
                        else
                        {
                            // 直接操作委托字段
                            try
                            {
                                object eventHandler = eventField.GetValue(masterCharacter);
                                
                                // 创建委托
                                System.Type actionType = typeof(System.Action<,>).MakeGenericType(
                                    characterMainControlType, typeof(Vector3));
                                System.Delegate handler = System.Delegate.CreateDelegate(actionType, petAI, onSetPositionMethod);
                                
                                // 从委托中移除
                                if (eventHandler != null)
                                {
                                    System.Delegate combinedDelegate = System.Delegate.Remove((System.Delegate)eventHandler, handler);
                                    eventField.SetValue(masterCharacter, combinedDelegate);
                                    
                                    // 保存信息以便恢复
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
                                    FPLogger.Log("已禁用PetAI传送（通过字段）: PetAI={0}, Master={1}", petAI, masterCharacter);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                FPLogger.LogWarning("操作PetAI事件字段失败: {0}", ex.Message);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        FPLogger.LogWarning("处理PetAI时出错: {0}", ex.Message);
                    }
                }

                if (foundCount > 0)
                {
                    FPLogger.Log("已禁用 {0} 个PetAI的传送功能", foundCount);
                }
                else
                {
                    FPLogger.Log("未找到需要禁用的PetAI组件");
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "禁用PetAI传送时发生异常");
            }
        }

        /// <summary>
        /// 恢复狗的传送（跳跃结束后）
        /// 使用反射重新添加OnMainCharacterSetPosition方法到OnSetPositionEvent事件
        /// </summary>
        private void EnablePetTeleportAfterJump()
        {
            try
            {
                if (petAIEventInfos == null || petAIEventInfos.Count == 0)
                {
                    return;
                }

                int restoredCount = 0;

                foreach (PetAIEventInfo info in petAIEventInfos)
                {
                    try
                    {
                        if (info.petAIInstance == null || info.masterCharacter == null || info.onSetPositionMethod == null)
                        {
                            continue;
                        }

                        // 方法1：使用EventInfo的AddEventHandler方法
                        if (info.eventInfo != null && info.eventHandler != null)
                        {
                            try
                            {
                                info.eventInfo.AddEventHandler(info.masterCharacter, (System.Delegate)info.eventHandler);
                                restoredCount++;
                                FPLogger.Log("已恢复PetAI传送（通过EventInfo）: PetAI={0}", info.petAIInstance);
                                continue;
                            }
                            catch (System.Exception ex)
                            {
                                FPLogger.LogWarning("通过EventInfo恢复失败: {0}", ex.Message);
                            }
                        }

                        // 方法2：直接操作委托字段
                        if (info.eventField != null)
                        {
                            try
                            {
                                object currentHandler = info.eventField.GetValue(info.masterCharacter);
                                
                                // 创建委托
                                System.Type characterMainControlType = info.masterCharacter.GetType();
                                System.Type actionType = typeof(System.Action<,>).MakeGenericType(
                                    characterMainControlType, typeof(Vector3));
                                System.Delegate handler = System.Delegate.CreateDelegate(actionType, info.petAIInstance, info.onSetPositionMethod);
                                
                                // 添加到委托
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
                                FPLogger.Log("已恢复PetAI传送（通过字段）: PetAI={0}", info.petAIInstance);
                            }
                            catch (System.Exception ex)
                            {
                                FPLogger.LogWarning("通过字段恢复失败: {0}", ex.Message);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        FPLogger.LogWarning("恢复PetAI传送时出错: {0}", ex.Message);
                    }
                }

                if (restoredCount > 0)
                {
                    FPLogger.Log("已恢复 {0} 个PetAI的传送功能", restoredCount);
                }

                // 清空记录
                petAIEventInfos.Clear();
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "恢复PetAI传送时发生异常");
            }
        }

        #endregion
    }
}

