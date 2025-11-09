using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 武器检视系统模块
    /// 负责处理武器检视动画：拿起武器、翻转查看、恢复原位
    /// 支持被其他动作打断（射击、瞄准、换弹等）
    /// </summary>
    public partial class FirstPersonCameraController
    {
        // 在协程中复用，确保在所有相机与动画更新后再驱动武器位置，避免抖动
        private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();
        #region 武器检视系统字段
        /// <summary>
        /// 是否正在检视武器
        /// </summary>
        private bool isInspectingWeapon = false;
        
        /// <summary>
        /// 检视前激光是否开启（用于恢复）
        /// </summary>
        private bool laserWasEnabledBeforeInspect = true;
        
        /// <summary>
        /// 武器检视按键
        /// </summary>
        private KeyCode inspectKey = KeyCode.H;
        
        /// <summary>
        /// 上次检测按键的帧数（避免重复触发）
        /// </summary>
        private int lastInspectKeyFrame = -1;
        
        /// <summary>
        /// 武器检视协程
        /// </summary>
        private Coroutine weaponInspectCoroutine;
        
        /// <summary>
        /// 武器的原始局部位置（检视前保存）
        /// </summary>
        private Vector3 inspectOriginalLocalPos;
        
        /// <summary>
        /// 武器的原始局部旋转（检视前保存）
        /// </summary>
        private Quaternion inspectOriginalLocalRot;
        
        /// <summary>
        /// 检视开始时的武器引用（用于检测武器切换）
        /// </summary>
        private ItemAgent_Gun inspectingWeapon;
        
        /// <summary>
        /// 武器检视时的相机空间偏移（相机坐标系）
        /// 位置：稍微偏右，在面前，稍微向下
        /// </summary>
        private Vector3 inspectCamSpaceOffset = new Vector3(0.15f, -0.1f, 0.6f);
        
        /// <summary>
        /// 武器检视过渡时长（动作之间的过渡时间）
        /// </summary>
        private float inspectTransitionDuration = 0.7f;
        
        /// <summary>
        /// 武器检视每个动作停留时长
        /// </summary>
        private float inspectHoldDuration = 1.0f;
        
        /// <summary>
        /// 武器检视恢复时长（回到原始位置）
        /// </summary>
        private float inspectRestoreDuration = 0.6f;
        
        /// <summary>
        /// 武器检视角度组合列表（X, Y, Z欧拉角）
        /// </summary>
        private readonly Vector3[] inspectAnglePresets = new Vector3[]
        {
            new Vector3(-8f, -85f, -3f),    // 组合1
            new Vector3(-27f, -63f, 44f),  // 组合2
            new Vector3(-55f, -33f, 0f),   // 组合3
            new Vector3(-3f, -75f, 0f),    // 组合4
            new Vector3(-3f, -33f, -12f)   // 组合5
        };
        
        /// <summary>
        /// 按键加载间隔（每N帧加载一次，避免每帧都加载）
        /// </summary>
        private const int INSPECT_KEY_LOAD_INTERVAL = 30;
        #endregion

        #region 武器检视公共方法
        /// <summary>
        /// 开始武器检视
        /// </summary>
        public void StartWeaponInspect()
        {
            // 如果已经在检视，不重复开始
            if (isInspectingWeapon) return;
            
            // 如果不在第一人称模式，不执行
            if (!isFirstPersonMode) return;
            
            // 如果没有武器，不执行
            if (mainCharacter == null) return;
            var gun = mainCharacter.GetGun();
            if (gun == null || gun.transform == null) return;
            
            // 如果正在瞄准，不执行（避免冲突）
            if (IsInAdsState()) return;
            
            // 停止之前的协程（如果有）
            if (weaponInspectCoroutine != null)
            {
                StopCoroutine(weaponInspectCoroutine);
                weaponInspectCoroutine = null;
            }
            
            // 保存激光状态并关闭激光
            try
            {
                laserWasEnabledBeforeInspect = LaserPatch.GetLaserEnabled();
                LaserPatch.SetLaserEnabled(false);
            }
            catch { }
            
            // 保存检视开始时的武器引用（用于检测武器切换）
            inspectingWeapon = gun;
            
            // 开始新的检视协程
            isInspectingWeapon = true;
            weaponInspectCoroutine = StartCoroutine(WeaponInspectCoroutine(gun));
        }
        
        /// <summary>
        /// 停止武器检视（被其他动作打断时调用）
        /// </summary>
        public void StopWeaponInspect()
        {
            if (!isInspectingWeapon) return;
            
            // 停止协程
            if (weaponInspectCoroutine != null)
            {
                StopCoroutine(weaponInspectCoroutine);
                weaponInspectCoroutine = null;
            }
            
            // 恢复武器到原始位置
            RestoreWeaponFromInspect();
            
            // 恢复激光状态
            try
            {
                LaserPatch.SetLaserEnabled(laserWasEnabledBeforeInspect);
            }
            catch { }
            
            // 清除武器引用
            inspectingWeapon = null;
            
            isInspectingWeapon = false;
        }
        
        /// <summary>
        /// 检查是否正在检视武器
        /// </summary>
        public bool IsInspectingWeapon => isInspectingWeapon;
        
        /// <summary>
        /// 更新武器检视输入
        /// </summary>
        /// <param name="uiBlocking">是否被UI阻挡</param>
        private void UpdateWeaponInspectInput(bool uiBlocking)
        {
            if (uiBlocking) return;
            
            // 定期加载按键配置（避免每帧都加载）
            if (Time.frameCount % INSPECT_KEY_LOAD_INTERVAL == 0)
            {
                LoadInspectKeyFromOptions();
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
                StartWeaponInspect();
            }
        }
        
        /// <summary>
        /// 从选项加载检视按键
        /// </summary>
        private void LoadInspectKeyFromOptions()
        {
            try
            {
                inspectKey = OptionsHelper.LoadKeyCode(
                    OptionsUIConstants.WeaponInspectKeyCodeKey, KeyCode.H);
            }
            catch
            {
                inspectKey = KeyCode.H; // 默认按键
            }
        }
        
        /// <summary>
        /// 从Keyboard获取按键按下状态（本帧按下）
        /// </summary>
        private bool GetKeyPressedThisFrame(Keyboard keyboard, KeyCode keyCode)
        {
            try
            {
                switch (keyCode)
                {
                    case KeyCode.F: return keyboard.fKey.wasPressedThisFrame;
                    case KeyCode.H: return keyboard.hKey.wasPressedThisFrame;
                    case KeyCode.Q: return keyboard.qKey.wasPressedThisFrame;
                    case KeyCode.E: return keyboard.eKey.wasPressedThisFrame;
                    case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
                    case KeyCode.T: return keyboard.tKey.wasPressedThisFrame;
                    case KeyCode.G: return keyboard.gKey.wasPressedThisFrame;
                    case KeyCode.V: return keyboard.vKey.wasPressedThisFrame;
                    default: return false;
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 武器检视协程
        /// <summary>
        /// 武器检视协程
        /// 随机选择2-3个角度组合，每个动作停留1秒，动作之间过渡0.7秒
        /// </summary>
        private IEnumerator WeaponInspectCoroutine(ItemAgent_Gun gun)
        {
            if (gun == null || gun.transform == null)
            {
                inspectingWeapon = null;
                isInspectingWeapon = false;
                yield break;
            }
            
            var gunTf = gun.transform;
            Transform parentTf = gunTf.parent;
            
            // 保存原始位置和旋转
            inspectOriginalLocalPos = gunTf.localPosition;
            inspectOriginalLocalRot = gunTf.localRotation;
            
            // 相机位置跟踪（用于检测抖动）
            Vector3 lastCamPos = mainCamera.transform.position;
            const float jitterThreshold = 0.002f; // 抖动阈值（米/帧），小于此值认为是抖动，使用更平滑的插值
            const float smoothLerpSpeed = 30f; // 平滑插值速度（静止时使用，减少抖动）
            const float fastLerpSpeed = 60f; // 快速插值速度（移动时使用，快速跟随但保持流畅）
            
            // 随机选择2-3个角度组合
            int numAngles = Random.Range(2, 4); // 2或3个
            System.Collections.Generic.List<int> selectedIndices = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> availableIndices = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4 };
            
            for (int i = 0; i < numAngles; i++)
            {
                int randomIndex = Random.Range(0, availableIndices.Count);
                int selectedIndex = availableIndices[randomIndex];
                selectedIndices.Add(selectedIndex);
                availableIndices.RemoveAt(randomIndex);
            }
            
            // 第一阶段：拿起武器到面前（使用第一个角度组合）
            Vector3 firstAngle = inspectAnglePresets[selectedIndices[0]];
            float elapsed = 0f;
            Vector3 startPos = inspectOriginalLocalPos;
            Quaternion startRot = inspectOriginalLocalRot;
            
            while (elapsed < inspectTransitionDuration)
            {
                // 检查是否被打断
                if (ShouldInterruptInspect())
                {
                    RestoreWeaponFromInspect();
                    inspectingWeapon = null;
                    isInspectingWeapon = false;
                    yield break;
                }

                // 等到本帧所有相机/LateUpdate完成后再计算，避免与相机更新不同步导致的抖动
                yield return _waitEndOfFrame;

                // 每帧重新计算目标位置（基于当前相机位置），防止受行走晃动影响
                Vector3 currentCamPos = mainCamera.transform.position;
                Quaternion currentCamRot = mainCamera.transform.rotation;
                Vector3 currentTargetWorldPos = currentCamPos + currentCamRot * inspectCamSpaceOffset;
                Quaternion currentTargetWorldRot = currentCamRot * Quaternion.Euler(firstAngle.x, firstAngle.y, firstAngle.z);
                
                float t = elapsed / inspectTransitionDuration;
                // 使用平滑曲线（easeOutCubic）
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                // 计算起始和目标位置（基于当前相机位置）
                Vector3 currentStartWorldPos = parentTf != null ? parentTf.TransformPoint(startPos) : gunTf.position;
                Quaternion currentStartWorldRot = parentTf != null ? parentTf.rotation * startRot : gunTf.rotation;
                
                // 插值计算当前位置和旋转
                Vector3 currentWorldPos = Vector3.Lerp(currentStartWorldPos, currentTargetWorldPos, t);
                Quaternion currentWorldRot = Quaternion.Slerp(currentStartWorldRot, currentTargetWorldRot, t);
                
                // 强制设置武器位置和旋转（使用世界空间，避免受父节点动画影响）
                if (parentTf != null)
                {
                    Vector3 currentLocalPos = parentTf.InverseTransformPoint(currentWorldPos);
                    Quaternion currentLocalRot = Quaternion.Inverse(parentTf.rotation) * currentWorldRot;
                    gunTf.localPosition = currentLocalPos;
                    gunTf.localRotation = currentLocalRot;
                }
                else
                {
                    gunTf.position = currentWorldPos;
                    gunTf.rotation = currentWorldRot;
                }
                
                elapsed += Time.unscaledDeltaTime;
            }
            
            // 遍历所有选中的角度组合
            for (int angleIndex = 0; angleIndex < selectedIndices.Count; angleIndex++)
            {
                Vector3 currentAngle = inspectAnglePresets[selectedIndices[angleIndex]];
                
                // 等待阶段：保持当前角度1秒
                float waitElapsed = 0f;
                while (waitElapsed < inspectHoldDuration)
                {
                    // 检查是否被打断
                    if (ShouldInterruptInspect())
                    {
                        RestoreWeaponFromInspect();
                        inspectingWeapon = null;
                        isInspectingWeapon = false;
                        yield break;
                    }

                    // 等到本帧所有相机/LateUpdate完成后再计算，避免与相机更新不同步导致的抖动
                    yield return _waitEndOfFrame;

                    // 每帧重新计算目标位置（基于当前相机位置），防止受行走晃动影响
                    Vector3 currentCamPos = mainCamera.transform.position;
                    Quaternion currentCamRot = mainCamera.transform.rotation;
                    Vector3 currentTargetWorldPos = currentCamPos + currentCamRot * inspectCamSpaceOffset;
                    Quaternion currentTargetWorldRot = currentCamRot * Quaternion.Euler(currentAngle.x, currentAngle.y, currentAngle.z);
                    
                    // 计算相机移动速度，用于判断是有效移动还是抖动
                    Vector3 camMovement = currentCamPos - lastCamPos;
                    float camSpeed = camMovement.magnitude;
                    lastCamPos = currentCamPos;
                    
                    // 优化策略：始终使用平滑插值，但根据移动速度调整平滑速度
                    // 移动时使用快速平滑（快速跟随但保持流畅），静止时使用慢速平滑（减少抖动）
                    float lerpSpeed = camSpeed > jitterThreshold ? fastLerpSpeed : smoothLerpSpeed;
                    float t = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
                    
                    if (parentTf != null)
                    {
                        Vector3 currentTargetLocalPos = parentTf.InverseTransformPoint(currentTargetWorldPos);
                        Quaternion currentTargetLocalRot = Quaternion.Inverse(parentTf.rotation) * currentTargetWorldRot;
                        
                        // 使用平滑插值，速度根据移动状态自适应
                        gunTf.localPosition = Vector3.Lerp(gunTf.localPosition, currentTargetLocalPos, t);
                        gunTf.localRotation = Quaternion.Slerp(gunTf.localRotation, currentTargetLocalRot, t);
                    }
                    else
                    {
                        // 使用平滑插值，速度根据移动状态自适应
                        gunTf.position = Vector3.Lerp(gunTf.position, currentTargetWorldPos, t);
                        gunTf.rotation = Quaternion.Slerp(gunTf.rotation, currentTargetWorldRot, t);
                    }
                    
                    waitElapsed += Time.unscaledDeltaTime;
                }
                
                // 如果不是最后一个角度，过渡到下一个角度
                if (angleIndex < selectedIndices.Count - 1)
                {
                    Vector3 nextAngle = inspectAnglePresets[selectedIndices[angleIndex + 1]];
                    
                    elapsed = 0f;
                    
                    while (elapsed < inspectTransitionDuration)
                    {
                        // 检查是否被打断
                        if (ShouldInterruptInspect())
                        {
                            RestoreWeaponFromInspect();
                            inspectingWeapon = null;
                            isInspectingWeapon = false;
                            yield break;
                        }

                        // 等到本帧所有相机/LateUpdate完成后再计算，避免与相机更新不同步导致的抖动
                        yield return _waitEndOfFrame;

                        // 每帧重新计算目标位置（基于当前相机位置），防止受行走晃动影响
                        Vector3 currentCamPos = mainCamera.transform.position;
                        Quaternion currentCamRot = mainCamera.transform.rotation;
                        Vector3 currentTargetWorldPos = currentCamPos + currentCamRot * inspectCamSpaceOffset;

                        float t = elapsed / inspectTransitionDuration;
                        // 使用平滑曲线（easeInOutCubic）
                        t = t < 0.5f 
                            ? 4f * t * t * t 
                            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                        
                        // 计算当前旋转（从当前角度过渡到下一个角度）
                        Quaternion startRotWorld = currentCamRot * Quaternion.Euler(currentAngle.x, currentAngle.y, currentAngle.z);
                        Quaternion endRotWorld = currentCamRot * Quaternion.Euler(nextAngle.x, nextAngle.y, nextAngle.z);
                        Quaternion currentTargetWorldRot = Quaternion.Slerp(startRotWorld, endRotWorld, t);
                        
                        // 计算相机移动速度，用于判断是有效移动还是抖动
                        Vector3 camMovement = currentCamPos - lastCamPos;
                        float camSpeed = camMovement.magnitude;
                        lastCamPos = currentCamPos;
                        
                        // 优化策略：始终使用平滑插值，但根据移动速度调整平滑速度
                        // 移动时使用快速平滑（快速跟随但保持流畅），静止时使用慢速平滑（减少抖动）
                        float lerpSpeed = camSpeed > jitterThreshold ? fastLerpSpeed : smoothLerpSpeed;
                        float smoothT = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
                        
                        if (parentTf != null)
                        {
                            Vector3 currentTargetLocalPos = parentTf.InverseTransformPoint(currentTargetWorldPos);
                            Quaternion currentTargetLocalRot = Quaternion.Inverse(parentTf.rotation) * currentTargetWorldRot;
                            
                            // 使用平滑插值，速度根据移动状态自适应
                            gunTf.localPosition = Vector3.Lerp(gunTf.localPosition, currentTargetLocalPos, smoothT);
                            gunTf.localRotation = Quaternion.Slerp(gunTf.localRotation, currentTargetLocalRot, smoothT);
                        }
                        else
                        {
                            // 使用平滑插值，速度根据移动状态自适应
                            gunTf.position = Vector3.Lerp(gunTf.position, currentTargetWorldPos, smoothT);
                            gunTf.rotation = Quaternion.Slerp(gunTf.rotation, currentTargetWorldRot, smoothT);
                        }
                        
                        elapsed += Time.unscaledDeltaTime;
                    }
                }
            }
            
            // 检查是否被打断
            if (ShouldInterruptInspect())
            {
                RestoreWeaponFromInspect();
                inspectingWeapon = null;
                isInspectingWeapon = false;
                yield break;
            }
            
            // 最后阶段：恢复武器到原始位置
            elapsed = 0f;
            Vector3 restoreStartPos = gunTf.localPosition;
            Quaternion restoreStartRot = gunTf.localRotation;
            
            while (elapsed < inspectRestoreDuration)
            {
                // 检查是否被打断
                if (ShouldInterruptInspect())
                {
                    RestoreWeaponFromInspect();
                    inspectingWeapon = null;
                    isInspectingWeapon = false;
                    yield break;
                }

                // 等到本帧所有相机/LateUpdate完成后再计算，避免与相机更新不同步导致的抖动
                yield return _waitEndOfFrame;

                float t = elapsed / inspectRestoreDuration;
                // 使用平滑曲线（easeInCubic）
                t = t * t * t;
                
                if (parentTf != null)
                {
                    gunTf.localPosition = Vector3.Lerp(restoreStartPos, inspectOriginalLocalPos, t);
                    gunTf.localRotation = Quaternion.Slerp(restoreStartRot, inspectOriginalLocalRot, t);
                }
                else
                {
                    // 无父节点时，使用世界空间
                    Vector3 worldRestoreStartPos = gunTf.position;
                    Quaternion worldRestoreStartRot = gunTf.rotation;
                    Vector3 worldRestoreEndPos = inspectOriginalLocalPos; // 如果没有父节点，原始位置就是世界位置
                    Quaternion worldRestoreEndRot = inspectOriginalLocalRot;
                    gunTf.position = Vector3.Lerp(worldRestoreStartPos, worldRestoreEndPos, t);
                    gunTf.rotation = Quaternion.Slerp(worldRestoreStartRot, worldRestoreEndRot, t);
                }
                
                elapsed += Time.unscaledDeltaTime;
            }
            
            // 确保恢复到原始位置
            RestoreWeaponFromInspect();
            
            // 恢复激光状态
            try
            {
                LaserPatch.SetLaserEnabled(laserWasEnabledBeforeInspect);
            }
            catch { }
            
            // 清除武器引用
            inspectingWeapon = null;
            
            // 完成检视
            isInspectingWeapon = false;
            weaponInspectCoroutine = null;
        }
        
        /// <summary>
        /// 检查是否应该打断检视
        /// </summary>
        private bool ShouldInterruptInspect()
        {
            if (mainCharacter == null) return true;
            
            // 如果开始瞄准，打断检视
            if (IsInAdsState()) return true;
            
            // 如果开始射击，打断检视
            try
            {
                bool mousePressed = false;
                if (useNewInputSystem)
                {
                    var m = Mouse.current;
                    if (m != null) mousePressed = m.leftButton.isPressed;
                }
                else
                {
                    mousePressed = Input.GetMouseButton(0);
                }
                if (mousePressed) return true;
            }
            catch { }
            
            // 如果不在第一人称模式，打断检视
            if (!isFirstPersonMode) return true;
            
            // 如果武器被移除，打断检视
            var gun = mainCharacter.GetGun();
            if (gun == null || gun.transform == null) return true;
            
            // 如果武器切换了（当前武器与检视开始时的武器不同），打断检视
            if (inspectingWeapon != null && gun != inspectingWeapon) return true;
            
            // 如果角色在跑步（只检测Shift键，不检测速度，避免行走时误判）
            try
            {
                // 只检测跑步键（Shift），完全忽略速度检测
                bool isRunning = false;
                if (useNewInputSystem)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        isRunning = keyboard.leftShiftKey.isPressed;
                    }
                }
                else
                {
                    isRunning = Input.GetKey(KeyCode.LeftShift);
                }
                
                if (isRunning) return true;
            }
            catch { }
            
            return false;
        }
        
        /// <summary>
        /// 恢复武器到检视前的位置
        /// </summary>
        private void RestoreWeaponFromInspect()
        {
            if (mainCharacter == null) return;
            
            // 优先使用检视开始时的武器引用，如果该武器仍然有效
            ItemAgent_Gun gunToRestore = null;
            if (inspectingWeapon != null && inspectingWeapon.transform != null)
            {
                // 检查这个武器是否仍然是当前武器
                var currentGun = mainCharacter.GetGun();
                if (currentGun == inspectingWeapon)
                {
                    gunToRestore = inspectingWeapon;
                }
            }
            
            // 如果检视开始时的武器不再有效或不是当前武器，尝试获取当前武器
            if (gunToRestore == null)
            {
                gunToRestore = mainCharacter.GetGun();
            }
            
            // 如果仍然没有有效武器，无法恢复
            if (gunToRestore == null || gunToRestore.transform == null) return;
            
            // 恢复武器位置和旋转
            var gunTf = gunToRestore.transform;
            gunTf.localPosition = inspectOriginalLocalPos;
            gunTf.localRotation = inspectOriginalLocalRot;
        }
        #endregion
    }
}
