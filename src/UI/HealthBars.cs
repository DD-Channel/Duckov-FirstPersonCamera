using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Duckov.UI;
using HarmonyLib;
using UnityEngine.UI;
using TMPro;

namespace FirstPersonCamera
{
    // 受伤时间跟踪器
    public static class DamageTimeTracker
    {
        private static Dictionary<Health, float> lastTimeToHurt = new Dictionary<Health, float>();
        private const float HURT_VISIBLE_DURATION = 3f;

        public static bool IsRecentlyHurt(Health health)
        {
            if (health == null) return false;
            if (lastTimeToHurt.TryGetValue(health, out float lastHurt))
            {
                if (Time.time - lastHurt <= HURT_VISIBLE_DURATION)
                    return true;
                else
                    lastTimeToHurt.Remove(health);
            }
            return false;
        }

        public static void RecordHurt(Health health)
        {
            if (health != null)
                lastTimeToHurt[health] = Time.time;
        }

        public static void ClearHealth(Health health)
        {
            if (health != null)
                lastTimeToHurt.Remove(health);
        }
    }

    //Harmony 补丁：捕获受伤和死亡
    [HarmonyPatch(typeof(HealthBar))]
    [HarmonyPatch("OnTargetHurt")]
    public static class HealthBar_OnTargetHurt_Patch
    {
        public static void Postfix(HealthBar __instance)
        {
            if (__instance.target != null)
                DamageTimeTracker.RecordHurt(__instance.target);
        }
    }

    [HarmonyPatch(typeof(HealthBar))]
    [HarmonyPatch("OnTargetDead")]
    public static class HealthBar_OnTargetDead_Patch
    {
        public static void Postfix(HealthBar __instance)
        {
            if (__instance.target != null)
                DamageTimeTracker.ClearHealth(__instance.target);
        }
    }

    // 核心血条控制补丁（每帧更新）
    [HarmonyPatch(typeof(HealthBar))]
    [HarmonyPatch("UpdatePosition")]
    public static class HealthBar_UpdatePosition_Extender
    {
        private const float HealthBarFadeDelay = 3f;

        public static void Postfix(HealthBar __instance)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode)
                return; // 非第一人称模式，原版行为不受影响

            // 获取 CanvasGroup（由分帧扫描预先添加）
            CanvasGroup cg = __instance.GetComponent<CanvasGroup>();
            if (cg == null) return; // 尚未初始化，跳过

            // 基础条件：目标存在且非隐藏
            bool shouldShow = __instance.target != null && !__instance.target.Hidden;

            // 玩家自己的血条永远隐藏
            if (shouldShow && __instance.target.IsMainCharacterHealth)
                shouldShow = false;

            // 对所有非玩家目标（无论敌友）应用受伤+视野检测
            if (shouldShow && !__instance.target.IsMainCharacterHealth)
            {
                bool recentlyHurt = DamageTimeTracker.IsRecentlyHurt(__instance.target);
                bool inView = controller.IsInFrustum(__instance.target);
                shouldShow = recentlyHurt && inView;
            }

            // 设置透明度（1显示，0隐藏）
            cg.alpha = shouldShow ? 1f : 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    // 第一人称相机控制器 - 血条管理模块
    public partial class FirstPersonCameraController
    {
        #region 常量
        private const int LIGHT_SWEEP_MAX_COUNT_PER_FRAME = 32;
        private const float VISIBILITY_UPDATE_INTERVAL = 0.1f;
        private const float FULL_SCAN_INTERVAL = 1f;
        #endregion

        #region 私有字段
        private readonly Dictionary<Health, bool> cachedShowHealthBar = new Dictionary<Health, bool>();
        private readonly Dictionary<HealthBar, float> transparentBarsPrevAlpha = new Dictionary<HealthBar, float>();
        private readonly Dictionary<HealthBar, CanvasGroup> cachedCanvasGroups = new Dictionary<HealthBar, CanvasGroup>();

