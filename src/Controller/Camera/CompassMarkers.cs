using System;
using System.Collections.Generic;
using System.Linq;
using Duckov.MiniMaps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 标记模块常量
        private const int MAX_MARKER_SLOTS = 10;
        private const float DEFAULT_ICON_SIZE = 24f;
        private const float DISTANCE_TEXT_Y_OFFSET = -20f;
        #endregion

        #region 标记模块字段
        private List<CompassMarkerSlot> markerSlots = new List<CompassMarkerSlot>();
        private Dictionary<MonoBehaviour, CompassMarkerSlot> markerSlotMap = new Dictionary<MonoBehaviour, CompassMarkerSlot>();
        private Sprite defaultMarkerIcon;
        private bool markerEventsSubscribed;

        private class CompassMarkerSlot
        {
            public GameObject gameObjectA;
            public GameObject gameObjectB;
            public RectTransform rectA;
            public RectTransform rectB;
            public Image iconA;
            public Image iconB;
            public TextMeshProUGUI textA;
            public TextMeshProUGUI textB;
            public MonoBehaviour targetMarker;

            // 控制透明度以实现显示/隐藏（避免禁用GameObject导致激活问题）
            private CanvasGroup canvasGroupA;
            private CanvasGroup canvasGroupB;

            public void InitCanvasGroups()
            {
                if (gameObjectA != null)
                {
                    canvasGroupA = gameObjectA.GetComponent<CanvasGroup>();
                    if (canvasGroupA == null) canvasGroupA = gameObjectA.AddComponent<CanvasGroup>();
                }
                if (gameObjectB != null)
                {
                    canvasGroupB = gameObjectB.GetComponent<CanvasGroup>();
                    if (canvasGroupB == null) canvasGroupB = gameObjectB.AddComponent<CanvasGroup>();
                }
            }

            public void SetVisible(bool visible)
            {
                float alpha = visible ? 1f : 0f;
                if (canvasGroupA != null) canvasGroupA.alpha = alpha;
                if (canvasGroupB != null) canvasGroupB.alpha = alpha;
            }

            public void SetActive(bool active)
            {
                // 保持GameObject始终激活，通过透明度控制可见性
                if (gameObjectA != null) gameObjectA.SetActive(active);
                if (gameObjectB != null) gameObjectB.SetActive(active);
            }

            public void SetPosition(float x, float segmentWidth)
            {
                if (rectA != null) rectA.anchoredPosition = new Vector2(x, 0);
                if (rectB != null) rectB.anchoredPosition = new Vector2(x + segmentWidth, 0);
            }

            public void SetDistance(float distance)
            {
                string text = $"{distance:F0}m";
                if (textA != null) textA.text = text;
                if (textB != null) textB.text = text;
            }

            public void SetIcon(Sprite sprite, Color color)
            {
                if (sprite != null)
                {
                    if (iconA != null) iconA.sprite = sprite;
                    if (iconB != null) iconB.sprite = sprite;
                }
                if (iconA != null) iconA.color = color;
                if (iconB != null) iconB.color = color;
            }
        }
        #endregion

        #region 标记模块初始化
        private void CreateMarkerSlots()
        {
            if (compassContent == null)
            {
                FPLogger.LogError("指南针内容容器为空，无法创建标记槽位");
                return;
            }

            if (defaultMarkerIcon == null)
                defaultMarkerIcon = CreateDefaultMarkerIcon();

            for (int i = 0; i < MAX_MARKER_SLOTS; i++)
            {
                var slot = new CompassMarkerSlot();

                var goA = CreateSingleMarker($"CompassMarker_{i}_A", compassContent);
                slot.gameObjectA = goA;
                slot.rectA = goA.GetComponent<RectTransform>();
                slot.iconA = goA.transform.Find("Icon").GetComponent<Image>();
                slot.textA = goA.transform.Find("Distance").GetComponent<TextMeshProUGUI>();

                var goB = CreateSingleMarker($"CompassMarker_{i}_B", compassContent);
                slot.gameObjectB = goB;
                slot.rectB = goB.GetComponent<RectTransform>();
                slot.iconB = goB.transform.Find("Icon").GetComponent<Image>();
                slot.textB = goB.transform.Find("Distance").GetComponent<TextMeshProUGUI>();

                slot.InitCanvasGroups(); // 初始化 CanvasGroup
                markerSlots.Add(slot);
                slot.SetActive(false); // 初始禁用
            }

            FPLogger.Log($"已创建 {MAX_MARKER_SLOTS} 个双段指南针标记槽位");
        }

        private GameObject CreateSingleMarker(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(DEFAULT_ICON_SIZE, DEFAULT_ICON_SIZE);
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(DEFAULT_ICON_SIZE, DEFAULT_ICON_SIZE);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.raycastTarget = false;

            var textGO = new GameObject("Distance");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, DISTANCE_TEXT_Y_OFFSET);
            textRect.sizeDelta = new Vector2(60, 20);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return go;
        }

        private Sprite CreateDefaultMarkerIcon()
        {
            var texture = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        #endregion

        #region 事件订阅与取消
        private void SubscribeMarkerEvents()
        {
            if (markerEventsSubscribed) return;
            try
            {
                PointsOfInterests.OnPointRegistered += OnMarkerRegistered;
                PointsOfInterests.OnPointUnregistered += OnMarkerUnregistered;
                markerEventsSubscribed = true;
                FPLogger.Log("已订阅指南针标记事件");
                ProcessExistingMarkers();
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "订阅指南针标记事件失败");
            }
        }

        private void UnsubscribeMarkerEvents()
        {
            if (!markerEventsSubscribed) return;
            try
            {
                PointsOfInterests.OnPointRegistered -= OnMarkerRegistered;
                PointsOfInterests.OnPointUnregistered -= OnMarkerUnregistered;
                markerEventsSubscribed = false;
                FPLogger.Log("已取消订阅指南针标记事件");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "取消订阅指南针标记事件失败");
            }
        }

        private void ProcessExistingMarkers()
        {
            try
            {
                foreach (var poi in PointsOfInterests.Points)
                    if (poi != null) OnMarkerRegistered(poi);
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "处理现有标记时出错");
            }
        }

        private void OnMarkerRegistered(MonoBehaviour marker)
        {
            if (marker == null) return;
            if (!(marker is MapMarkerPOI)) return;
            var poi = marker as IPointOfInterest;
            if (poi == null) return;
            if (markerSlotMap.ContainsKey(marker)) return;

            var slot = markerSlots.FirstOrDefault(s => !s.gameObjectA.activeSelf);
            if (slot == null)
            {
                FPLogger.LogWarning("指南针标记槽位不足，无法显示新标记");
                return;
            }

            slot.targetMarker = marker;
            slot.SetIcon(poi.Icon ?? defaultMarkerIcon, poi.Color);
            slot.SetActive(true);
            slot.SetVisible(false); // 初始不可见
            markerSlotMap[marker] = slot;
        }

        private void OnMarkerUnregistered(MonoBehaviour marker)
        {
            if (marker == null) return;
            if (markerSlotMap.TryGetValue(marker, out var slot))
            {
                slot.SetActive(false);
                slot.targetMarker = null;
                markerSlotMap.Remove(marker);
            }
        }
        #endregion

        #region 标记更新逻辑
        private void LateUpdateMarkers()
        {
            if (!isFirstPersonMode || mainCamera == null || markerSlots.Count == 0)
                return;

            // 相机朝向的世界角度（0°=正北，顺时针）
            float cameraYaw = (mainCamera.transform.eulerAngles.y + 360) % 360;

            foreach (var slot in markerSlots)
            {
                if (!slot.gameObjectA.activeSelf || slot.targetMarker == null) continue;

                Vector3 toMarker = slot.targetMarker.transform.position - mainCamera.transform.position;
                toMarker.y = 0;
                if (toMarker.sqrMagnitude < 0.01f)
                {
                    slot.SetVisible(false);
                    continue;
                }

                Vector3 markerDir = toMarker.normalized;
                // 标记的世界角度（0°=正北，顺时针）
                float markerWorldAngle = Vector3.SignedAngle(Vector3.forward, markerDir, Vector3.up);
                if (markerWorldAngle < 0) markerWorldAngle += 360;

                // 计算与相机朝向的角度差（-180°~180°）
                float deltaAngle = Mathf.DeltaAngle(cameraYaw, markerWorldAngle);
                bool inFront = Mathf.Abs(deltaAngle) <= 90f; // 前方180°范围

                // 根据是否在前方设置可见性
                slot.SetVisible(inFront);

                // 始终更新位置（即使不可见，下次变为可见时位置正确）
                float xPos = markerWorldAngle * PixelsPerDegree;
                slot.SetPosition(xPos, segmentWidthPx);
                slot.SetDistance(Vector3.Distance(mainCamera.transform.position, slot.targetMarker.transform.position));
            }
        }

        private void OnSceneLoadedForMarkers(Scene scene, LoadSceneMode mode)
        {
            foreach (var slot in markerSlots)
            {
                slot.SetActive(false);
                slot.targetMarker = null;
            }
            markerSlotMap.Clear();
            UnsubscribeMarkerEvents();
            SubscribeMarkerEvents();
        }
        #endregion

        #region 生命周期集成
        private void InitializeMarkerModule()
        {
            CreateMarkerSlots();
            SubscribeMarkerEvents();
            SceneManager.sceneLoaded += OnSceneLoadedForMarkers;
        }

        private void CleanupMarkerModule()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForMarkers;
            UnsubscribeMarkerEvents();
        }
        #endregion
    }
}