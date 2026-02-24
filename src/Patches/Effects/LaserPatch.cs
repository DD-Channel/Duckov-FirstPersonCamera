using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Duckov.Utilities;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;
using System.Collections.Generic;

namespace FirstPersonCamera
{
    /// <summary>
    /// 激光器补丁核心逻辑（共享）
    /// </summary>
    internal static class LaserPatchCore
    {
        // 字段缓存（Accessory_Lazer）
        private static readonly FieldInfo lineRendererField = AccessTools.Field(typeof(Accessory_Lazer), "lineRenderer");
        private static readonly FieldInfo hitLayersField = AccessTools.Field(typeof(Accessory_Lazer), "hitLayers");
        private static readonly FieldInfo hitMarkerField = AccessTools.Field(typeof(Accessory_Lazer), "hitMarker");
        private static readonly FieldInfo localPointsField = AccessTools.Field(typeof(Accessory_Lazer), "localPoints");

        // TecLazer 相关
        private static Type tecLazerType;
        private static FieldInfo tecLineRendererField;
        private static FieldInfo tecHitLayersField;
        private static FieldInfo tecHitMarkerField;
        private static FieldInfo tecLocalPointsField;
        private static FieldInfo tecCharacterField;

        // 忽略极近命中的最小距离（米），用于枪口嵌入情况
        private const float MIN_HIT_DISTANCE = 0.1f;

        // 当激光线长度小于此值时隐藏激光（米）- 调整为 1.2 米以解决近距离问题
        private const float LASER_LENGTH_HIDE_THRESHOLD = 1.1f;

        static LaserPatchCore()
        {
            tecLazerType = AccessTools.TypeByName("TecLazer");
            if (tecLazerType != null)
            {
                // 尝试匹配常用字段名
                foreach (var field in tecLazerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.Name.Equals("lineRenderer") || field.Name.Equals("m_lineRenderer"))
                        tecLineRendererField = field;
                    else if (field.Name.Equals("hitLayers") || field.Name.Equals("m_hitLayers"))
                        tecHitLayersField = field;
                    else if (field.Name.Equals("hitMarker") || field.Name.Equals("m_hitMarker"))
                        tecHitMarkerField = field;
                    else if (field.Name.Equals("localPoints") || field.Name.Equals("m_localPoints"))
                        tecLocalPointsField = field;
                    else if (field.Name.Equals("character") || field.Name.Equals("m_character"))
                        tecCharacterField = field;
                }
                // 保底：使用默认字段名（与 Accessory_Lazer 相同）
                if (tecLineRendererField == null) tecLineRendererField = AccessTools.Field(tecLazerType, "lineRenderer");
                if (tecHitLayersField == null) tecHitLayersField = AccessTools.Field(tecLazerType, "hitLayers");
                if (tecHitMarkerField == null) tecHitMarkerField = AccessTools.Field(tecLazerType, "hitMarker");
                if (tecLocalPointsField == null) tecLocalPointsField = AccessTools.Field(tecLazerType, "localPoints");
                if (tecCharacterField == null) tecCharacterField = AccessTools.Field(tecLazerType, "character");
            }
        }

        // 存储每个激光实例的最终点位置，用于LateUpdate中更新红点
        public static readonly Dictionary<Component, Vector3> finalPoints = new Dictionary<Component, Vector3>();

        // 激光开关状态
        private static bool isLaserEnabled = true;
        private static int lastToggleFrame = -1;

