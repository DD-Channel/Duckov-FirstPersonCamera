using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using FirstPersonCamera.Utilities; // 如需使用 FPLogger

namespace FirstPersonCamera
{
    /// <summary>
    /// 追踪目标UI框：当武器具备追踪能力且开镜时，显示锁定框在追踪目标头部。
    /// 完全依赖原生逻辑，不修改子弹方向。
    /// </summary>
    public class LockOnSystem : MonoBehaviour
    {
        #region 单例与初始化
        private static LockOnSystem _instance;
        public static LockOnSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LockOnSystem");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LockOnSystem>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region UI配置
        [Header("UI Settings")]
        public float boxSize = 64f;
        public Color boxColor = Color.white;
        public int canvasSortOrder = 5000;
        public bool enableDebugLog = true; // 调试开关
        #endregion

        #region 运行时数据
        private Canvas lockCanvas;
        private Image lockBox;
        private RectTransform boxRect;
        private Camera mainCamera;
        #endregion

        #region 初始化UI
        private void EnsureCanvas()
        {
            if (lockCanvas != null) return;

            var canvasGO = new GameObject("LockOnCanvas");
            canvasGO.transform.SetParent(transform);
            lockCanvas = canvasGO.AddComponent<Canvas>();
            lockCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lockCanvas.sortingOrder = canvasSortOrder;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>().enabled = false;

            var boxGO = new GameObject("LockBox");
            boxGO.transform.SetParent(lockCanvas.transform, false);
            boxRect = boxGO.AddComponent<RectTransform>();
            boxRect.sizeDelta = new Vector2(boxSize, boxSize);
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);

            lockBox = boxGO.AddComponent<Image>();
            lockBox.sprite = CreateCornerBoxSprite();
            lockBox.color = boxColor;
            lockBox.raycastTarget = false;
            lockBox.gameObject.SetActive(false);
        }

        private Sprite CreateCornerBoxSprite()
        {
            int size = 64;
            int borderThickness = 3;
            int cornerLength = 20;
            var tex = new Texture2D(size, size);
            Color transparent = Color.clear;
            Color white = Color.white;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    tex.SetPixel(x, y, transparent);

                    if (x < cornerLength && y > size - borderThickness - 1 && y < size)
                        tex.SetPixel(x, y, white);
                    if (x < borderThickness && y > size - cornerLength - 1 && y < size)
                        tex.SetPixel(x, y, white);

                    if (x > size - borderThickness - 1 && x < size && y > size - cornerLength - 1 && y < size)
                        tex.SetPixel(x, y, white);
                    if (x > size - cornerLength - 1 && x < size && y > size - borderThickness - 1 && y < size)
                        tex.SetPixel(x, y, white);

                    if (x < cornerLength && y < borderThickness)
                        tex.SetPixel(x, y, white);
                    if (x < borderThickness && y < cornerLength)
                        tex.SetPixel(x, y, white);

                    if (x > size - borderThickness - 1 && x < size && y < cornerLength)
                        tex.SetPixel(x, y, white);
                    if (x > size - cornerLength - 1 && x < size && y < borderThickness)
                        tex.SetPixel(x, y, white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        #endregion

        #region 核心逻辑
        private void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main ?? FindObjectOfType<Camera>();

            if (!ShouldShowLockBox())
            {
                if (lockBox != null && lockBox.gameObject.activeSelf)
                {
                    lockBox.gameObject.SetActive(false);
                    // if (enableDebugLog) Debug.Log("[LockOn] 条件不满足，隐藏锁定框");
                }
                return;
            }

            EnsureCanvas();

            var gun = GetCurrentGun();
            if (gun == null)
            {
                // if (enableDebugLog) Debug.Log("[LockOn] 当前没有枪械");
                return;
            }

            var traceTarget = gun.TraceTarget;
            if (traceTarget == null)
            {
                if (lockBox != null) lockBox.gameObject.SetActive(false);
                // if (enableDebugLog) Debug.Log("[LockOn] 追踪目标为空");
                return;
            }

            if (traceTarget.Health == null || traceTarget.Health.IsDead)
            {
                if (lockBox != null) lockBox.gameObject.SetActive(false);
                // if (enableDebugLog) Debug.Log("[LockOn] 追踪目标已死亡");
                return;
            }

            // 获取目标头部位置
            Vector3 headPos = GetHeadPosition(traceTarget);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(headPos);
            if (screenPos.z > 0)
            {
                boxRect.position = screenPos;
                if (!lockBox.gameObject.activeSelf)
                {
                    lockBox.gameObject.SetActive(true);
                    // if (enableDebugLog) Debug.Log($"[LockOn] 显示锁定框，目标位置: {headPos}");
                }
            }
            else
            {
                lockBox.gameObject.SetActive(false);
                // if (enableDebugLog) Debug.Log("[LockOn] 目标在相机后方");
            }
        }

        /// <summary>
        /// 判断是否应该显示锁定框
        /// </summary>
        private bool ShouldShowLockBox()
        {
            var gun = GetCurrentGun();
            if (gun == null)
            {
                // if (enableDebugLog) Debug.Log("[LockOn] ShouldShow: 无枪械");
                return false;
            }

            // 放宽条件：只要有追踪能力（TraceAbility > 0）且正在开镜（AdsValue > 0.2f）
            float trace = gun.TraceAbility;
            float ads = gun.AdsValue;

            // if (enableDebugLog && Time.frameCount % 30 == 0) // 每30帧打印一次避免刷屏
            //     Debug.Log($"[LockOn] TraceAbility={trace}, AdsValue={ads}");

            return trace > 0.01f && ads > 0.2f;
        }

        /// <summary>
        /// 获取当前玩家持有的枪械
        /// </summary>
        private ItemAgent_Gun GetCurrentGun()
        {
            var character = CharacterMainControl.Main;
            if (character == null) return null;
            return character.GetGun();
        }

        /// <summary>
        /// 获取目标头部世界坐标
        /// </summary>
        private Vector3 GetHeadPosition(CharacterMainControl target)
        {
            if (target.characterModel != null && target.characterModel.HelmatSocket != null)
                return target.characterModel.HelmatSocket.position;
            return target.transform.position + Vector3.up * 1.7f;
        }
        #endregion
    }
}