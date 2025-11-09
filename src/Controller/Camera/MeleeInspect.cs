using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 近战检视系统模块
    /// 回旋镖效果：举到面前 -> 旋转着丢出去 -> 旋转着飞回 -> 还原
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 近战检视字段
        private bool isInspectingMelee = false;
        private Coroutine meleeInspectCoroutine;
        /// <summary>
        /// 当前正在检视的近战武器引用（用于检测武器切换）
        /// </summary>
        private ItemAgent_MeleeWeapon inspectingMelee;
        private Vector3 meleeOriginalLocalPos;
        private Quaternion meleeOriginalLocalRot;

        // 默认按键：H（与武器检视共用）
        private KeyCode meleeInspectKey = KeyCode.H;
        private int lastMeleeInspectKeyFrame = -1;

        // 近战武器检视角度信息结构
        private struct MeleeInspectAngle
        {
            public Vector3 camSpaceOffset;  // 相机空间偏移
            public Vector3 eulerAngles;      // 欧拉角
            public bool isSpecialEffect;    // 是否为特殊效果
            public int specialType;          // 特殊效果类型：0=普通，1=自旋2圈，2=回旋镖
        }

        // 动画参数
        private float meleeTransitionDuration = 0.6f;  // 动作之间的过渡时间
        private float meleeHoldDuration = 0.5f;         // 每个动作停留时长
        private float meleeRestoreDuration = 0.6f;      // 还原到原位


        // 近战武器检视角度组合列表（只保留3个普通角度）
        private readonly MeleeInspectAngle[] meleeInspectAnglePresets = new MeleeInspectAngle[]
        {
            // 角度1：空间坐标(0.24, -0.05, 0.68)，角度(74.5, -90, 0)
            new MeleeInspectAngle 
            { 
                camSpaceOffset = new Vector3(0.35f, -0.05f, 0.58f), 
                eulerAngles = new Vector3(74.5f, -90f, 0f),
                isSpecialEffect = false,
                specialType = 0
            },
            // 角度2：空间坐标(0.35, 0.02, 0.75)，角度(-90, 26.3, -88)
            new MeleeInspectAngle 
            { 
                camSpaceOffset = new Vector3(0.35f, 0.02f, 0.58f), 
                eulerAngles = new Vector3(-90f, 26.3f, 88f),
                isSpecialEffect = false,
                specialType = 0
            },
            // 角度3：空间坐标(0.45, -0.10, 0.58)，角度(65.29, 180, -51.5)
            new MeleeInspectAngle 
            { 
                camSpaceOffset = new Vector3(0.45f, -0.10f, 0.58f), 
                eulerAngles = new Vector3(65.29f, 180f, -51.5f),
                isSpecialEffect = false,
                specialType = 0
            }
        };
        #endregion

        #region 对外接口
        public bool IsInspectingMelee => isInspectingMelee;

        public void StartMeleeInspect()
        {
            if (isInspectingMelee) return;
            if (!isFirstPersonMode) return;
            if (mainCharacter == null) return;
            var melee = mainCharacter.GetMeleeWeapon();
            if (melee == null || melee.transform == null) return;
            if (IsInAdsState()) return;
            
            // 如果正在跑步，检查设置是否允许奔跑时检视
            if (IsRunning() && !IsAllowInspectWhileRunning())
            {
                return;
            }

            if (meleeInspectCoroutine != null)
            {
                try { StopCoroutine(meleeInspectCoroutine); } catch { }
                meleeInspectCoroutine = null;
            }

            isInspectingMelee = true;
            inspectingMelee = melee; // 保存当前检视的武器引用
            meleeInspectCoroutine = StartCoroutine(MeleeInspectCoroutine(melee));
        }

        public void StopMeleeInspect()
        {
            if (!isInspectingMelee) return;

            // 先停止协程
            if (meleeInspectCoroutine != null)
            {
                try { StopCoroutine(meleeInspectCoroutine); } catch { }
                meleeInspectCoroutine = null;
            }

            // 尝试恢复近战武器位置（如果还存在）
            try
            {
                RestoreMeleeFromInspect();
            }
            catch { }
            
            // 清除状态（必须在恢复之后，确保状态被清除）
            isInspectingMelee = false;
            inspectingMelee = null; // 清除武器引用
        }

        /// <summary>
        /// 从选项加载检视按键
        /// </summary>
        private void LoadMeleeInspectKeyFromOptions()
        {
            try
            {
                meleeInspectKey = OptionsHelper.LoadKeyCode(
                    OptionsUIConstants.InspectKeyCodeKey, KeyCode.H);
            }
            catch
            {
                meleeInspectKey = KeyCode.H;
            }
        }
        
        private void UpdateMeleeInspectInput(bool uiBlocking)
        {
            if (uiBlocking) return;

            // 定期加载按键配置（避免每帧都加载）
            if (Time.frameCount % 30 == 0)
            {
                LoadMeleeInspectKeyFromOptions();
            }

            // 同帧去抖
            if (lastMeleeInspectKeyFrame == Time.frameCount) return;

            bool keyPressed = false;
            try
            {
                if (useNewInputSystem)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        keyPressed = GetKeyPressedThisFrame(keyboard, meleeInspectKey);
                    }
                }
                else
                {
                    keyPressed = Input.GetKeyDown(meleeInspectKey);
                }
            }
            catch { }

            if (keyPressed)
            {
                // 仅当手上是近战时响应
                var melee = mainCharacter != null ? mainCharacter.GetMeleeWeapon() : null;
                if (melee != null)
                {
                    lastMeleeInspectKeyFrame = Time.frameCount;
                    StartMeleeInspect();
                }
            }
        }
        #endregion

        #region 主协程
        private IEnumerator MeleeInspectCoroutine(ItemAgent_MeleeWeapon melee)
        {
            if (melee == null || melee.transform == null)
            {
                isInspectingMelee = false;
                yield break;
            }

            var tf = melee.transform;
            var parent = tf.parent;

            // 保存原始局部变换
            meleeOriginalLocalPos = tf.localPosition;
            meleeOriginalLocalRot = tf.localRotation;

            // 相机运动跟踪用于平滑
            Vector3 lastCamPos = mainCamera.transform.position;
            const float jitterThreshold = 0.002f;
            const float smoothLerpSpeed = 30f;
            const float fastLerpSpeed = 60f;

            // 随机选择2-3个角度组合（只从3个普通角度中选择）
            int numAngles = Random.Range(2, 4); // 2或3个
            System.Collections.Generic.List<int> selectedIndices = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> availableIndices = new System.Collections.Generic.List<int> { 0, 1, 2 }; // 3个普通角度

            for (int i = 0; i < numAngles; i++)
            {
                int randomIndex = Random.Range(0, availableIndices.Count);
                int selectedIndex = availableIndices[randomIndex];
                selectedIndices.Add(selectedIndex);
                availableIndices.RemoveAt(randomIndex);
            }

            // 第一阶段：直接移动到第一个角度的位置和旋转
            MeleeInspectAngle firstAngleInfo = meleeInspectAnglePresets[selectedIndices[0]];
            float elapsed = 0f;
            // 记录起始位置（世界坐标），用于插值
            Vector3 startWorldPos = parent != null ? parent.TransformPoint(meleeOriginalLocalPos) : meleeOriginalLocalPos;
            Quaternion startWorldRot = parent != null ? parent.rotation * meleeOriginalLocalRot : meleeOriginalLocalRot;

            while (elapsed < meleeTransitionDuration)
            {
                if (ShouldInterruptMeleeInspect()) { RestoreMeleeFromInspect(); isInspectingMelee = false; inspectingMelee = null; yield break; }
                yield return _waitEndOfFrame;

                Vector3 camPos = mainCamera.transform.position;
                Quaternion camRot = mainCamera.transform.rotation;
                
                // 每帧都基于当前相机位置计算目标位置，确保跟随相机移动
                Vector3 targetWorldPos = camPos + camRot * firstAngleInfo.camSpaceOffset;
                Quaternion targetWorldRot = camRot * Quaternion.Euler(firstAngleInfo.eulerAngles.x, firstAngleInfo.eulerAngles.y, firstAngleInfo.eulerAngles.z);

                float t = elapsed / meleeTransitionDuration;
                t = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic

                // 插值位置和角度
                Vector3 worldPos = Vector3.Lerp(startWorldPos, targetWorldPos, t);
                Quaternion worldRot = Quaternion.Slerp(startWorldRot, targetWorldRot, t);

                if (parent != null)
                {
                    tf.localPosition = parent.InverseTransformPoint(worldPos);
                    tf.localRotation = Quaternion.Inverse(parent.rotation) * worldRot;
                }
                else
                {
                    tf.position = worldPos;
                    tf.rotation = worldRot;
                }

                elapsed += Time.unscaledDeltaTime;
            }

            // 遍历所有选中的角度组合（所有角度都是普通角度）
            for (int angleIndex = 0; angleIndex < selectedIndices.Count; angleIndex++)
            {
                int currentAngleIndex = selectedIndices[angleIndex];
                MeleeInspectAngle currentAngleInfo = meleeInspectAnglePresets[currentAngleIndex];

                // 普通角度：保持当前角度0.5秒
                elapsed = 0f;
                while (elapsed < meleeHoldDuration)
                {
                    if (ShouldInterruptMeleeInspect()) { RestoreMeleeFromInspect(); isInspectingMelee = false; inspectingMelee = null; yield break; }
                    yield return _waitEndOfFrame;

                    Vector3 camPos = mainCamera.transform.position;
                    Quaternion camRot = mainCamera.transform.rotation;
                    Vector3 targetWorldPos = camPos + camRot * currentAngleInfo.camSpaceOffset;
                    Quaternion targetWorldRot = camRot * Quaternion.Euler(currentAngleInfo.eulerAngles.x, currentAngleInfo.eulerAngles.y, currentAngleInfo.eulerAngles.z);

                    Vector3 camMove = camPos - lastCamPos; float camSpeed = camMove.magnitude; lastCamPos = camPos;
                    float lerpSpeed = camSpeed > jitterThreshold ? fastLerpSpeed : smoothLerpSpeed;
                    float t = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);

                    if (parent != null)
                    {
                        Vector3 targetLocalPos = parent.InverseTransformPoint(targetWorldPos);
                        Quaternion targetLocalRot = Quaternion.Inverse(parent.rotation) * targetWorldRot;
                        tf.localPosition = Vector3.Lerp(tf.localPosition, targetLocalPos, t);
                        tf.localRotation = Quaternion.Slerp(tf.localRotation, targetLocalRot, t);
                    }
                    else
                    {
                        tf.position = Vector3.Lerp(tf.position, targetWorldPos, t);
                        tf.rotation = Quaternion.Slerp(tf.rotation, targetWorldRot, t);
                    }

                    elapsed += Time.unscaledDeltaTime;
                }

                // 如果不是最后一个角度，过渡到下一个角度
                if (angleIndex < selectedIndices.Count - 1)
                {
                    MeleeInspectAngle nextAngleInfo = meleeInspectAnglePresets[selectedIndices[angleIndex + 1]];

                    elapsed = 0f;
                    while (elapsed < meleeTransitionDuration)
                    {
                        if (ShouldInterruptMeleeInspect()) { RestoreMeleeFromInspect(); isInspectingMelee = false; inspectingMelee = null; yield break; }
                        yield return _waitEndOfFrame;

                        Vector3 camPos = mainCamera.transform.position;
                        Quaternion camRot = mainCamera.transform.rotation;
                        
                        float t = elapsed / meleeTransitionDuration;
                        t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; // easeInOutCubic
                        
                        // 每帧都基于当前相机位置重新计算起始位置和目标位置，确保跟随相机移动
                        Vector3 startPosWorld = camPos + camRot * currentAngleInfo.camSpaceOffset;
                        Vector3 endPosWorld = camPos + camRot * nextAngleInfo.camSpaceOffset;
                        
                        Quaternion startRotWorld = camRot * Quaternion.Euler(currentAngleInfo.eulerAngles.x, currentAngleInfo.eulerAngles.y, currentAngleInfo.eulerAngles.z);
                        Quaternion endRotWorld = camRot * Quaternion.Euler(nextAngleInfo.eulerAngles.x, nextAngleInfo.eulerAngles.y, nextAngleInfo.eulerAngles.z);
                        
                        // 同步插值位置和角度
                        Vector3 targetWorldPos = Vector3.Lerp(startPosWorld, endPosWorld, t);
                        Quaternion targetWorldRot = Quaternion.Slerp(startRotWorld, endRotWorld, t);

                        // 直接设置插值后的位置和角度，确保同步
                        if (parent != null)
                        {
                            tf.localPosition = parent.InverseTransformPoint(targetWorldPos);
                            tf.localRotation = Quaternion.Inverse(parent.rotation) * targetWorldRot;
                        }
                        else
                        {
                            tf.position = targetWorldPos;
                            tf.rotation = targetWorldRot;
                        }

                        Vector3 camMove = camPos - lastCamPos; lastCamPos = camPos; // 更新lastCamPos用于后续阶段

                        elapsed += Time.unscaledDeltaTime;
                    }
                }
            }

            // 最后阶段：恢复武器到原始位置
            elapsed = 0f;
            Vector3 restoreStartPos = tf.localPosition;
            Quaternion restoreStartRot = tf.localRotation;
            while (elapsed < meleeRestoreDuration)
            {
                if (ShouldInterruptMeleeInspect()) { RestoreMeleeFromInspect(); isInspectingMelee = false; inspectingMelee = null; yield break; }
                yield return _waitEndOfFrame;

                float t = elapsed / meleeRestoreDuration;
                t = t * t * t; // easeInCubic

                if (parent != null)
                {
                    tf.localPosition = Vector3.Lerp(restoreStartPos, meleeOriginalLocalPos, t);
                    tf.localRotation = Quaternion.Slerp(restoreStartRot, meleeOriginalLocalRot, t);
                }
                else
                {
                    Vector3 worldStartPos = tf.position;
                    Quaternion worldStartRot = tf.rotation;
                    tf.position = Vector3.Lerp(worldStartPos, meleeOriginalLocalPos, t);
                    tf.rotation = Quaternion.Slerp(worldStartRot, meleeOriginalLocalRot, t);
                }

                elapsed += Time.unscaledDeltaTime;
            }

            RestoreMeleeFromInspect();
            isInspectingMelee = false;
            meleeInspectCoroutine = null;
            inspectingMelee = null; // 清除武器引用
        }
        #endregion

        #region 工具
        private bool ShouldInterruptMeleeInspect()
        {
            if (mainCharacter == null) return true;
            if (IsInAdsState()) return true; // 瞄准中

            // 鼠标左键按下视为打断
            try
            {
                bool mousePressed = false;
                if (useNewInputSystem) { var m = Mouse.current; if (m != null) mousePressed = m.leftButton.isPressed; }
                else { mousePressed = Input.GetMouseButton(0); }
                if (mousePressed) return true;
            }
            catch { }

            // 切出第一人称
            if (!isFirstPersonMode) return true;

            // 手上近战消失或切换了
            var melee = mainCharacter.GetMeleeWeapon();
            if (melee == null || melee.transform == null) return true;
            
            // 检查武器是否切换了（通过比较武器对象引用）
            if (inspectingMelee != null && inspectingMelee != melee) return true;

            // 如果角色在跑步，检查设置是否允许奔跑时检视
            if (IsRunning())
            {
                // 如果不允许奔跑时检视，打断检视
                if (!IsAllowInspectWhileRunning())
                {
                    return true;
                }
            }

            return false;
        }

        private void RestoreMeleeFromInspect()
        {
            if (mainCharacter == null) return;
            var melee = mainCharacter.GetMeleeWeapon();
            if (melee == null || melee.transform == null) return;
            var tf = melee.transform;
            tf.localPosition = meleeOriginalLocalPos;
            tf.localRotation = meleeOriginalLocalRot;
        }
        #endregion
    }
}
