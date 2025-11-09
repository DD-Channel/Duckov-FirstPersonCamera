using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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
            // 跳跃参数 - 现代FPS风格设计（已优化：减慢速度，防止穿墙）
            // 参考：CS:GO, Valorant, Apex Legends等现代FPS游戏
            float jumpHeight = 0.65f; // 跳跃高度（米）- 稍微提高高度，保持合理的跳跃感觉
            float timeStep = 0.016f; // 时间步长（16ms，60fps更新频率），更平滑的跳跃
            float maxJumpDuration = 0.6f; // 最大跳跃持续时间（秒）- 增加持续时间，减慢跳跃
            float gravityMultiplier = 2.0f; // 重力倍数 - 降低重力，让跳跃更慢更平滑
            int maxFrames = Mathf.CeilToInt(maxJumpDuration / timeStep);
            
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
                yield break;
            }
            
            // 执行跳跃动画 - 使用速度系统防止穿墙
            float elapsedTime = 0f;
            float lastVerticalVelocity = float.MinValue; // 用于检测到达最高点
            bool hasReachedPeak = false;
            bool hasStartedRising = false; // 标记是否已经开始上升
            float minJumpTime = 0.05f; // 最小跳跃时间（秒）
            
            // 使用速度系统：每帧计算速度并设置，让游戏的运动系统处理碰撞
            for (int i = 0; i < maxFrames; i++)
            {
                try
                {
                    // 检查是否还在第一人称模式
                    if (!isFirstPersonMode || mainCharacter == null || movementControl == null)
                    {
                        FPLogger.Log("跳跃中断: 退出第一人称模式或角色为空");
                        break;
                    }
                    
                    // 更新经过的时间
                    elapsedTime += timeStep;
                    
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
                    
                    // 落地检测：检查是否在地面上或垂直速度已经很小
                    bool shouldLand = false;
                    
                    if (hasStartedRising && elapsedTime > minJumpTime)
                    {
                        // 方法1：检查是否在地面上（最可靠）
                        if (hasReachedPeak && movementControl.IsOnGround)
                        {
                            shouldLand = true;
                            FPLogger.Log("跳跃完成: 已落地 (时间 {0:F3}s, 帧 {1}, 地面检测)", elapsedTime, i);
                        }
                        // 方法2：如果垂直速度向下且很小，并且已经到达最高点，认为已落地
                        else if (hasReachedPeak && currentVerticalVelocity < -2f && elapsedTime > timeToPeak * 1.2f)
                        {
                            // 检查是否接近地面
                            if (movementControl.IsOnGround || currentVerticalVelocity < -5f)
                            {
                                shouldLand = true;
                                FPLogger.Log("跳跃完成: 已落地 (时间 {0:F3}s, 帧 {1}, 垂直速度 {2:F2}m/s)", elapsedTime, i, currentVerticalVelocity);
                            }
                        }
                    }
                    
                    if (shouldLand)
                    {
                        // 停止强制移动，让游戏正常处理
                        movementControl.SetForceMoveVelocity(Vector3.zero);
                        break;
                    }
                    
                    // 混合方案：水平速度用速度系统（防止水平穿墙），垂直位置用物理计算
                    // SetForceMoveVelocity会忽略Y值，所以我们需要分别处理
                    
                    // 1. 设置水平速度（防止水平方向穿墙）
                    movementControl.SetForceMoveVelocity(horizontalVelocity);
                    
                    // 2. 计算垂直位置（使用物理公式）
                    float verticalOffset = initialVerticalVelocity * elapsedTime - 0.5f * gravity * elapsedTime * elapsedTime;
                    
                    // 3. 获取当前位置（已经由速度系统更新了水平位置）
                    Vector3 currentPos = mainCharacter.transform.position;
                    
                    // 4. 只更新垂直位置（水平位置由速度系统处理，不会穿墙）
                    Vector3 targetPos = new Vector3(currentPos.x, startPosition.y + verticalOffset, currentPos.z);
                    
                    // 5. 碰撞检测：检查垂直方向是否有障碍物
                    float lastVerticalOffset = (i > 0) ? (initialVerticalVelocity * (elapsedTime - timeStep) - 0.5f * gravity * (elapsedTime - timeStep) * (elapsedTime - timeStep)) : 0f;
                    float verticalDelta = verticalOffset - lastVerticalOffset;
                    
                    if (Mathf.Abs(verticalDelta) > 0.01f)
                    {
                        Vector3 raycastStart = new Vector3(currentPos.x, startPosition.y + lastVerticalOffset, currentPos.z);
                        Vector3 raycastDirection = verticalDelta > 0f ? Vector3.up : Vector3.down;
                        float raycastDistance = Mathf.Abs(verticalDelta) + 0.1f;
                        
                        // 使用Raycast检查垂直方向碰撞
                        RaycastHit hit;
                        if (Physics.Raycast(raycastStart, raycastDirection, out hit, raycastDistance, ~0, QueryTriggerInteraction.Ignore))
                        {
                            // 碰到障碍物，调整目标位置
                            float hitHeight = hit.point.y;
                            if (verticalDelta > 0f)
                            {
                                // 上升阶段：不能超过障碍物
                                targetPos.y = Mathf.Min(targetPos.y, hitHeight - 0.1f);
                                FPLogger.Log("跳跃碰到上方障碍物，调整高度: {0:F2}m -> {1:F2}m", startPosition.y + verticalOffset, targetPos.y);
                                
                                // 如果被阻挡，可能需要提前落地
                                if (targetPos.y <= startPosition.y + 0.1f)
                                {
                                    shouldLand = true;
                                }
                            }
                            else
                            {
                                // 下降阶段：不能低于障碍物（可能是地面）
                                targetPos.y = Mathf.Max(targetPos.y, hitHeight + 0.1f);
                                // 如果接近地面，认为已落地
                                if (Mathf.Abs(targetPos.y - startPosition.y) < 0.1f)
                                {
                                    shouldLand = true;
                                }
                            }
                        }
                    }
                    
                    // 设置位置（垂直方向，水平位置已由速度系统更新）
                    if (!shouldLand)
                    {
                        mainCharacter.SetPosition(targetPos);
                    }
                    else
                    {
                        // 如果应该落地，设置到地面位置
                        targetPos.y = startPosition.y;
                        mainCharacter.SetPosition(targetPos);
                        movementControl.SetForceMoveVelocity(Vector3.zero);
                        break;
                    }
                    
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "跳跃协程帧更新异常");
                    break;
                }
                
                // 等待下一帧（必须在try-catch之外）
                yield return new WaitForSeconds(timeStep);
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

        #endregion
    }
}

