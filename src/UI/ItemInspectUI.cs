using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace FirstPersonCamera
{
    /// <summary>
    /// Shows an item icon at screen center for a short duration.
    /// Supports interrupt and configurable size/duration.
    /// </summary>
    public class ItemInspectUI : MonoBehaviour
    {
        private static ItemInspectUI _instance;
        public static ItemInspectUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("FirstPerson_ItemInspectUI");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ItemInspectUI>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        private Canvas canvas;
        private CanvasScaler scaler;
        private Image image;
        private RectTransform imageRect;
        private float hideAtTime;
        private bool visible;
        private float baseSize = 256f;
        private bool useNewInputSystem;

        private void Init()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20000; // above most UIs

            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var go = new GameObject("Icon");
            go.transform.SetParent(transform, false);
            imageRect = go.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.sizeDelta = new Vector2(baseSize, baseSize);
            image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            
            useNewInputSystem = Keyboard.current != null && Mouse.current != null;
        }

        public static void Show(Sprite sprite, float seconds, float sizeMultiplier = 1f)
        {
            if (sprite == null) return;
            var ui = Instance;
            ui.image.sprite = sprite;
            ui.image.preserveAspect = true;
            
            // 应用大小倍数
            float size = ui.baseSize * sizeMultiplier;
            ui.imageRect.sizeDelta = new Vector2(size, size);
            
            ui.image.enabled = true;
            ui.visible = true;
            ui.hideAtTime = Time.unscaledTime + Mathf.Max(0.1f, seconds);
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance.image.enabled = false;
            _instance.visible = false;
            _instance.hideAtTime = 0f;
        }

        public static bool IsVisible()
        {
            return _instance != null && _instance.visible;
        }

        private void Update()
        {
            if (!visible) return;
            
            // 检查是否被打断（与武器检视相同的打断规则）
            bool shouldInterrupt = false;
            
            try
            {
                // 检查是否在ADS状态
                var controller = FirstPersonCameraController.Instance;
                if (controller != null && controller.IsInAdsState())
                {
                    shouldInterrupt = true;
                }
                
                // 检查鼠标左键是否按下
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
                if (mousePressed) shouldInterrupt = true;
                
                // 检查是否在跑步
                bool running = false;
                if (useNewInputSystem)
                {
                    var kb = Keyboard.current;
                    if (kb != null) running = kb.leftShiftKey.isPressed;
                }
                else
                {
                    running = Input.GetKey(KeyCode.LeftShift);
                }
                if (running) shouldInterrupt = true;
            }
            catch { }
            
            if (shouldInterrupt || Time.unscaledTime >= hideAtTime)
            {
                Hide();
                // 通知控制器停止UI检视
                try
                {
                    var controller = FirstPersonCameraController.Instance;
                    if (controller != null)
                    {
                        controller.StopUIItemInspect();
                    }
                }
                catch { }
            }
        }
    }
}
