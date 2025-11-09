using UnityEngine;
using UnityEngine.InputSystem;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.Patches.Movement;

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

            // 执行跳跃：直接修改角色的垂直速度
            var movementControl = mainCharacter.movementControl;
            if (movementControl == null)
            {
                FPLogger.LogWarning("跳跃失败: movementControl为null");
                return;
            }

            // 检查是否在地面上（只有在地面上才能跳跃）
            if (!movementControl.IsOnGround)
            {
                FPLogger.Log("跳跃失败: 不在地面上");
                return; // 不在地面上时不能跳跃
            }

            // 消耗体力
            mainCharacter.UseStamina(staminaCost);
            FPLogger.Log("开始跳跃，消耗体力: {0}", staminaCost);

            // 获取当前移动方向或使用向上方向
            Vector3 jumpDirection = Vector3.up;
            float jumpSpeed = 5f; // 跳跃速度

            // 如果有移动输入，可以添加水平分量
            Vector3 moveInput = movementControl.MoveInput;
            if (moveInput.magnitude > 0.1f)
            {
                // 添加水平移动分量（30%水平，70%垂直）
                Vector3 horizontalDir = new Vector3(moveInput.x, 0f, moveInput.z).normalized;
                jumpDirection = (horizontalDir * 0.3f + Vector3.up * 0.7f).normalized;
            }

            Vector3 jumpVelocity = jumpDirection * jumpSpeed;
            FPLogger.Log("跳跃速度: {0}, Y值: {1}", jumpVelocity, jumpVelocity.y);

            // 设置待应用的跳跃速度Y值（补丁会在UpdateMovement中处理）
            // 关键：补丁会暂停地面约束（PauseGroundConstraint），避免 CharacterMovement 重置速度
            JumpVelocityPatch.SetPendingJumpVelocityY(jumpVelocity.y);

            // 设置水平速度分量
            Vector3 horizontalVelocity = new Vector3(
                jumpVelocity.x * 0.3f,
                0f, // Y值由补丁处理
                jumpVelocity.z * 0.3f
            );

            // 获取当前速度并添加水平分量
            Vector3 currentVelocity = movementControl.Velocity;
            Vector3 newVelocity = new Vector3(
                currentVelocity.x + horizontalVelocity.x,
                jumpVelocity.y, // 这个Y值会被UpdateForceMove覆盖，但补丁会在Prefix中恢复它
                currentVelocity.z + horizontalVelocity.z
            );

            FPLogger.Log("设置强制移动速度: {0}", newVelocity);
            
            // 使用SetForceMoveVelocity设置速度
            // 补丁会在UpdateForceMove的Prefix中同时修改velocity.y和forceMoveVelocity.y
            mainCharacter.SetForceMoveVelocity(newVelocity);
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

