using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Duckov.Utilities;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁：第一人称模式下重写激光器逻辑，使其从发射器位置出射并指向准星的世界瞄准点。
    /// 保留原版的碰撞判断，真实挡到时会提前命中障碍。
    /// </summary>
    
    /// <summary>
    /// 补丁原版的ShowHitMarker方法，在第一人称模式下禁用（仅对玩家角色）
    /// </summary>
    [HarmonyPatch(typeof(Accessory_Lazer), "ShowHitMarker")]
    internal static class LaserShowHitMarkerPatch
    {
        public static bool Prefix(Accessory_Lazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                // 检查是否是玩家角色的激光器
                var characterField = AccessTools.Field(typeof(Accessory_Lazer), "character");
                var character = characterField?.GetValue(__instance) as CharacterMainControl;
                var mainCharacter = CharacterMainControl.Main;
                
                // 只对玩家角色的激光器禁用原版逻辑
                if (character == mainCharacter)
                {
                    // 第一人称模式下，我们的逻辑会处理红点，禁用原版逻辑
                    return false;
                }
            }
            return true; // 第三人称模式或非玩家角色，允许原版逻辑执行
        }
    }

    /// <summary>
    /// 补丁原版的HideHitMarker方法，在第一人称模式下禁用（仅对玩家角色）
    /// </summary>
    [HarmonyPatch(typeof(Accessory_Lazer), "HideHitMarker")]
    internal static class LaserHideHitMarkerPatch
    {
        public static bool Prefix(Accessory_Lazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                // 检查是否是玩家角色的激光器
                var characterField = AccessTools.Field(typeof(Accessory_Lazer), "character");
                var character = characterField?.GetValue(__instance) as CharacterMainControl;
                var mainCharacter = CharacterMainControl.Main;
                
                // 只对玩家角色的激光器禁用原版逻辑
                if (character == mainCharacter)
                {
                    // 第一人称模式下，我们的逻辑会处理红点，禁用原版逻辑
                    return false;
                }
            }
            return true; // 第三人称模式或非玩家角色，允许原版逻辑执行
        }
    }

    [HarmonyPatch(typeof(Accessory_Lazer))]
    [HarmonyPatch("Update")]
    internal static class LaserPatch
    {
        private static readonly FieldInfo lineRendererField = AccessTools.Field(typeof(Accessory_Lazer), "lineRenderer");
        private static readonly FieldInfo characterField = AccessTools.Field(typeof(Accessory_Lazer), "character");
        private static readonly FieldInfo hitLayersField = AccessTools.Field(typeof(Accessory_Lazer), "hitLayers");
        private static readonly FieldInfo hitMarkerField = AccessTools.Field(typeof(Accessory_Lazer), "hitMarker");
        private static readonly FieldInfo localPointsField = AccessTools.Field(typeof(Accessory_Lazer), "localPoints");

        // 存储每个激光实例的最终点位置，用于LateUpdate中更新红点
        private static readonly System.Collections.Generic.Dictionary<Accessory_Lazer, Vector3> finalPoints = 
            new System.Collections.Generic.Dictionary<Accessory_Lazer, Vector3>();

        // 激光开关状态（默认开启）
        private static bool isLaserEnabled = true;
        
        // 上次检测快捷键的帧数（用于避免重复触发）
        private static int lastToggleFrame = -1;

        /// <summary>
        /// Harmony前缀拦截，仅在第一人称下生效，第三人称走原版逻辑
        /// </summary>
        public static bool Prefix(Accessory_Lazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            
            // 非第一人称模式：恢复原版逻辑，返回true让原方法执行
            if (controller == null || !controller.IsFirstPersonMode)
            {
                ResetLineRendererSpace(__instance);
                return true;
            }

            // 如果正在检视武器，禁用激光
            if (controller.IsInspectingWeapon)
            {
                var lineRendererForInspect = lineRendererField?.GetValue(__instance) as LineRenderer;
                if (lineRendererForInspect != null)
                {
                    lineRendererForInspect.enabled = false;
                }
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false; // 跳过原更新
            }
            
            // 检测激光开关快捷键
            CheckLaserToggleKey();
            
            // 获取LineRenderer组件（激光发射器）
            var lineRenderer = lineRendererField?.GetValue(__instance) as LineRenderer;
            
            // 如果激光被禁用，隐藏激光和红点
            if (!isLaserEnabled)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false; // 跳过原更新
            }
            if (lineRenderer == null)
            {
                return true; // LineRenderer不存在，走原版逻辑
            }

            // 获取角色信息
            var character = characterField?.GetValue(__instance) as CharacterMainControl;
            if (character == null)
            {
                return true; // 角色不存在，走原版逻辑
            }

            // 关键修复：只对玩家角色的激光器应用第一人称逻辑
            // 敌人的激光器应该走原版逻辑，不应该锁定到玩家的准星
            var mainCharacter = CharacterMainControl.Main;
            if (character != mainCharacter)
            {
                // 不是玩家角色，走原版逻辑
                ResetLineRendererSpace(__instance);
                return true;
            }

            // 检查是否在瞄准状态
            bool isAiming = character.IsAiming();
            lineRenderer.enabled = isAiming;

            if (!isAiming)
            {
                HideHitMarker(__instance);
                finalPoints.Remove(__instance); // 清除存储的最终点
                return false; // 跳过原更新
            }

            // 检查摄像机上下角度（pitch）是否在允许范围内（±30度）
            float pitch = 0f;
            try
            {
                var pitchField = AccessTools.Field(typeof(FirstPersonCameraController), "pitch");
                if (pitchField != null && controller != null)
                {
                    pitch = (float)pitchField.GetValue(controller);
                }
            }
            catch { }

            // 如果pitch角度超过±30度，隐藏激光和红点
            const float maxPitchAngle = 15f;
            if (Mathf.Abs(pitch) > maxPitchAngle)
            {
                lineRenderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false; // 跳过原更新
            }

            // 起点：激光发射器（LineRenderer所在节点）的世界坐标
            Vector3 emitterPosition = lineRenderer.transform.position;

            // 获取原组件的hitLayers（需要先获取，用于遮挡检测）
            LayerMask hitLayers;
            if (hitLayersField != null)
            {
                var value = hitLayersField.GetValue(__instance);
                hitLayers = value is LayerMask mask ? mask : (LayerMask)(int)value;
            }
            else
            {
                hitLayers = Physics.DefaultRaycastLayers;
            }

            // 检测发射器是否被遮挡（从发射器向相机方向做短距离检测）
            bool isEmitterObstructed = CheckEmitterObstruction(emitterPosition, character, hitLayers);
            
            // 如果发射器被遮挡，隐藏激光和红点
            if (isEmitterObstructed)
            {
                lineRenderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false; // 跳过原更新
            }

            // 终点：获取当前基于相机前向与我们自定义规则计算出的"世界瞄准点"（与准星一致）
            Vector3 aimPoint = controller.GetCurrentAimWorldPoint();

            // 抗抖逻辑（非插值）：对瞄准点做轻量位置量化（0.01m），抑制微小噪声，不引入时间延迟
            const float quantizeStep = 0.01f;
            aimPoint = new Vector3(
                Mathf.Round(aimPoint.x / quantizeStep) * quantizeStep,
                Mathf.Round(aimPoint.y / quantizeStep) * quantizeStep,
                Mathf.Round(aimPoint.z / quantizeStep) * quantizeStep
            );

            // 计算从发射器到量化后的瞄准点的方向
            Vector3 direction = (aimPoint - emitterPosition);
            float distance = direction.magnitude;

            // 处理距离过小的情况
            if (distance < 0.01f)
            {
                // 如果瞄准点与发射器位置几乎重合，使用发射器前向方向
                direction = lineRenderer.transform.forward;
                distance = Mathf.Max(character.GetAimRange(), 50f);
            }
            else
            {
                direction = direction.normalized;
            }

            // 从发射器朝向瞄准点方向做Physics.Raycast，使用原组件的hitLayers
            // 若中途被挡则终点改为命中点，实现"正常受阻"
            Vector3 finalPoint = aimPoint; // 默认终点就是量化后的准星瞄准点
            if (Physics.Raycast(emitterPosition, direction, out RaycastHit hit, distance, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // 中途被阻挡，终点改为命中点
                // 对命中点做轻量位置量化（0.01m），抑制微小噪声，不引入时间延迟
                finalPoint = new Vector3(
                    Mathf.Round(hit.point.x / quantizeStep) * quantizeStep,
                    Mathf.Round(hit.point.y / quantizeStep) * quantizeStep,
                    Mathf.Round(hit.point.z / quantizeStep) * quantizeStep
                );
            }

            // 检查玩家是否在沙袋附近，如果在附近则隐藏红点
            bool hasNearByHalfObsticle = false;
            try
            {
                if (character != null)
                {
                    hasNearByHalfObsticle = character.HasNearByHalfObsticle();
                }
            }
            catch { }

            if (hasNearByHalfObsticle)
            {
                // 玩家在沙袋附近，隐藏红点
                HideHitMarker(__instance);
            }
            else
            {
                // 玩家不在沙袋附近，显示红点
                ShowHitMarker(__instance, finalPoint);
            }

            // 绘制：LineRenderer改为useWorldSpace = true，设置两端点为起点/终点
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.SetPosition(0, emitterPosition);
            lineRenderer.SetPosition(1, finalPoint);

            // 存储最终点位置，用于LateUpdate中更新红点
            finalPoints[__instance] = finalPoint;

            // 清空原版的localPoints，防止原版逻辑干扰
            // 原版逻辑使用localPoints和useWorldSpace=false，我们必须确保这些不会影响我们的渲染
            if (localPointsField != null)
            {
                localPointsField.SetValue(__instance, null);
            }

            // 强制确保useWorldSpace为true，防止被其他代码修改
            lineRenderer.useWorldSpace = true;

            // 返回false跳过原更新，完全禁用原版逻辑
            return false;
        }

        /// <summary>
        /// 显示命中标记器
        /// </summary>
        private static void ShowHitMarker(Accessory_Lazer instance, Vector3 point)
        {
            if (hitMarkerField?.GetValue(instance) is GameObject marker)
            {
                if (!marker.activeSelf)
                {
                    marker.SetActive(true);
                }
                // 直接设置世界空间位置
                marker.transform.position = point;
            }
        }

        /// <summary>
        /// 在LateUpdate中强制更新所有激光的红点位置
        /// 确保红点位置在所有其他更新之后被设置，防止被其他代码覆盖
        /// </summary>
        public static void LateUpdateHitMarkers()
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode)
            {
                return;
            }

            // 遍历所有存储的最终点，强制更新红点位置（只处理玩家角色的激光器）
            var mainCharacter = CharacterMainControl.Main;
            var keys = new System.Collections.Generic.List<Accessory_Lazer>(finalPoints.Keys);
            foreach (var instance in keys)
            {
                if (instance == null) continue;

                // 检查是否是玩家角色的激光器
                var character = characterField?.GetValue(instance) as CharacterMainControl;
                if (character != mainCharacter)
                {
                    // 不是玩家角色，从字典中移除，不再处理
                    finalPoints.Remove(instance);
                    continue;
                }

                if (finalPoints.TryGetValue(instance, out Vector3 finalPoint))
                {
                    if (hitMarkerField?.GetValue(instance) is GameObject marker)
                    {
                        bool hasNearByHalfObsticle = false;
                        
                        try
                        {
                            if (character != null)
                            {
                                hasNearByHalfObsticle = character.HasNearByHalfObsticle();
                            }
                        }
                        catch { }

                        if (hasNearByHalfObsticle)
                        {
                            // 玩家在沙袋附近，隐藏红点
                            if (marker.activeSelf)
                            {
                                marker.SetActive(false);
                            }
                        }
                        else
                        {
                            // 玩家不在沙袋附近，显示并更新红点位置
                            if (!marker.activeSelf)
                            {
                                marker.SetActive(true);
                            }
                            // 强制更新红点位置到最终点
                            marker.transform.position = finalPoint;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 隐藏命中标记器
        /// </summary>
        private static void HideHitMarker(Accessory_Lazer instance)
        {
            if (hitMarkerField?.GetValue(instance) is GameObject marker && marker.activeSelf)
            {
                marker.SetActive(false);
            }
        }

        /// <summary>
        /// 检测发射器是否被遮挡
        /// 从发射器位置向相机方向（或发射器前向）做短距离射线检测
        /// </summary>
        private static bool CheckEmitterObstruction(Vector3 emitterPosition, CharacterMainControl character, LayerMask hitLayers)
        {
            try
            {
                // 获取相机位置和方向
                var controller = FirstPersonCameraController.Instance;
                Vector3 cameraPosition = Vector3.zero;
                Vector3 cameraForward = Vector3.forward;
                
                if (controller != null)
                {
                    // 通过反射获取mainCamera
                    var mainCameraField = AccessTools.Field(typeof(FirstPersonCameraController), "mainCamera");
                    if (mainCameraField != null)
                    {
                        var cam = mainCameraField.GetValue(controller) as Camera;
                        if (cam != null)
                        {
                            cameraPosition = cam.transform.position;
                            cameraForward = cam.transform.forward;
                        }
                    }
                }

                // 如果无法获取相机，使用发射器前向方向
                if (cameraPosition == Vector3.zero)
                {
                    var cam = GameCamera.Instance != null ? GameCamera.Instance.renderCamera : null;
                    if (cam != null)
                    {
                        cameraPosition = cam.transform.position;
                        cameraForward = cam.transform.forward;
                    }
                }

                // 计算从发射器到相机的方向
                Vector3 toCamera = cameraPosition - emitterPosition;
                float distanceToCamera = toCamera.magnitude;
                
                // 如果距离太近，使用发射器前向方向
                Vector3 checkDirection;
                float checkDistance;
                
                if (distanceToCamera < 0.1f)
                {
                    // 无法确定相机位置，使用发射器前向
                    // 这里我们需要获取LineRenderer的transform
                    return false; // 无法检测，假设不被遮挡
                }
                else
                {
                    checkDirection = toCamera.normalized;
                    // 检测距离：发射器到相机的距离，但限制在合理范围内（0.5米到2米）
                    checkDistance = Mathf.Clamp(distanceToCamera, 0.5f, 2.0f);
                }

                // 执行射线检测，检查发射器是否被遮挡
                // 使用一个小的偏移量，避免检测到发射器本身
                const float offsetDistance = 0.05f;
                Vector3 rayStart = emitterPosition + checkDirection * offsetDistance;
                float rayDistance = checkDistance - offsetDistance;

                if (rayDistance > 0.01f)
                {
                    // 检查是否有附近的半遮挡物（如沙袋）
                    bool hasNearByHalfObsticle = false;
                    LayerMask obstructionLayers = hitLayers;
                    
                    try
                    {
                        if (character != null)
                        {
                            hasNearByHalfObsticle = character.HasNearByHalfObsticle();
                            
                            // 如果有附近的半遮挡物，需要从射线检测中排除HalfObsticle层
                            if (hasNearByHalfObsticle)
                            {
                                int halfObsticleLayer = LayerMask.NameToLayer("HalfObsticle");
                                if (halfObsticleLayer >= 0)
                                {
                                    // 排除HalfObsticle层，使射线可以穿透半遮挡物
                                    obstructionLayers &= (~(1 << halfObsticleLayer));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 半遮挡物检查失败时使用默认层掩码
                    }

                    // 执行射线检测
                    if (Physics.Raycast(rayStart, checkDirection, out RaycastHit hit, rayDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
                    {
                        // 检查是否命中角色自身（不应该因为角色自身而隐藏激光）
                        if (character != null && character.mainDamageReceiver != null)
                        {
                            var damageReceiver = hit.collider.GetComponent<DamageReceiver>();
                            if (damageReceiver != null && damageReceiver.health != null)
                            {
                                var hitCharacter = damageReceiver.health.TryGetCharacter();
                                if (hitCharacter == character)
                                {
                                    // 命中的是角色自身，不算遮挡
                                    return false;
                                }
                            }
                        }

                        // 如果有附近的半遮挡物，检查是否命中的是半遮挡物
                        if (hasNearByHalfObsticle)
                        {
                            try
                            {
                                // 检查是否命中HalfObsticle层
                                if (GameplayDataSettings.LayersData.IsLayerInLayerMask(hit.collider.gameObject.layer, GameplayDataSettings.Layers.halfObsticleLayer))
                                {
                                    // 命中的是半遮挡物，不算遮挡
                                    return false;
                                }
                                
                                // 检查DamageReceiver是否是半遮挡物
                                var damageReceiver = hit.collider.GetComponent<DamageReceiver>();
                                if (damageReceiver != null && damageReceiver.isHalfObsticle)
                                {
                                    // 命中的是半遮挡物，不算遮挡
                                    return false;
                                }
                            }
                            catch
                            {
                                // 检查失败，继续判断为遮挡
                            }
                        }
                        
                        // 检测到遮挡
                        return true;
                    }
                }
            }
            catch
            {
                // 检测失败，假设不被遮挡
            }

            return false; // 没有被遮挡
        }

        /// <summary>
        /// 检测激光开关快捷键
        /// </summary>
        private static void CheckLaserToggleKey()
        {
            try
            {
                // 避免同一帧重复检测
                if (lastToggleFrame == Time.frameCount)
                {
                    return;
                }

                // 加载快捷键配置
                KeyCode toggleKey = OptionsHelper.LoadKeyCode(OptionsUIConstants.LaserToggleKeyCodeKey, KeyCode.None);
                
                // 如果快捷键未设置（KeyCode.None），则不检测
                if (toggleKey == KeyCode.None)
                {
                    return;
                }

                // 检测按键按下
                if (Input.GetKeyDown(toggleKey))
                {
                    isLaserEnabled = !isLaserEnabled;
                    lastToggleFrame = Time.frameCount;
                }
            }
            catch
            {
                // 检测失败时静默处理
            }
        }

        /// <summary>
        /// 重置LineRenderer为本地空间（退出第一人称时恢复原版行为）
        /// </summary>
        private static void ResetLineRendererSpace(Accessory_Lazer instance)
        {
            if (lineRendererField?.GetValue(instance) is LineRenderer renderer)
            {
                renderer.useWorldSpace = false;
            }
            // 恢复原版的localPoints（如果需要的话，让原版逻辑可以正常工作）
            // 注意：这里不清空，让原版逻辑自己初始化
        }
        
        /// <summary>
        /// 获取激光是否启用（公共方法，供外部调用）
        /// </summary>
        public static bool GetLaserEnabled()
        {
            return isLaserEnabled;
        }
        
        /// <summary>
        /// 设置激光是否启用（公共方法，供外部调用）
        /// </summary>
        public static void SetLaserEnabled(bool enabled)
        {
            isLaserEnabled = enabled;
        }
    }
}
