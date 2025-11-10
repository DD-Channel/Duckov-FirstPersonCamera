using UnityEngine;
using UnityEngine.InputSystem;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 相机位置和旋转更新模块
    /// 负责处理相机位置更新（包括防抖动）、相机旋转和身体对齐
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 相机防抖动系统字段
        /// <summary>
        /// 防抖动当前Y轴偏移值（平滑后的）
        /// </summary>
        private float antiBobCurrentY;
        
        /// <summary>
        /// 防抖动Y轴速度（用于SmoothDamp）
        /// </summary>
        private float antiBobVelY;
        
        /// <summary>
        /// 防抖动当前侧向偏移（XZ平面，沿右轴）
        /// </summary>
        private float antiBobCurrentLat;
        
        /// <summary>
        /// 防抖动侧向速度（用于SmoothDamp）
        /// </summary>
        private float antiBobVelLat;
        
        /// <summary>
        /// 防抖动当前前后偏移（XZ平面，沿前轴）
        /// </summary>
        private float antiBobCurrentFwd;
        
        /// <summary>
        /// 防抖动前后速度（用于SmoothDamp）
        /// </summary>
        private float antiBobVelFwd;
        
        /// <summary>
        /// 防抖动系统是否已初始化
        /// </summary>
        private bool antiBobInitialized;
        
        /// <summary>
        /// 防抖动平滑时间最小值（秒），平滑时间会根据antiBobStrength在区间内映射
        /// </summary>
        private const float ANTI_BOB_SMOOTH_MIN = 0.02f;
        
        /// <summary>
        /// 防抖动平滑时间最大值（秒）
        /// </summary>
        private const float ANTI_BOB_SMOOTH_MAX = 0.25f;
        
        /// <summary>
        /// 防抖动强度阈值，低于此值时不启用防抖动
        /// </summary>
        private const float ANTI_BOB_STRENGTH_THRESHOLD = 0.0001f;
        
        /// <summary>
        /// 是否正在跳跃（用于禁用防抖动的Y轴平滑）
        /// </summary>
        private bool isJumping;
        
        /// <summary>
        /// 跳跃状态持续的帧数
        /// </summary>
        private int jumpingFrameCount;
        
        /// <summary>
        /// 通知防抖动系统正在跳跃
        /// </summary>
        public void NotifyJumping()
        {
            isJumping = true;
            jumpingFrameCount = 0;
            // 重置防抖动状态，让相机立即跟随
            antiBobInitialized = false;
            FPLogger.Log("通知防抖动系统：正在跳跃，重置防抖动状态");
        }
        #endregion

        #region 相机位置和旋转更新
        /// <summary>
        /// 更新相机位置
        /// 计算相机在世界空间中的位置，包括基础偏移、偏头偏移和防抖动处理
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (mainCamera == null) return;

            // 计算基础位置（头部插槽或角色位置 + 高度偏移）
            Vector3 basePos;
            if (headSocket != null)
            {
                basePos = headSocket.position + Vector3.up * cameraHeightOffset;
            }
            else if (mainCharacter != null)
            {
                basePos = mainCharacter.transform.position + Vector3.up * (1.7f + cameraHeightOffset);
            }
            else
            {
                return;
            }

            // 复用一次旋转，减少每帧四元数构造
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 forward = rot * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            
            // 应用前后和左右偏移
            basePos += forward * cameraForwardOffset + right * cameraRightOffset;

            // 相机防抖动：当强度>0时对Y轴和水平轴进行平滑
            if (antiBobStrength > ANTI_BOB_STRENGTH_THRESHOLD)
            {
                // 基于当前视角的水平轴（用于计算水平偏移）
                Vector3 rightFlat = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                Vector3 forwardFlat = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

                // 锚点：角色根节点 + 固定偏移（不包含头部动画产生的抖动，也不包含偏头偏移）
                // 注意：偏头偏移不应该包含在锚点中，因为它不是头部动画，而是用户输入
                Vector3 rootPos = (mainCharacter != null ? mainCharacter.transform.position : basePos);
                Vector3 rotFwd = rot * Vector3.forward;
                Vector3 anchor = rootPos + Vector3.up * (1.7f + cameraHeightOffset)
                                  + rotFwd * cameraForwardOffset
                                  + rightFlat * cameraRightOffset;

                // 计算头部动画引入的局部偏移（这是需要平滑的抖动）
                Vector3 dev = basePos - anchor;
                float devY = dev.y;
                float devLat = Vector3.Dot(dev, rightFlat);
                float devFwd = Vector3.Dot(dev, forwardFlat);

                // 更新跳跃状态：如果标记为跳跃，持续几帧
                if (isJumping)
                {
                    jumpingFrameCount++;
                    // 跳跃状态持续10帧（约0.16秒，60fps），或者直到速度下降到阈值以下
                    if (jumpingFrameCount > 10)
                    {
                        // 检查速度，如果速度已经下降，停止跳跃状态
                        if (mainCharacter != null && mainCharacter.movementControl != null)
                        {
                            Vector3 velocity = mainCharacter.movementControl.Velocity;
                            if (velocity.y < 0.5f)
                            {
                                isJumping = false;
                                FPLogger.Log("跳跃状态结束（速度下降）");
                            }
                        }
                        else
                        {
                            isJumping = false;
                            FPLogger.Log("跳跃状态结束（超时）");
                        }
                    }
                }

                // 初始化或平滑偏移值
                if (!antiBobInitialized)
                {
                    // 首次初始化，直接设置当前值
                    antiBobCurrentY = devY;
                    antiBobVelY = 0f;
                    antiBobCurrentLat = devLat;
                    antiBobVelLat = 0f;
                    antiBobCurrentFwd = devFwd;
                    antiBobVelFwd = 0f;
                    antiBobInitialized = true;
                }
                else if (isJumping)
                {
                    // 高帧率优化：跳跃时完全禁用防抖动系统，让相机立即跟随角色位置
                    // 这样可以避免在高帧率下防抖动系统与位置更新不同步导致的抖动
                    antiBobCurrentY = devY;
                    antiBobVelY = 0f;
                    antiBobCurrentLat = devLat;
                    antiBobVelLat = 0f;
                    antiBobCurrentFwd = devFwd;
                    antiBobVelFwd = 0f;
                }
                else
                {
                    // 根据防抖动强度计算平滑时间（强度越大，平滑时间越长）
                    float smoothTime = Mathf.Lerp(ANTI_BOB_SMOOTH_MIN, ANTI_BOB_SMOOTH_MAX, 
                        Mathf.Clamp01(antiBobStrength));
                    
                    // 使用SmoothDamp平滑各个轴的偏移
                    antiBobCurrentY = Mathf.SmoothDamp(antiBobCurrentY, devY, ref antiBobVelY, 
                        smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                    antiBobCurrentLat = Mathf.SmoothDamp(antiBobCurrentLat, devLat, ref antiBobVelLat, 
                        smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                    antiBobCurrentFwd = Mathf.SmoothDamp(antiBobCurrentFwd, devFwd, ref antiBobVelFwd, 
                        smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                }

                // 重建平滑后的偏移并应用到锚点
                Vector3 smoothedDev = rightFlat * antiBobCurrentLat + 
                                     forwardFlat * antiBobCurrentFwd + 
                                     Vector3.up * antiBobCurrentY;
                basePos = anchor + smoothedDev;
            }
            else
            {
                // 防抖动关闭时，重置初始化状态
                antiBobInitialized = false;
            }

            // 应用偏头偏移（在所有计算之后，作为最终偏移应用）
            // 这样偏头偏移不会影响防抖动系统的计算，避免在跳跃时偏头导致视野闪动
            basePos += right * peekOffset * peekMaxOffset;

            // 应用最终位置
            mainCamera.transform.position = basePos;
        }

        /// <summary>
        /// 更新相机旋转
        /// 处理鼠标输入、后坐力恢复和偏头旋转
        /// 修复：在LateUpdate中直接读取鼠标增量，不依赖Update中的捕获，避免输入系统更新时机导致的跳帧
        /// 关键：不使用Time.deltaTime缩放，因为鼠标增量本身已经是基于输入设备采样率的，直接应用即可
        /// </summary>
        /// <param name="uiBlocking">是否被UI阻挡（UI打开时不响应鼠标输入）</param>
        private void UpdateCameraRotation(bool uiBlocking)
        {
            if (mainCamera == null) return;
            
            // 先处理后坐力回弹
            UpdateRecoilRecovery();
            
            // 处理鼠标输入（仅在UI未阻挡时）
            if (!uiBlocking)
            {
                // 在LateUpdate中直接读取鼠标增量，此时输入系统已经更新完毕
                // 这样可以避免Update中读取时输入系统还未更新，或者Update/LateUpdate调用频率不一致的问题
                float mouseX = 0f;
                float mouseY = 0f;
                
                try
                {
                    if (useNewInputSystem)
                    {
                        var mouse = Mouse.current;
                        if (mouse != null)
                        {
                            Vector2 d = mouse.delta.ReadValue();
                            mouseX = d.x;
                            mouseY = d.y;
                        }
                    }
                    else
                    {
                        mouseX = Input.GetAxis("Mouse X");
                        mouseY = Input.GetAxis("Mouse Y");
                    }
                }
                catch
                {
                    mouseX = 0f;
                    mouseY = 0f;
                }
                
                // 如果设置了抑制下一帧鼠标增量标志，则清零
                if (suppressNextMouseDelta)
                {
                    mouseX = 0f;
                    mouseY = 0f;
                    suppressNextMouseDelta = false;
                }
                
                // 根据当前倍镜倍率选择对应的灵敏度倍数（相对于普通灵敏度）
                float currentSensitivityX = mouseSensitivityX;
                float currentSensitivityY = mouseSensitivityY;
                
                bool isAiming = IsInAdsState();
                if (isAiming)
                {
                    // 尝试获取当前倍镜信息
                    float magnification = 0f;
                    int scopeTypeID = -1;
                    
                    try
                    {
                        var gun = mainCharacter?.GetGun();
                        if (gun != null && gun.Item != null)
                        {
                            var slot = gun.Item.Slots?.GetSlot("Scope");
                            if (slot != null && slot.Content != null)
                            {
                                scopeTypeID = slot.Content.TypeID;
                                magnification = GetScopeMagnification(scopeTypeID);
                            }
                        }
                    }
                    catch
                    {
                        // 获取倍镜信息失败时使用缓存的currentScopeTypeID
                        if (currentScopeTypeID != -1)
                        {
                            magnification = GetScopeMagnification(currentScopeTypeID);
                        }
                    }
                    
                    // 如果获取到了有效的倍率，应用对应的灵敏度倍数
                    if (magnification > 0f)
                    {
                        float sensitivityMultiplier = 1f;
                        
                        // 根据倍率选择对应的灵敏度倍数（同时应用于水平和垂直）
                        if (Mathf.Approximately(magnification, 1.2f))
                        {
                            sensitivityMultiplier = scope1_2xSensitivity;
                        }
                        else if (Mathf.Approximately(magnification, 2f))
                        {
                            sensitivityMultiplier = scope2xSensitivity;
                        }
                        else if (Mathf.Approximately(magnification, 4f))
                        {
                            sensitivityMultiplier = scope4xSensitivity;
                        }
                        else if (Mathf.Approximately(magnification, 8f))
                        {
                            sensitivityMultiplier = scope8xSensitivity;
                        }
                        
                        // 应用倍数到普通灵敏度（分别乘以水平和垂直）
                        currentSensitivityX = mouseSensitivityX * sensitivityMultiplier;
                        currentSensitivityY = mouseSensitivityY * sensitivityMultiplier;
                    }
                }
                
                // 更新yaw和pitch
                // 关键修复：限制每帧最大旋转量，防止高帧率下的跳帧问题
                // 原因：Unity Input System的mouse.delta在某些情况下可能会累积，特别是在高帧率下
                // 通过限制每帧最大旋转量，可以防止突然的大幅跳跃，同时不影响后坐力系统
                // 后坐力系统使用Time.unscaledDeltaTime进行恢复，不应该受到鼠标输入缩放的影响
                // 注意：不使用时间缩放，因为这会影响到后坐力恢复系统的一致性
                float maxRotationPerFrame = 10f; // 每帧最大旋转角度（度），防止跳帧（足够大以保持响应性）
                float deltaYaw = mouseX * currentSensitivityX;
                float deltaPitch = mouseY * currentSensitivityY;
                
                // 限制每帧最大旋转量（防止异常大的delta值导致的跳帧）
                // 这个限制足够大，不会影响正常使用，但可以防止异常情况
                if (Mathf.Abs(deltaYaw) > maxRotationPerFrame)
                {
                    deltaYaw = Mathf.Sign(deltaYaw) * maxRotationPerFrame;
                }
                if (Mathf.Abs(deltaPitch) > maxRotationPerFrame)
                {
                    deltaPitch = Mathf.Sign(deltaPitch) * maxRotationPerFrame;
                }
                
                yaw += deltaYaw;
                pitch -= deltaPitch;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
            }
            
            // 应用偏头旋转（roll角）
            // 取反以匹配直觉：左偏右转，右偏左转
            float roll = -peekOffset * peekMaxRotation;
            mainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        /// <summary>
        /// 将身体朝向对齐到相机朝向
        /// 当身体朝向与相机朝向差异超过阈值时，强制角色转向
        /// </summary>
        private void AlignBodyToCamera()
        {
            if (mainCharacter == null || mainCamera == null) return;
            
            // 获取相机朝向（仅水平方向）
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.0001f) return;
            camForward.Normalize();
            
            // 获取身体朝向（仅水平方向）
            Vector3 bodyForward = mainCharacter.transform.forward;
            bodyForward.y = 0f;
            if (bodyForward.sqrMagnitude < 0.0001f) return;
            bodyForward.Normalize();
            
            // 计算角度差
            float dot = Mathf.Clamp(Vector3.Dot(bodyForward, camForward), -1f, 1f);
            float angleDeg = Mathf.Acos(dot) * Mathf.Rad2Deg;
            
            // 如果角度差超过阈值，强制角色转向
            if (angleDeg > bodyAlignAngleThreshold)
            {
                mainCharacter.movementControl.ForceTurnTo(camForward);
            }
        }
        #endregion
    }
}