        private bool eventHooked;
        private float nextHealthBarSweepTime;
        private float nextFullScanTime;
        private int lightSweepCursor;
        private HealthBar[] lightSweepCache;
        private Camera playerCamera;
        private Plane[] frustumPlanes = new Plane[6];
        #endregion

        #region 事件管理
        private void HookHealthBarEvents()
        {
            if (eventHooked) return;
            try
            {
                Health.OnRequestHealthBar += OnHealthBarRequested;
                eventHooked = true;
            }
            catch { }
        }

        private void UnhookHealthBarEvents()
        {
            if (!eventHooked) return;
            try
            {
                Health.OnRequestHealthBar -= OnHealthBarRequested;
            }
            catch { }
            finally
            {
                eventHooked = false;
            }
        }
        #endregion

        #region 目标识别
        private bool IsEnemy(Health health)
        {
            if (health == null || health.IsMainCharacterHealth) return false;
            return Team.IsEnemy(Teams.player, health.team);
        }

        private bool IsSelf(Health health) => health != null && health.IsMainCharacterHealth;
        private bool IsTarget(Health health) => IsEnemy(health) || IsSelf(health);
        #endregion

        #region 视野检测
        private Camera GetPlayerCamera()
        {
            if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy)
                playerCamera = GetComponent<Camera>() ?? Camera.main;
            return playerCamera;
        }

