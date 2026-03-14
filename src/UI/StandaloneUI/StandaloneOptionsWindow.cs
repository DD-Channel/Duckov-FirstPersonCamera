using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.UI; // 新增：用于 LocalizedText 和 FPLocalization
using System.Collections;
using System.Collections.Generic;
using Duckov;
using FirstPersonCamera;

namespace FirstPersonCamera.StandaloneUI
{
    /// <summary>
    /// 独立选项窗口
    /// 完全自己绘制的UI系统，不依赖游戏官方UI
    /// </summary>
    public class StandaloneOptionsWindow : MonoBehaviour
    {
        #region 单例
        private static StandaloneOptionsWindow instance;
        public static StandaloneOptionsWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("FirstPersonCamera_StandaloneOptionsWindow");
                    instance = go.AddComponent<StandaloneOptionsWindow>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region 私有字段
        private Canvas canvas;
        private GameObject windowPanel;
        private GameObject tabButtonContainer;
        private GameObject contentContainer;
        private ScrollRect scrollRect;
        private VerticalLayoutGroup contentLayout;
        
        private Dictionary<string, GameObject> tabContents = new Dictionary<string, GameObject>();
        private string currentTab = "";
        
        // UI组件引用
        private Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();
        private Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();
        private Dictionary<string, TMP_Dropdown> dropdowns = new Dictionary<string, TMP_Dropdown>();
        private Dictionary<string, Button> keybindButtons = new Dictionary<string, Button>();
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                // 确保游戏对象激活
                gameObject.SetActive(true);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            CreateUI();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
        #endregion