        /// <summary>
        /// 从激光器实例获取所属角色（通用方法）
        /// </summary>
        public static CharacterMainControl GetCharacterFromLaser(Component laser)
        {
            if (laser == null) return null;

            var parentChar = laser.GetComponentInParent<CharacterMainControl>();
            if (parentChar != null)
                return parentChar;

            if (laser is Accessory_Lazer)
            {
                var charField = AccessTools.Field(typeof(Accessory_Lazer), "character");
                if (charField != null)
                {
                    var charFromField = charField.GetValue(laser) as CharacterMainControl;
                    if (charFromField != null)
                        return charFromField;
                }
            }
            else if (tecLazerType != null && tecLazerType.IsInstanceOfType(laser))
            {
                if (tecCharacterField != null)
                {
                    var charFromField = tecCharacterField.GetValue(laser) as CharacterMainControl;
                    if (charFromField != null)
                        return charFromField;
                }
            }

            try
            {
                Type gunType = AccessTools.TypeByName("ItemAgent_Gun") ??
                               AccessTools.TypeByName("ItemAgentGun") ??
                               AccessTools.TypeByName("GunItemAgent");
                if (gunType != null)
                {
                    var gun = laser.GetComponentInParent(gunType) as Component;
                    if (gun != null)
                    {
                        string[] possibleFieldNames = { "carrier", "owner", "character", "m_carrier", "m_owner", "m_character", "holder" };
                        foreach (string fieldName in possibleFieldNames)
                        {
                            var field = AccessTools.Field(gunType, fieldName);
                            if (field != null)
                            {
                                var carrier = field.GetValue(gun) as CharacterMainControl;
                                if (carrier != null)
                                    return carrier;
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 显示命中标记器
        /// </summary>
        public static void ShowHitMarker(Component instance, Vector3 point)
        {
            GameObject marker = null;
            if (instance is Accessory_Lazer)
                marker = hitMarkerField?.GetValue(instance) as GameObject;
            else if (tecLazerType != null && tecLazerType.IsInstanceOfType(instance))
                marker = tecHitMarkerField?.GetValue(instance) as GameObject;

            if (marker != null)
            {
                if (!marker.activeSelf)
                    marker.SetActive(true);
                marker.transform.position = point;
            }
        }

        /// <summary>
        /// 隐藏命中标记器
        /// </summary>
        public static void HideHitMarker(Component instance)
        {
            GameObject marker = null;
            if (instance is Accessory_Lazer)
                marker = hitMarkerField?.GetValue(instance) as GameObject;
            else if (tecLazerType != null && tecLazerType.IsInstanceOfType(instance))
                marker = tecHitMarkerField?.GetValue(instance) as GameObject;

            if (marker != null && marker.activeSelf)
                marker.SetActive(false);
        }

        /// <summary>
        /// 获取 LineRenderer（辅助方法）
        /// </summary>
        private static LineRenderer GetLineRenderer(Component instance)
        {
            if (instance is Accessory_Lazer)
                return lineRendererField?.GetValue(instance) as LineRenderer;
            else if (tecLazerType != null && tecLazerType.IsInstanceOfType(instance))
                return tecLineRendererField?.GetValue(instance) as LineRenderer;
            return null;
        }

        /// <summary>
        /// 获取 hitLayers（辅助方法）
        /// </summary>
        private static LayerMask GetHitLayers(Component instance)
        {
            if (instance is Accessory_Lazer)
            {
                if (hitLayersField != null)
                {
                    var value = hitLayersField.GetValue(instance);
                    return value is LayerMask mask ? mask : (LayerMask)(int)value;
                }
            }
            else if (tecLazerType != null && tecLazerType.IsInstanceOfType(instance))
            {
                if (tecHitLayersField != null)
                {
                    var value = tecHitLayersField.GetValue(instance);
                    return value is LayerMask mask ? mask : (LayerMask)(int)value;
                }
            }
            return Physics.DefaultRaycastLayers;
        }

        /// <summary>
        /// 重置 LineRenderer 为本地空间（退出第一人称时恢复原版行为）
        /// </summary>
        private static void ResetLineRendererSpace(Component instance)
        {
            var renderer = GetLineRenderer(instance);
            if (renderer != null)
                renderer.useWorldSpace = false;
        }

        /// <summary>
        /// 检测发射器是否被遮挡
        /// </summary>
        private static bool CheckEmitterObstruction(Vector3 emitterPosition, CharacterMainControl character, LayerMask hitLayers)
        {
            try
            {
                var controller = FirstPersonCameraController.Instance;
                Vector3 cameraPosition = Vector3.zero;
                if (controller != null)
                {
                    var mainCameraField = AccessTools.Field(typeof(FirstPersonCameraController), "mainCamera");
                    if (mainCameraField != null)
                    {
                        var cam = mainCameraField.GetValue(controller) as Camera;
                        if (cam != null)
                            cameraPosition = cam.transform.position;
                    }
                }
                if (cameraPosition == Vector3.zero)
                {
                    var cam = GameCamera.Instance?.renderCamera;
                    if (cam != null)
                        cameraPosition = cam.transform.position;
                }
                if (cameraPosition == Vector3.zero)
                    return false;

                Vector3 toCamera = cameraPosition - emitterPosition;
                float distanceToCamera = toCamera.magnitude;
                if (distanceToCamera < 0.1f)
                    return false;

                Vector3 checkDirection = toCamera.normalized;
                float checkDistance = Mathf.Clamp(distanceToCamera, 0.5f, 2.0f);

                const float offsetDistance = 0.05f;
                Vector3 rayStart = emitterPosition + checkDirection * offsetDistance;
                float rayDistance = checkDistance - offsetDistance;

                if (rayDistance > 0.01f)
                {
                    LayerMask obstructionLayers = hitLayers;
                    bool hasNearByHalfObsticle = false;
                    try
                    {
                        if (character != null)
                        {
                            hasNearByHalfObsticle = character.HasNearByHalfObsticle();
                            if (hasNearByHalfObsticle)
                            {
                                int halfObsticleLayer = LayerMask.NameToLayer("HalfObsticle");
                                if (halfObsticleLayer >= 0)
                                    obstructionLayers &= ~(1 << halfObsticleLayer);
                            }
                        }
                    }
                    catch { }

                    if (Physics.Raycast(rayStart, checkDirection, out RaycastHit hit, rayDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (character != null && character.mainDamageReceiver != null)
                        {
                            var damageReceiver = hit.collider.GetComponent<DamageReceiver>();
                            if (damageReceiver != null && damageReceiver.health != null)
                            {
                                var hitCharacter = damageReceiver.health.TryGetCharacter();
                                if (hitCharacter == character)
                                    return false;
                            }
                        }
                        if (hasNearByHalfObsticle)
                        {
                            try
                            {
                                if (GameplayDataSettings.LayersData.IsLayerInLayerMask(hit.collider.gameObject.layer, GameplayDataSettings.Layers.halfObsticleLayer))
                                    return false;
                                var damageReceiver = hit.collider.GetComponent<DamageReceiver>();
                                if (damageReceiver != null && damageReceiver.isHalfObsticle)
                                    return false;
                            }
                            catch { }
                        }
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 检测激光开关快捷键
        /// </summary>
        private static void CheckLaserToggleKey()
        {
            try
            {
                if (lastToggleFrame == Time.frameCount)
                    return;

                KeyCode toggleKey = OptionsHelper.LoadKeyCode(OptionsUIConstants.LaserToggleKeyCodeKey, KeyCode.None);
                if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                {
                    isLaserEnabled = !isLaserEnabled;
                    lastToggleFrame = Time.frameCount;
                }
            }
            catch { }
        }

        /// <summary>
        /// 激光器更新核心逻辑
        /// </summary>
        public static bool Prefix(Component __instance)
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
                var renderer = GetLineRenderer(__instance);
                if (renderer != null)
                    renderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false;
            }

            // 检测激光开关快捷键
            CheckLaserToggleKey();

            // 获取LineRenderer组件
            var lineRenderer = GetLineRenderer(__instance);

            // 如果激光被禁用，隐藏激光和红点
            if (!isLaserEnabled)
            {
                if (lineRenderer != null)
                    lineRenderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false;
            }
            if (lineRenderer == null)
                return true;

            // 获取角色信息
            var character = GetCharacterFromLaser(__instance);
            if (character == null)
                return true;

            var mainCharacter = CharacterMainControl.Main;
            if (character != mainCharacter)
            {
                ResetLineRendererSpace(__instance);
                return true;
            }

            // 检查是否在瞄准状态
            bool isAiming = character.IsAiming();
            lineRenderer.enabled = isAiming;

            if (!isAiming)
            {
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false;
            }

            // 检查摄像机上下角度（pitch）
            float pitch = 0f;
            try
            {
                var pitchField = AccessTools.Field(typeof(FirstPersonCameraController), "pitch");
                if (pitchField != null && controller != null)
                    pitch = (float)pitchField.GetValue(controller);
            }
            catch { }

            const float maxPitchAngle = 15f;
            if (Mathf.Abs(pitch) > maxPitchAngle)
            {
                lineRenderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false;
            }

            // 起点：激光发射器的世界坐标
            Vector3 emitterPosition = lineRenderer.transform.position;

            // 获取原组件的hitLayers
            LayerMask hitLayers = GetHitLayers(__instance);

            // 检测发射器是否被遮挡
            bool isEmitterObstructed = CheckEmitterObstruction(emitterPosition, character, hitLayers);
            if (isEmitterObstructed)
            {
                lineRenderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false;
            }

            // 终点：当前瞄准点
            Vector3 aimPoint = controller.GetCurrentAimWorldPoint();

            // 抗抖量化
            const float quantizeStep = 0.01f;
            aimPoint = new Vector3(
                Mathf.Round(aimPoint.x / quantizeStep) * quantizeStep,
                Mathf.Round(aimPoint.y / quantizeStep) * quantizeStep,
                Mathf.Round(aimPoint.z / quantizeStep) * quantizeStep
            );

            // 计算从发射器到瞄准点的方向
            Vector3 direction = (aimPoint - emitterPosition);
            float distance = direction.magnitude;

            if (distance < 0.01f)
            {
                direction = lineRenderer.transform.forward;
                distance = Mathf.Max(character.GetAimRange(), 50f);
            }
            else
            {
                direction = direction.normalized;
            }

            // ========== 关键修改：构建射线检测掩码，如果附近有半遮挡物则排除半遮挡层 ==========
            LayerMask raycastMask = hitLayers;
            bool hasNearByHalfObsticle = false;
            try
            {
                if (character != null)
                {
                    hasNearByHalfObsticle = character.HasNearByHalfObsticle();
                    if (hasNearByHalfObsticle)
                    {
                        int halfObsticleLayer = LayerMask.NameToLayer("HalfObsticle");
                        if (halfObsticleLayer >= 0)
                        {
                            // 排除半遮挡层，使射线可以穿透沙袋
                            raycastMask &= ~(1 << halfObsticleLayer);
                        }
                    }
                }
            }
            catch { }

            // 射线检测障碍（使用调整后的掩码）
            Vector3 finalPoint = aimPoint;
            if (Physics.Raycast(emitterPosition, direction, out RaycastHit hit, distance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= MIN_HIT_DISTANCE)
                {
                    finalPoint = new Vector3(
                        Mathf.Round(hit.point.x / quantizeStep) * quantizeStep,
                        Mathf.Round(hit.point.y / quantizeStep) * quantizeStep,
                        Mathf.Round(hit.point.z / quantizeStep) * quantizeStep
                    );
                }
            }

            // 计算激光线长度，如果小于阈值则隐藏
            float laserLength = Vector3.Distance(emitterPosition, finalPoint);
            if (laserLength < LASER_LENGTH_HIDE_THRESHOLD)
            {
                lineRenderer.enabled = false;
                HideHitMarker(__instance);
                finalPoints.Remove(__instance);
                return false;
            }

            // 显示红点
            ShowHitMarker(__instance, finalPoint);

            // 绘制激光线
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.SetPosition(0, emitterPosition);
            lineRenderer.SetPosition(1, finalPoint);

            // 存储最终点
            finalPoints[__instance] = finalPoint;

            // 清空原版的localPoints
            if (__instance is Accessory_Lazer)
                localPointsField?.SetValue(__instance, null);
            else if (tecLocalPointsField != null && tecLazerType.IsInstanceOfType(__instance))
                tecLocalPointsField.SetValue(__instance, null);

            return false;
        }

        /// <summary>
        /// 在LateUpdate中强制更新所有激光的红点位置（供外部调用）
        /// </summary>
        public static void LateUpdateHitMarkers()
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode)
                return;

            var mainCharacter = CharacterMainControl.Main;
            var keys = new List<Component>(finalPoints.Keys);
            foreach (var instance in keys)
            {
                if (instance == null) continue;

                var character = GetCharacterFromLaser(instance);
                if (character != mainCharacter)
                {
                    finalPoints.Remove(instance);
                    continue;
                }

                if (finalPoints.TryGetValue(instance, out Vector3 finalPoint))
                {
                    GameObject marker = null;
                    if (instance is Accessory_Lazer)
                        marker = hitMarkerField?.GetValue(instance) as GameObject;
                    else if (tecLazerType != null && tecLazerType.IsInstanceOfType(instance))
                        marker = tecHitMarkerField?.GetValue(instance) as GameObject;

                    if (marker != null)
                    {
                        if (!marker.activeSelf)
                            marker.SetActive(true);
                        marker.transform.position = finalPoint;
                    }
                }
            }
        }

        public static bool GetLaserEnabled() => isLaserEnabled;
        public static void SetLaserEnabled(bool enabled) => isLaserEnabled = enabled;
    }

    public static class LaserPatch
    {
        public static bool GetLaserEnabled() => LaserPatchCore.GetLaserEnabled();
        public static void SetLaserEnabled(bool enabled) => LaserPatchCore.SetLaserEnabled(enabled);
        public static void LateUpdateHitMarkers() => LaserPatchCore.LateUpdateHitMarkers();
        public static CharacterMainControl GetCharacterFromLaser(Component laser) => LaserPatchCore.GetCharacterFromLaser(laser);
    }

    [HarmonyPatch(typeof(Accessory_Lazer), "ShowHitMarker")]
    internal static class LaserShowHitMarkerPatch
    {
        public static bool Prefix(Accessory_Lazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                var character = LaserPatchCore.GetCharacterFromLaser(__instance);
                var mainCharacter = CharacterMainControl.Main;
                if (character == mainCharacter)
                    return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Accessory_Lazer), "HideHitMarker")]
    internal static class LaserHideHitMarkerPatch
    {
        public static bool Prefix(Accessory_Lazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                var character = LaserPatchCore.GetCharacterFromLaser(__instance);
                var mainCharacter = CharacterMainControl.Main;
                if (character == mainCharacter)
                    return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Accessory_Lazer), "Update")]
    internal static class LaserUpdatePatch
    {
        public static bool Prefix(Accessory_Lazer __instance) => LaserPatchCore.Prefix(__instance);
    }

    [HarmonyPatch(typeof(TecLazer), "Update")]
    internal static class TecLaserUpdatePatch
    {
        public static bool Prefix(TecLazer __instance) => LaserPatchCore.Prefix(__instance);
    }

    [HarmonyPatch(typeof(TecLazer), "ShowHitMarker")]
    internal static class TecLaserShowHitMarkerPatch
    {
        public static bool Prefix(TecLazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                var character = LaserPatchCore.GetCharacterFromLaser(__instance);
                var mainCharacter = CharacterMainControl.Main;
                if (character == mainCharacter)
                    return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TecLazer), "HideHitMarker")]
    internal static class TecLaserHideHitMarkerPatch
    {
        public static bool Prefix(TecLazer __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                var character = LaserPatchCore.GetCharacterFromLaser(__instance);
                var mainCharacter = CharacterMainControl.Main;
                if (character == mainCharacter)
                    return false;
            }
            return true;
        }
    }
}