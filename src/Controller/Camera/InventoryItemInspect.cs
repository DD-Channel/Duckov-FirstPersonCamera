using System.Collections;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称 - 背包中物品的临时检视（不更换装备，仅隐藏当前持有物并生成一个临时Agent用于检视）。
    /// </summary>
    public partial class FirstPersonCameraController
    {
        // 预览检视状态
        private bool isPreviewInspecting = false;
        private GameObject hiddenHoldAgentGO; // 被暂时隐藏的当前持有物
        private Item previewItem;
        private ItemAgent previewAgent; // 临时生成的Agent（枪/近战）
        private Coroutine previewInspectCoroutine;
        // UI检视状态
        private bool isUIInspecting = false;
        private Coroutine uiInspectCoroutine;

        /// <summary>
        /// 从背包中对指定物品进行检视（不装备）。支持枪械与近战。
        /// </summary>
        public void StartInventoryItemInspect(Item item)
        {
            if (item == null) return;
            if (!isFirstPersonMode) return;
            if (mainCharacter == null || mainCamera == null) return;
            if (IsInAdsState()) return;
            if (isPreviewInspecting || isInspectingWeapon || isInspectingMelee) return;

            // 类型判断
            bool isGun = item.GetComponent<ItemSetting_Gun>() != null;
            bool isMelee = item.GetComponent<ItemSetting_MeleeWeapon>() != null;
            if (!isGun && !isMelee) return;

            // 隐藏当前持有物
            hiddenHoldAgentGO = mainCharacter.CurrentHoldItemAgent ? mainCharacter.CurrentHoldItemAgent.gameObject : null;
            if (hiddenHoldAgentGO != null)
            {
                hiddenHoldAgentGO.SetActive(false);
            }

            // 生成临时Agent（handheld）
            previewItem = item;
            previewAgent = item.CreateHandheldAgent();
            if (previewAgent == null)
            {
                // 失败则恢复
                if (hiddenHoldAgentGO != null) hiddenHoldAgentGO.SetActive(true);
                previewItem = null;
                return;
            }

            // 加载3D模型检视距离
            float inspectDistance = OptionsUIConstants.ModelInspectDistanceDefault;
            try
            {
                inspectDistance = FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(
                    OptionsUIConstants.ModelInspectDistanceKey, 
                    OptionsUIConstants.ModelInspectDistanceDefault);
                inspectDistance = Mathf.Clamp(inspectDistance, 
                    OptionsUIConstants.ModelInspectDistanceMin, 
                    OptionsUIConstants.ModelInspectDistanceMax);
            }
            catch { }

            // 将临时Agent放到屏幕外一个初始稳定位置，后续使用世界空间插值到相机前
            var tf = previewAgent.transform;
            tf.position = mainCamera.transform.position + mainCamera.transform.forward * inspectDistance;
            tf.rotation = mainCamera.transform.rotation;

            // 开启协程（根据类型走不同角度组，但共用同一相机跟随与打断规则）
            isPreviewInspecting = true;
            previewInspectCoroutine = StartCoroutine(PreviewInspectCoroutine(tf, isGun, inspectDistance));
        }

        /// <summary>
        /// 通用入口：根据类型分发到 预览模型检视 / UI检视。
        /// </summary>
        public void StartInspectForAnyItem(Item item)
        {
            if (item == null) return;
            if (!isFirstPersonMode) return;
            if (IsInAdsState()) return;

            // 武器/近战 → 直接用已有预览逻辑
            if (item.GetComponent<ItemSetting_Gun>() != null || item.GetComponent<ItemSetting_MeleeWeapon>() != null)
            {
                StartInventoryItemInspect(item);
                return;
            }

            // 尝试以handheld方式生成Agent（有建模的物品）
            if (TryStartModeledItemInspect(item))
            {
                return;
            }

            // 否则走UI图标检视（可被打断）
            StartUIItemInspect(item);
        }

        private bool TryStartModeledItemInspect(Item item)
        {
            // 隐藏当前持有物
            hiddenHoldAgentGO = mainCharacter != null && mainCharacter.CurrentHoldItemAgent ? mainCharacter.CurrentHoldItemAgent.gameObject : null;
            if (hiddenHoldAgentGO != null) hiddenHoldAgentGO.SetActive(false);

            previewItem = item;
            previewAgent = item.CreateHandheldAgent();
            if (previewAgent == null)
            {
                if (hiddenHoldAgentGO != null) hiddenHoldAgentGO.SetActive(true);
                previewItem = null;
                return false;
            }

            // 检查生成的Agent是否有渲染器（有3D模型）
            var renderers = previewAgent.GetComponentsInChildren<Renderer>(true);
            bool has3DModel = renderers != null && renderers.Length > 0;
            
            if (!has3DModel)
            {
                // 没有3D模型，释放Agent
                try
                {
                    if (previewItem != null && previewItem.AgentUtilities != null)
                    {
                        previewItem.AgentUtilities.ReleaseActiveAgent();
                    }
                }
                catch { }
                previewAgent = null;
                previewItem = null;
                if (hiddenHoldAgentGO != null) hiddenHoldAgentGO.SetActive(true);
                return false;
            }

            // 加载3D模型检视距离
            float inspectDistance = OptionsUIConstants.ModelInspectDistanceDefault;
            try
            {
                inspectDistance = FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(
                    OptionsUIConstants.ModelInspectDistanceKey, 
                    OptionsUIConstants.ModelInspectDistanceDefault);
                inspectDistance = Mathf.Clamp(inspectDistance, 
                    OptionsUIConstants.ModelInspectDistanceMin, 
                    OptionsUIConstants.ModelInspectDistanceMax);
            }
            catch { }

            var tf = previewAgent.transform;
            // 使用配置的距离
            tf.position = mainCamera.transform.position + mainCamera.transform.forward * inspectDistance;
            tf.rotation = mainCamera.transform.rotation;

            isPreviewInspecting = true;
            previewInspectCoroutine = StartCoroutine(PreviewInspectCoroutine(tf, asGun: true, inspectDistance));
            return true;
        }

        public void StartUIItemInspect(Item item)
        {
            if (item == null) return;
            if (isUIInspecting) return;
            var icon = item.Icon;
            if (icon == null) return;

            // 加载UI检视配置
            float duration = OptionsUIConstants.UIInspectDurationDefault;
            float size = OptionsUIConstants.UIInspectSizeDefault;
            try
            {
                duration = FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(
                    OptionsUIConstants.UIInspectDurationKey, 
                    OptionsUIConstants.UIInspectDurationDefault);
                duration = Mathf.Clamp(duration, 
                    OptionsUIConstants.UIInspectDurationMin, 
                    OptionsUIConstants.UIInspectDurationMax);
                
                size = FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(
                    OptionsUIConstants.UIInspectSizeKey, 
                    OptionsUIConstants.UIInspectSizeDefault);
                size = Mathf.Clamp(size, 
                    OptionsUIConstants.UIInspectSizeMin, 
                    OptionsUIConstants.UIInspectSizeMax);
            }
            catch { }

            isUIInspecting = true;
            uiInspectCoroutine = StartCoroutine(UIInspectCoroutine(icon, duration, size));
        }

        private IEnumerator UIInspectCoroutine(Sprite sprite, float seconds, float sizeMultiplier)
        {
            ItemInspectUI.Show(sprite, seconds, sizeMultiplier);
            
            // 等待直到被打断或时间到期（ItemInspectUI内部会处理打断逻辑）
            while (ItemInspectUI.IsVisible())
            {
                if (ShouldInterruptPreview())
                {
                    ItemInspectUI.Hide();
                    break;
                }
                yield return _waitEndOfFrame;
            }
            
            isUIInspecting = false;
            uiInspectCoroutine = null;
        }

        /// <summary>
        /// 预览检视 - 主协程。模仿武器/近战检视的时序与相机跟随，但不依赖手上物。
        /// </summary>
        private IEnumerator PreviewInspectCoroutine(Transform targetTf, bool asGun, float inspectDistance = 0.6f)
        {
            if (targetTf == null)
            {
                StopInventoryItemInspect();
                yield break;
            }

            // 相机运动跟踪参数（与武器/近战一致的风格）
            Vector3 lastCamPos = mainCamera.transform.position;
            const float jitterThreshold = 0.002f;
            const float smoothLerpSpeed = 30f;
            const float fastLerpSpeed = 60f;

            // 记录初始局部（相对自身，无父级时无意义，但用于恢复动画）
            Vector3 startLocalPos = targetTf.localPosition;
            Quaternion startLocalRot = targetTf.localRotation;
            
            // 使用配置的检视距离
            Vector3 camSpaceOffset = new Vector3(0f, 0f, inspectDistance);

            // 选择角度集合（复用现有预设）
            if (asGun)
            {
                // 使用武器检视参数
                // 第一阶段：过渡到第一视角
                Vector3 firstAngles = inspectAnglePresets[Random.Range(0, inspectAnglePresets.Length)];
                float elapsed = 0f;
                while (elapsed < inspectTransitionDuration)
                {
                    if (ShouldInterruptPreview()) { StopInventoryItemInspect(); yield break; }
                    yield return _waitEndOfFrame;

                    Vector3 camPos = mainCamera.transform.position;
                    Quaternion camRot = mainCamera.transform.rotation;
                    Vector3 targetWorldPos = camPos + camRot * camSpaceOffset;
                    Quaternion targetWorldRot = camRot * Quaternion.Euler(firstAngles);
                    float t = elapsed / inspectTransitionDuration; t = 1f - Mathf.Pow(1f - t, 3f);
                    targetTf.position = Vector3.Lerp(targetTf.position, targetWorldPos, t);
                    targetTf.rotation = Quaternion.Slerp(targetTf.rotation, targetWorldRot, t);
                    elapsed += Time.unscaledDeltaTime;
                }

                // 随机挑选2-3个角度轮换
                int numAngles = Random.Range(2, 4);
                System.Collections.Generic.List<int> indices = new System.Collections.Generic.List<int>();
                System.Collections.Generic.List<int> avail = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4 };
                for (int i = 0; i < numAngles; i++) { int r = Random.Range(0, avail.Count); indices.Add(avail[r]); avail.RemoveAt(r); }

                for (int k = 0; k < indices.Count; k++)
                {
                    float hold = 0f;
                    while (hold < inspectHoldDuration)
                    {
                        if (ShouldInterruptPreview()) { StopInventoryItemInspect(); yield break; }
                        yield return _waitEndOfFrame;
                        Vector3 camPos = mainCamera.transform.position; Quaternion camRot = mainCamera.transform.rotation;
                        Vector3 camMove = camPos - lastCamPos; float camSpeed = camMove.magnitude; lastCamPos = camPos;
                        float lerp = 1f - Mathf.Exp(-(camSpeed > jitterThreshold ? fastLerpSpeed : smoothLerpSpeed) * Time.unscaledDeltaTime);
                        Vector3 worldPos = camPos + camRot * camSpaceOffset;
                        Quaternion worldRot = camRot * Quaternion.Euler(inspectAnglePresets[indices[k]]);
                        targetTf.position = Vector3.Lerp(targetTf.position, worldPos, lerp);
                        targetTf.rotation = Quaternion.Slerp(targetTf.rotation, worldRot, lerp);
                        hold += Time.unscaledDeltaTime;
                    }

                    if (k < indices.Count - 1)
                    {
                        float tran = 0f;
                        Vector3 nextAngles = inspectAnglePresets[indices[k + 1]];
                        while (tran < inspectTransitionDuration)
                        {
                            if (ShouldInterruptPreview()) { StopInventoryItemInspect(); yield break; }
                            yield return _waitEndOfFrame;
                            Vector3 camPos = mainCamera.transform.position; Quaternion camRot = mainCamera.transform.rotation;
                            float t = tran / inspectTransitionDuration; t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                            Vector3 worldPos = camPos + camRot * camSpaceOffset;
                            Quaternion worldRot = camRot * Quaternion.Euler(nextAngles);
                            targetTf.position = Vector3.Lerp(targetTf.position, worldPos, t);
                            targetTf.rotation = Quaternion.Slerp(targetTf.rotation, worldRot, t);
                            tran += Time.unscaledDeltaTime;
                        }
                    }
                }
            }
            else
            {
                // 使用近战检视参数（取第一个普通角度作为起始）
                var firstAngleInfo = meleeInspectAnglePresets[0];
                // 调整偏移距离为配置的距离
                Vector3 adjustedOffset = firstAngleInfo.camSpaceOffset;
                adjustedOffset.z = inspectDistance;
                
                float elapsed = 0f;
                while (elapsed < meleeTransitionDuration)
                {
                    if (ShouldInterruptPreview()) { StopInventoryItemInspect(); yield break; }
                    yield return _waitEndOfFrame;
                    Vector3 camPos = mainCamera.transform.position; Quaternion camRot = mainCamera.transform.rotation;
                    Vector3 worldPos = camPos + camRot * adjustedOffset;
                    Quaternion worldRot = camRot * Quaternion.Euler(firstAngleInfo.eulerAngles);
                    float t = elapsed / meleeTransitionDuration; t = 1f - Mathf.Pow(1f - t, 3f);
                    targetTf.position = Vector3.Lerp(targetTf.position, worldPos, t);
                    targetTf.rotation = Quaternion.Slerp(targetTf.rotation, worldRot, t);
                    elapsed += Time.unscaledDeltaTime;
                }

                // 保持+轮换几个角度
                int numAngles = 2;
                for (int i = 0; i < numAngles; i++)
                {
                    float hold = 0f;
                    var info = meleeInspectAnglePresets[Mathf.Min(i, meleeInspectAnglePresets.Length - 1)];
                    // 调整偏移距离
                    Vector3 adjustedInfoOffset = info.camSpaceOffset;
                    adjustedInfoOffset.z = inspectDistance;
                    
                    while (hold < meleeHoldDuration)
                    {
                        if (ShouldInterruptPreview()) { StopInventoryItemInspect(); yield break; }
                        yield return _waitEndOfFrame;
                        Vector3 camPos = mainCamera.transform.position; Quaternion camRot = mainCamera.transform.rotation;
                        Vector3 camMove = camPos - lastCamPos; float camSpeed = camMove.magnitude; lastCamPos = camPos;
                        float lerp = 1f - Mathf.Exp(-(camSpeed > jitterThreshold ? fastLerpSpeed : smoothLerpSpeed) * Time.unscaledDeltaTime);
                        Vector3 worldPos = camPos + camRot * adjustedInfoOffset;
                        Quaternion worldRot = camRot * Quaternion.Euler(info.eulerAngles);
                        targetTf.position = Vector3.Lerp(targetTf.position, worldPos, lerp);
                        targetTf.rotation = Quaternion.Slerp(targetTf.rotation, worldRot, lerp);
                        hold += Time.unscaledDeltaTime;
                    }
                }
            }

            // 恢复阶段（简单淡回初始相对状态）
            float restore = 0f;
            while (restore < (asGun ? inspectRestoreDuration : meleeRestoreDuration))
            {
                if (ShouldInterruptPreview()) { StopInventoryItemInspect(); yield break; }
                yield return _waitEndOfFrame;
                float t = restore / (asGun ? inspectRestoreDuration : meleeRestoreDuration); t = t * t * t;
                targetTf.localPosition = Vector3.Lerp(targetTf.localPosition, startLocalPos, t);
                targetTf.localRotation = Quaternion.Slerp(targetTf.localRotation, startLocalRot, t);
                restore += Time.unscaledDeltaTime;
            }

            StopInventoryItemInspect();
        }

        /// <summary>
        /// 预览检视打断规则：与武器/近战检视一致（ADS/左键/跑步/退出第一人称等）。
        /// </summary>
        private bool ShouldInterruptPreview()
        {
            if (!isFirstPersonMode) return true;
            if (IsInAdsState()) return true;

            try
            {
                bool mousePressed = false;
                if (useNewInputSystem) { var m = Mouse.current; if (m != null) mousePressed = m.leftButton.isPressed; }
                else { mousePressed = Input.GetMouseButton(0); }
                if (mousePressed) return true;
            }
            catch { }

            try
            {
                bool running = false;
                if (useNewInputSystem) { var kb = Keyboard.current; if (kb != null) running = kb.leftShiftKey.isPressed; }
                else { running = Input.GetKey(KeyCode.LeftShift); }
                if (running) return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 结束并清理预览检视，恢复之前隐藏的持有物。
        /// </summary>
        public void StopInventoryItemInspect()
        {
            if (!isPreviewInspecting) return;
            if (previewInspectCoroutine != null)
            {
                try { StopCoroutine(previewInspectCoroutine); } catch { }
                previewInspectCoroutine = null;
            }

            // 释放预览Agent
            try
            {
                if (previewItem != null && previewItem.AgentUtilities != null)
                {
                    previewItem.AgentUtilities.ReleaseActiveAgent();
                }
            }
            catch { }

            // 恢复原持有物
            if (hiddenHoldAgentGO != null)
            {
                try { hiddenHoldAgentGO.SetActive(true); } catch { }
                hiddenHoldAgentGO = null;
            }

            previewItem = null;
            previewAgent = null;
            isPreviewInspecting = false;
        }

        public void StopUIItemInspect()
        {
            if (!isUIInspecting) return;
            if (uiInspectCoroutine != null)
            {
                try { StopCoroutine(uiInspectCoroutine); } catch { }
                uiInspectCoroutine = null;
            }
            ItemInspectUI.Hide();
            isUIInspecting = false;
        }
    }
}