        #region UI创建
        /// <summary>
        /// 创建UI
        /// </summary>
        private void CreateUI()
        {
            // 创建Canvas
            CreateCanvas();
            
            // 创建窗口面板
            CreateWindowPanel();
            
            // 创建标签按钮容器
            CreateTabButtons();
            
            // 创建内容区域
            CreateContentArea();
            
            // 创建所有标签页
            CreateAllTabs();
            
            // 默认显示：基础设置
            if (tabContents.Count > 0)
            {
                ShowTab("Basic");
            }
            
            // 在UI初始化完成后，保存所有配置项（包括默认值）
            // 使用协程延迟一帧，确保所有配置项都已加载
            StartCoroutine(SaveAllConfigsDelayed());
            
            // 初始隐藏窗口（但保持Canvas和windowPanel创建时的激活状态）
            // 注意：不要调用SetVisible(false)，因为这会禁用Canvas
            // 我们只需要隐藏windowPanel即可
            if (windowPanel != null)
            {
                windowPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 创建Canvas
        /// </summary>
        private void CreateCanvas()
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(transform, false);
            
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000; // 确保在最上层，高于ESC菜单和设置界面
            
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// 创建窗口面板
        /// </summary>
        private void CreateWindowPanel()
        {
            windowPanel = new GameObject("WindowPanel");
            windowPanel.transform.SetParent(canvas.transform, false);
            
            var rectTransform = windowPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(1000, 750); // 增加窗口大小
            rectTransform.anchoredPosition = Vector2.zero;
            
            var image = windowPanel.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            // 创建标题栏
            CreateTitleBar();
            
            // 创建关闭按钮
            CreateCloseButton();
        }

        /// <summary>
        /// 创建标题栏（已本地化）
        /// </summary>
        private void CreateTitleBar()
        {
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(windowPanel.transform, false);
            
            var rectTransform = titleBar.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(0f, 50f);
            rectTransform.anchoredPosition = Vector2.zero;
            
            var image = titleBar.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var titleText = new GameObject("TitleText");
            titleText.transform.SetParent(titleBar.transform, false);
            
            var titleRectTransform = titleText.AddComponent<RectTransform>();
            titleRectTransform.anchorMin = Vector2.zero;
            titleRectTransform.anchorMax = Vector2.one;
            titleRectTransform.sizeDelta = Vector2.zero;
            titleRectTransform.anchoredPosition = Vector2.zero;
            
            var text = titleText.AddComponent<TextMeshProUGUI>();
            text.text = FPLocalization.Get("FPC_SettingsTitle");
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            // 添加本地化组件
            var localized = titleText.AddComponent<LocalizedText>();
            localized.SetKey("FPC_SettingsTitle");
            
            // 在标题栏左上角添加版本号显示
            var versionLabel = new GameObject("VersionLabel");
            versionLabel.transform.SetParent(titleBar.transform, false);
            
            var versionRectTransform = versionLabel.AddComponent<RectTransform>();
            versionRectTransform.anchorMin = new Vector2(0f, 0f);
            versionRectTransform.anchorMax = new Vector2(0f, 1f);
            versionRectTransform.pivot = new Vector2(0f, 0.5f);
            versionRectTransform.sizeDelta = new Vector2(150f, 0f);
            versionRectTransform.anchoredPosition = new Vector2(10f, 0f);
            
            var versionText = versionLabel.AddComponent<TextMeshProUGUI>();
            versionText.text = $"Version: {OptionsUIConstants.ModVersion}";
            versionText.fontSize = 16f;
            versionText.alignment = TextAlignmentOptions.Left;
            versionText.color = Color.green; // 使用绿色显示版本号
        }

        /// <summary>
        /// 创建关闭按钮
        /// </summary>
        private void CreateCloseButton()
        {
            var closeButton = new GameObject("CloseButton");
            closeButton.transform.SetParent(windowPanel.transform, false);
            
            var rectTransform = closeButton.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.sizeDelta = new Vector2(40f, 40f);
            rectTransform.anchoredPosition = new Vector2(-5f, -5f);
            
            var image = closeButton.AddComponent<Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            var button = closeButton.AddComponent<Button>();
            button.onClick.AddListener(() => SetVisible(false));
            
            var text = new GameObject("Text");
            text.transform.SetParent(closeButton.transform, false);
            
            var textRectTransform = text.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.anchoredPosition = Vector2.zero;
            
            var textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = "×";
            textComponent.fontSize = 32f;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
        }

        /// <summary>
        /// 创建标签按钮容器
        /// </summary>
        private void CreateTabButtons()
        {
            tabButtonContainer = new GameObject("TabButtons");
            tabButtonContainer.transform.SetParent(windowPanel.transform, false);
            
            var rectTransform = tabButtonContainer.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(0f, 50f);
            rectTransform.anchoredPosition = new Vector2(0f, -50f); // 在标题栏下方
            
            var horizontalLayout = tabButtonContainer.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.spacing = 8f; // 增加标签按钮间距
            horizontalLayout.padding = new RectOffset(15, 15, 8, 8); // 增加内边距
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = true;
        }

        /// <summary>
        /// 创建内容区域
        /// </summary>
        private void CreateContentArea()
        {
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(windowPanel.transform, false);
            
            var rectTransform = contentArea.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            // 使用offset来为标题栏和标签栏留出空间
            rectTransform.offsetMin = new Vector2(0f, 0f); // 底部
            rectTransform.offsetMax = new Vector2(0f, -100f); // 顶部，留出100px给标题栏和标签栏
            
            // 添加ScrollRect
            scrollRect = contentArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 1f;
            
            // 创建Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(contentArea.transform, false);
            
            var viewportRectTransform = viewport.AddComponent<RectTransform>();
            viewportRectTransform.anchorMin = Vector2.zero;
            viewportRectTransform.anchorMax = Vector2.one;
            viewportRectTransform.sizeDelta = Vector2.zero;
            viewportRectTransform.anchoredPosition = Vector2.zero;
            
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            scrollRect.viewport = viewportRectTransform;
            
            // 创建Content容器
            contentContainer = new GameObject("Content");
            contentContainer.transform.SetParent(viewport.transform, false);
            
            var contentRectTransform = contentContainer.AddComponent<RectTransform>();
            contentRectTransform.anchorMin = new Vector2(0f, 1f);
            contentRectTransform.anchorMax = new Vector2(1f, 1f);
            contentRectTransform.pivot = new Vector2(0.5f, 1f);
            contentRectTransform.sizeDelta = new Vector2(0f, 0f);
            contentRectTransform.anchoredPosition = Vector2.zero;
            
            contentLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.spacing = 10f;
            contentLayout.padding = new RectOffset(20, 20, 20, 20);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            
            var contentSizeFitter = contentContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRectTransform;
            
            // 创建滚动条
            var scrollbar = new GameObject("Scrollbar");
            scrollbar.transform.SetParent(contentArea.transform, false);
            
            var scrollbarRectTransform = scrollbar.AddComponent<RectTransform>();
            scrollbarRectTransform.anchorMin = new Vector2(1f, 0f);
            scrollbarRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollbarRectTransform.pivot = new Vector2(1f, 0.5f);
            scrollbarRectTransform.sizeDelta = new Vector2(20f, 0f);
            scrollbarRectTransform.anchoredPosition = Vector2.zero;
            
            var scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
            
            scrollRect.verticalScrollbar = scrollbarComponent;
        }

        /// <summary>
        /// 创建所有标签页（已本地化）
        /// </summary>
        private void CreateAllTabs()
        {
            // 基础设置
            CreateTab("Basic", "FPC_SettingsTabBasic");
            if (tabContents.ContainsKey("Basic"))
            {
                Tabs.BasicTabBuilder.Build(
                    tabContents["Basic"].transform,
                    sliders, toggles, dropdowns, keybindButtons);
            }

            // 灵敏度设置
            CreateTab("Sensitivity", "FPC_SettingsTabSensitivity");
            if (tabContents.ContainsKey("Sensitivity"))
            {
                Tabs.SensitivityTabBuilder.Build(
                    tabContents["Sensitivity"].transform,
                    sliders, toggles, dropdowns, keybindButtons);
            }

            // 键位设置
            CreateTab("Keybinds", "FPC_SettingsTabKeybinds");
            if (tabContents.ContainsKey("Keybinds"))
            {
                Tabs.KeybindsTabBuilder.Build(
                    tabContents["Keybinds"].transform,
                    sliders, toggles, dropdowns, keybindButtons);
            }

            // 人物显示设置
            CreateTab("Display", "FPC_SettingsTabDisplay");
            if (tabContents.ContainsKey("Display"))
            {
                Tabs.DisplayTabBuilder.Build(
                    tabContents["Display"].transform,
                    sliders, toggles, dropdowns, keybindButtons);
            }
        }

        /// <summary>
        /// 创建第一人称设置标签页（旧方法，未使用）
        /// </summary>
        private void CreateFirstPersonTab()
        {
            CreateTab("FirstPerson", "第一人称设置");
            var tabContent = tabContents["FirstPerson"];
            
            // === 鼠标灵敏度设置 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "🎯 鼠标灵敏度");
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "水平灵敏度", OptionsUIConstants.SensitivityXKey, 
                OptionsUIConstants.SensitivityMin, OptionsUIConstants.SensitivityMax, OptionsUIConstants.SensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "垂直灵敏度", OptionsUIConstants.SensitivityYKey,
                OptionsUIConstants.SensitivityMin, OptionsUIConstants.SensitivityMax, OptionsUIConstants.SensitivityDefault, sliders);
            
            // === 倍镜灵敏度倍数设置（相对于普通灵敏度，1.0=100%，0.8=80%，同时应用于水平和垂直） ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "🔍 倍镜灵敏度倍数");
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "1.2x倍镜倍数", OptionsUIConstants.Scope1_2xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope1_2xSensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "2x倍镜倍数", OptionsUIConstants.Scope2xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope2xSensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "4x倍镜倍数", OptionsUIConstants.Scope4xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope4xSensitivityDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "8x倍镜倍数", OptionsUIConstants.Scope8xSensitivityKey,
                OptionsUIConstants.ScopeSensitivityMultiplierMin, OptionsUIConstants.ScopeSensitivityMultiplierMax, OptionsUIConstants.Scope8xSensitivityDefault, sliders);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 相机设置 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "📷 相机设置");
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "视野角度 (FOV)", OptionsUIConstants.FovKey,
                OptionsUIConstants.FovMin, OptionsUIConstants.FovMax, OptionsUIConstants.FovDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "高度偏移", OptionsUIConstants.OffsetHeightKey,
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetHeightDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "前向偏移", OptionsUIConstants.OffsetForwardKey,
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetForwardDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "右向偏移", OptionsUIConstants.OffsetRightKey,
                OptionsUIConstants.OffsetMin, OptionsUIConstants.OffsetMax, OptionsUIConstants.OffsetRightDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "防抖强度", OptionsUIConstants.AntiBobStrengthKey,
                0f, 1f, OptionsUIConstants.AntiBobDefault, sliders);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 战斗设置 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "⚔️ 战斗设置");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "启用后坐力", OptionsUIConstants.EnableRecoilKey, true, toggles);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "后坐力强度", OptionsUIConstants.RecoilStrengthKey,
                OptionsUIConstants.RecoilStrengthMin, OptionsUIConstants.RecoilStrengthMax, OptionsUIConstants.RecoilStrengthDefault, sliders);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "启用ADS武器位移", OptionsUIConstants.EnableAdsOffsetKey, true, toggles);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 受击特效 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "💥 受击特效");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "受击效果开关", OptionsUIConstants.HitFxEnableKey, true, toggles);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "受击后处理强度", OptionsUIConstants.HitFxPostStrengthKey,
                0f, 1f, OptionsUIConstants.HitFxPostDefault, sliders);
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "受击UI强度", OptionsUIConstants.HitFxUiStrengthKey,
                0f, 1f, OptionsUIConstants.HitFxUiDefault, sliders);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "受击方向指示", OptionsUIConstants.HitFxShowDirKey, true, toggles);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 性能设置 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "⚙️ 性能设置");
            StandaloneUICreator.CreateSliderRow(tabContent.transform, "渲染距离", OptionsUIConstants.RenderDistanceKey,
                OptionsUIConstants.RenderDistanceMin, OptionsUIConstants.RenderDistanceMax, OptionsUIConstants.RenderDistanceDefault, sliders);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 按键绑定 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "⌨️ 按键绑定");
            StandaloneUICreator.CreateKeybindRow(tabContent.transform, "切换视角按键", "FirstPersonCamera_ToggleKeyCode", KeyCode.F5, keybindButtons);
            StandaloneUICreator.CreateKeybindRow(tabContent.transform, "左侧头按键", "FirstPersonCamera_PeekLeftKeyCode", KeyCode.Q, keybindButtons);
            StandaloneUICreator.CreateKeybindRow(tabContent.transform, "右侧头按键", "FirstPersonCamera_PeekRightKeyCode", KeyCode.E, keybindButtons);
        }

        /// <summary>
        /// 创建显示设置标签页（旧方法，未使用）
        /// </summary>
        private void CreateDisplayTab()
        {
            CreateTab("Display", "FP人物显示设置");
            var tabContent = tabContents["Display"];
            
            // === 视觉效果 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "✨ 视觉效果");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "禁用景深/虚化", OptionsUIConstants.DisableBlurKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "禁用遮挡透视", OptionsUIConstants.DisableSeeThroughKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "禁用准星虚化", OptionsUIConstants.DisableAimOcclusionFadeKey, true, toggles);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 装备隐藏 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "🎩 装备隐藏");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏头盔", OptionsUIConstants.HideHelmetKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏口罩/面罩", OptionsUIConstants.HideFaceMaskKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏护甲", OptionsUIConstants.HideArmorKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏背包", OptionsUIConstants.HideBackpackKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏近战武器", OptionsUIConstants.HideMeleeKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏耳机/耳饰", OptionsUIConstants.HideHeadsetKey, false, toggles);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 身体部位隐藏 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "👤 身体部位");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏左手/手臂", OptionsUIConstants.HideLeftHandKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏右手/手臂", OptionsUIConstants.HideRightHandKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏脸部", OptionsUIConstants.HideFaceKey, true, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏头发", OptionsUIConstants.HideHairKey, true, toggles);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 细节部位隐藏 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "🔍 细节部位");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏眼睛部件", OptionsUIConstants.HideFaceEyesKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏眉毛部件", OptionsUIConstants.HideFaceEyebrowsKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏嘴部部件", OptionsUIConstants.HideFaceMouthKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏尾巴部件", OptionsUIConstants.HideFaceTailKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏脚部部件", OptionsUIConstants.HideFaceFeetKey, false, toggles);
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "隐藏翅膀部件", OptionsUIConstants.HideFaceWingsKey, false, toggles);
            StandaloneUICreator.CreateDivider(tabContent.transform);
            
            // === 武器显示 ===
            StandaloneUICreator.CreateSectionTitle(tabContent.transform, "🔫 武器显示");
            StandaloneUICreator.CreateToggleRow(tabContent.transform, "强制显示持握武器", OptionsUIConstants.ForceShowHeldWeaponKey, true, toggles);
        }

        /// <summary>
        /// 创建标签页（已本地化）
        /// </summary>
        private void CreateTab(string tabId, string tabNameKey)
        {
            // 创建标签按钮
            var tabButton = new GameObject($"TabButton_{tabId}");
            tabButton.transform.SetParent(tabButtonContainer.transform, false);
            
            var buttonRectTransform = tabButton.AddComponent<RectTransform>();
            buttonRectTransform.sizeDelta = new Vector2(180f, 42f); // 增加按钮大小
            
            var buttonImage = tabButton.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // 添加按钮颜色过渡
            var button = tabButton.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            colors.pressedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
            colors.selectedColor = new Color(0.4f, 0.5f, 0.7f, 1f);
            button.colors = colors;
            
            string currentTabId = tabId; // 捕获变量
            button.onClick.AddListener(() => ShowTab(currentTabId));
            
            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(tabButton.transform, false);
            
            var textRectTransform = buttonText.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.anchoredPosition = Vector2.zero;
            
            var text = buttonText.AddComponent<TextMeshProUGUI>();
            text.text = FPLocalization.Get(tabNameKey);
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            // 添加本地化组件
            var localized = buttonText.AddComponent<LocalizedText>();
            localized.SetKey(tabNameKey);
            
            // 创建标签页内容
            var tabContent = new GameObject($"TabContent_{tabId}");
            tabContent.transform.SetParent(contentContainer.transform, false);
            
            var contentRectTransform = tabContent.AddComponent<RectTransform>();
            contentRectTransform.anchorMin = new Vector2(0f, 1f);
            contentRectTransform.anchorMax = new Vector2(1f, 1f);
            contentRectTransform.pivot = new Vector2(0.5f, 1f);
            contentRectTransform.sizeDelta = new Vector2(0f, 0f);
            
            var verticalLayout = tabContent.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.spacing = 8f; // 增加间距
            verticalLayout.padding = new RectOffset(0, 0, 5, 5);
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            
            var contentSizeFitter = tabContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            tabContent.SetActive(false);
            tabContents[tabId] = tabContent;
        }

        /// <summary>
        /// 显示指定标签页
        /// </summary>
        private void ShowTab(string tabId)
        {
            if (!tabContents.ContainsKey(tabId)) return;
            
            // 隐藏所有标签页
            foreach (var kvp in tabContents)
            {
                kvp.Value.SetActive(false);
            }
            
            // 显示指定标签页
            tabContents[tabId].SetActive(true);
            currentTab = tabId;
            
            // 更新标签按钮颜色
            UpdateTabButtonColors();
        }

        /// <summary>
        /// 更新标签按钮颜色
        /// </summary>
        private void UpdateTabButtonColors()
        {
            for (int i = 0; i < tabButtonContainer.transform.childCount; i++)
            {
                var button = tabButtonContainer.transform.GetChild(i);
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    if (button.name.Contains(currentTab))
                    {
                        image.color = new Color(0.4f, 0.5f, 0.7f, 1f); // 选中颜色
                    }
                    else
                    {
                        image.color = new Color(0.3f, 0.3f, 0.3f, 1f); // 未选中颜色
                    }
                }
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 设置窗口可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (canvas == null)
            {
                FPLogger.LogError("Canvas 为空，无法设置可见性！");
                return;
            }

            // 确保 Canvas 和其父对象都激活
            if (visible)
            {
                // 先确保父对象激活
                if (transform != null && !transform.gameObject.activeSelf)
                {
                    transform.gameObject.SetActive(true);
                    FPLogger.Log("激活了设置窗口的父对象");
                }
                
                // 确保 Canvas 激活
                if (!canvas.gameObject.activeSelf)
                {
                    canvas.gameObject.SetActive(true);
                }
                
                // 确保窗口面板激活
                if (windowPanel != null && !windowPanel.activeSelf)
                {
                    windowPanel.SetActive(true);
                    FPLogger.Log("激活了窗口面板");
                }
                
                // 确保窗口在最上层
                canvas.sortingOrder = 10000;
                
                // 强制刷新Canvas
                canvas.enabled = false;
                canvas.enabled = true;
                
                // 打开时默认跳转到"基础设置"
                if (tabContents != null && tabContents.Count > 0)
                {
                    ShowTab("Basic");
                }
                
                FPLogger.Log("设置窗口已显示 - 父对象: {0}, Canvas: {1}, Canvas.enabled: {2}, 排序: {3}, 窗口面板: {4}", 
                    transform.gameObject.activeSelf, canvas.gameObject.activeSelf, canvas.enabled, canvas.sortingOrder, 
                    (windowPanel != null ? windowPanel.activeSelf.ToString() : "null"));
            }
            else
            {
                if (windowPanel != null)
                {
                    windowPanel.SetActive(false);
                }
                canvas.gameObject.SetActive(false);
            }
            
            // 不控制游戏暂停，保持原版设置界面打开
            // 只在原版设置界面中打开，不干扰游戏状态
        }

        /// <summary>
        /// 显示窗口
        /// </summary>
        public void Show()
        {
            SetVisible(true);
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// 切换窗口可见性
        /// </summary>
        public void Toggle()
        {
            if (canvas != null)
            {
                SetVisible(!canvas.gameObject.activeSelf);
            }
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible()
        {
            return canvas != null && canvas.gameObject.activeSelf;
        }

        /// <summary>
        /// 延迟保存所有配置（协程）
        /// 在UI初始化完成后，等待一帧确保所有配置项都已加载，然后保存所有配置
        /// </summary>
        private IEnumerator SaveAllConfigsDelayed()
        {
            // 等待一帧，确保所有UI组件都已初始化并加载了配置
            yield return null;
            
            // 再等待一小段时间，确保所有配置项都已通过Load方法加载
            yield return new WaitForSecondsRealtime(0.1f);
            
            // 保存所有配置项（包括默认值）
            ConfigManager.SaveAll();
            FPLogger.Log("UI初始化完成，已保存所有配置项（包括默认值）");
        }
        #endregion
    }
}