        public bool IsInFrustum(Health health)
        {
            Camera cam = GetPlayerCamera();
            if (cam == null) return false;

            float maxViewDistance = 100f; // 可调整
            if (Vector3.Distance(cam.transform.position, health.transform.position) > maxViewDistance)
                return false;

            Renderer[] renderers = health.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);
                return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
            }
            else
            {
                Collider col = health.GetComponent<Collider>();
                Bounds bounds = col != null ? col.bounds : new Bounds(health.transform.position, Vector3.one * 1f);
                GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);
                return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
            }
        }
        #endregion

        #region 事件回调：为新建血条添加 CanvasGroup
        private void OnHealthBarRequested(Health health)
        {
            if (!isFirstPersonMode) return;
            if (!IsTarget(health)) return;

            StartCoroutine(AddCanvasGroupNextFrame(health));

            if (!cachedShowHealthBar.ContainsKey(health))
                cachedShowHealthBar[health] = health.showHealthBar; // 仅缓存，不修改原值
        }

        private IEnumerator AddCanvasGroupNextFrame(Health health)
        {
            yield return null;
            if (health != null && isFirstPersonMode)
                EnsureCanvasGroupForHealth(health);
        }

        private void EnsureCanvasGroupForHealth(Health health)
        {
            try
            {
                var bars = UnityEngine.Object.FindObjectsOfType<HealthBar>(true);
                foreach (var bar in bars)
                {
                    if (bar == null || bar.target != health || !bar.gameObject.activeInHierarchy)
                        continue;
                    EnsureCanvasGroup(bar);
                }
            }
            catch { }
        }

        private CanvasGroup EnsureCanvasGroup(HealthBar bar)
        {
            if (cachedCanvasGroups.TryGetValue(bar, out CanvasGroup cg) && cg != null)
                return cg;

            cg = bar.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = bar.gameObject.AddComponent<CanvasGroup>();
            cachedCanvasGroups[bar] = cg;

            if (!transparentBarsPrevAlpha.ContainsKey(bar))
                transparentBarsPrevAlpha[bar] = cg.alpha;

            // 初始透明度由 UpdatePosition 补丁控制，此处无需设置
            return cg;
        }
        #endregion

        #region 分帧扫描（仅用于缓存管理和恢复，不再控制可见性）
        private void LightSweepTransparentSomeBars(int maxCount)
        {
            if (Time.time < nextHealthBarSweepTime) return;
            nextHealthBarSweepTime = Time.time + VISIBILITY_UPDATE_INTERVAL;

            if (lightSweepCache == null || lightSweepCursor >= lightSweepCache.Length)
            {
                if (Time.time >= nextFullScanTime)
                {
                    lightSweepCache = UnityEngine.Object.FindObjectsOfType<HealthBar>(true);
                    nextFullScanTime = Time.time + FULL_SCAN_INTERVAL;
                    lightSweepCursor = 0;
                }
                else
                {
                    return;
                }
            }

            int processed = 0;
            while (lightSweepCache != null && lightSweepCursor < lightSweepCache.Length && processed < maxCount)
            {
                var bar = lightSweepCache[lightSweepCursor++];
                if (bar == null || !bar.gameObject.activeInHierarchy) continue;

                // 确保 CanvasGroup 存在且被缓存
                EnsureCanvasGroup(bar);

                var target = bar.target;
                if (target != null && !cachedShowHealthBar.ContainsKey(target))
                    cachedShowHealthBar[target] = target.showHealthBar;

                processed++;
            }

            if (lightSweepCache != null && lightSweepCursor >= lightSweepCache.Length)
            {
                lightSweepCache = null;
                lightSweepCursor = 0;
            }
        }
        #endregion

        #region 恢复方法
        private void RestoreEnemyHealthBarFlags()
        {
            try
            {
                foreach (var kvp in cachedShowHealthBar)
                {
                    if (kvp.Key != null)
                        kvp.Key.showHealthBar = kvp.Value;
                }
            }
            catch { }
            finally
            {
                cachedShowHealthBar.Clear();
            }
        }

        private void RestoreEnemyHealthBarTransparency()
        {
            try
            {
                foreach (var kvp in transparentBarsPrevAlpha)
                {
                    var bar = kvp.Key;
                    if (bar == null) continue;
                    if (cachedCanvasGroups.TryGetValue(bar, out CanvasGroup cg) && cg != null)
                        cg.alpha = kvp.Value;
                    else
                    {
                        cg = bar.GetComponent<CanvasGroup>();
                        if (cg != null)
                            cg.alpha = kvp.Value;
                    }
                }
            }
            catch { }
            finally
            {
                transparentBarsPrevAlpha.Clear();
                cachedCanvasGroups.Clear();
            }
        }
        #endregion

        // 旧字段存根（保持兼容）
        private float nextHealthBarSweepTimeOld;
        private int lightSweepCursorOld;
        private HealthBar[] lightSweepCacheOld;
        private void HookHealthBarEventsOld() { }
        private void UnhookHealthBarEventsOld() { }
        private void LightSweepTransparentSomeBarsOld(int maxCount) { }
        private void RestoreEnemyHealthBarTransparencyOld() { }
        private void RestoreEnemyHealthBarFlagsOld() { }
    }
     // 强制关闭透视（游戏默认开启，此补丁无条件关闭）
    [HarmonyPatch(typeof(CharacterMainControl))]
    [HarmonyPatch("SetCharacterModel")]
    public static class CharacterSetModelPatch_DisableWallHack
    {
        private static Shader _showBackShader;

        public static void Postfix(CharacterMainControl __instance)
        {
            // 加载着色器（如果尚未加载）
            if (_showBackShader == null)
            {
                _showBackShader = Shader.Find("CharacterShowBack");
                if (_showBackShader == null)
                {
                    Debug.LogError("[WallHack] 找不到着色器 'CharacterShowBack'，无法关闭透视！");
                    return;
                }
            }

            // 遍历所有 SkinnedMeshRenderer，将使用了该着色器的材质的渲染队列设为 0（背景队列），从而关闭透视
            foreach (var renderer in __instance.characterModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat != null && mat.shader == _showBackShader)
                    {
                        mat.renderQueue = 0; // 恢复默认渲染顺序，取消透视
                        // 可选：为避免重复操作，可以添加标记，但非必须
                    }
                }
            }
        }
    }
}