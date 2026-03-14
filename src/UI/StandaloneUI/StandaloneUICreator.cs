using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.UI;          // 新增
using System.Collections.Generic;

namespace FirstPersonCamera.StandaloneUI
{
    /// <summary>
    /// 独立UI创建器
    /// 用于创建各种UI组件
    /// </summary>
    public static class StandaloneUICreator
    {
        #region 常量
        private const float LabelFontSize = 22f;
        private const float LabelWidth = 280f;
        private const float SliderWidth = 380f;
        private const float ValueFieldWidth = 70f;
        private const float RowHeight = 38f;
        private const float ToggleSize = 35f;
        private const float SectionTitleFontSize = 20f;
        private const float SectionSpacing = 20f;
        private const float SliderHeight = 20f; // 滑块高度
        #endregion

        #region 创建方法
        /// <summary>
        /// 创建分组标题（本地化版本）
        /// </summary>
        public static GameObject CreateSectionTitle(Transform parent, string localizationKey)
        {
            var section = new GameObject("SectionTitle");
            section.transform.SetParent(parent, false);
            
            var rectTransform = section.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 30f);
            
            var text = section.AddComponent<TextMeshProUGUI>();
            text.text = FPLocalization.Get(localizationKey);
            text.fontSize = SectionTitleFontSize;
            text.color = new Color(0.7f, 0.8f, 1f, 1f);
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var localized = section.AddComponent<LocalizedText>();
            localized.SetKey(localizationKey);

            // 下划线
            var underline = new GameObject("Underline");
            underline.transform.SetParent(section.transform, false);
            var underlineRect = underline.AddComponent<RectTransform>();
            underlineRect.anchorMin = new Vector2(0f, 0f);
            underlineRect.anchorMax = new Vector2(1f, 0f);
            underlineRect.pivot = new Vector2(0.5f, 0.5f);
            underlineRect.sizeDelta = new Vector2(0f, 2f);
            underlineRect.anchoredPosition = new Vector2(0f, -2f);
            var underlineImage = underline.AddComponent<Image>();
            underlineImage.color = new Color(0.4f, 0.5f, 0.7f, 0.6f);
            
            return section;
        }

        /// <summary>
        /// 创建分隔线（不变）
        /// </summary>
        public static GameObject CreateDivider(Transform parent)
        {
            var divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            
            var rectTransform = divider.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 1f);
            
            var image = divider.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            
            return divider;
        }

        /// <summary>
        /// 创建滑块行（本地化版本）
        /// </summary>
        public static void CreateSliderRow(
            Transform parent,
            string localizationKey,
            string configKey,
            float minValue,
            float maxValue,
            float defaultValue,
            Dictionary<string, Slider> sliders)
        {
            var row = CreateRow(parent);
            
            // 创建标签（使用本地化键）
            var label = CreateLabel(row.transform, localizationKey);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(LabelWidth, RowHeight);
            
            // 创建滑块
            var slider = CreateSlider(row.transform, minValue, maxValue, defaultValue);
            var sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(SliderWidth, RowHeight);
            
            // 创建数值显示
            var valueField = CreateValueField(row.transform, defaultValue);
            var valueRect = valueField.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(ValueFieldWidth, RowHeight);
            
            // 绑定事件
            var sliderComponent = slider.GetComponent<Slider>();
            var valueText = valueField.GetComponent<TextMeshProUGUI>();
            
            sliderComponent.onValueChanged.AddListener((value) =>
            {
                valueText.text = value.ToString("F2");
                ConfigManager.Save<float>(configKey, value);
                
                // 立即应用设置变更
                OptionsHelper.ApplySettingChange(configKey);
            });
            
            // 加载初始值
            float loadedValue = OptionsHelper.LoadFloat(configKey, defaultValue);
            sliderComponent.value = loadedValue;
            valueText.text = loadedValue.ToString("F2");
            
            sliders[configKey] = sliderComponent;
        }

        /// <summary>
        /// 创建开关行（本地化版本）
        /// </summary>
        public static void CreateToggleRow(
            Transform parent,
            string localizationKey,
            string configKey,
            bool defaultValue,
            Dictionary<string, Toggle> toggles)
        {
            var row = CreateRow(parent);
            
            // 创建标签（使用本地化键）
            var label = CreateLabel(row.transform, localizationKey);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(LabelWidth, RowHeight);
            
            // 创建开关
            var toggle = CreateToggle(row.transform);
            // Toggle的尺寸已在CreateToggle中设置
            
            // 获取组件
            var toggleComponent = toggle.GetComponent<Toggle>();
            var toggleAnimator = toggle.GetComponent<ToggleAnimator>();
            
            // 先加载初始值
            bool loadedValue = OptionsHelper.LoadInt(configKey, defaultValue ? 1 : 0) == 1;
            
            // 绑定事件（在设置值之前，避免触发）
            toggleComponent.onValueChanged.AddListener((value) =>
            {
                // 更新视觉效果
                if (toggleAnimator != null)
                {
                    toggleAnimator.OnValueChanged(value);
                }
                
                // 保存配置
                ConfigManager.Save<int>(configKey, value ? 1 : 0);
                
                // 立即应用设置
                OptionsHelper.ApplyToggleSetting(configKey, value);
            });
            
            // 设置初始值
            toggleComponent.isOn = loadedValue;
            
            // 确保ToggleAnimator的视觉效果正确
            if (toggleAnimator != null)
            {
                toggleAnimator.OnValueChanged(loadedValue);
            }
            
            toggles[configKey] = toggleComponent;
        }

        /// <summary>
        /// 创建按键绑定行（不变，但注意它调用了CreateLabel，目前传入的是字符串而非本地化键）
        /// </summary>
        public static void CreateKeybindRow(
            Transform parent,
            string labelText,
            string configKey,
            KeyCode defaultValue,
            Dictionary<string, Button> keybindButtons)
        {
            var row = CreateRow(parent);
            
            // 创建标签（这里直接传入文本，但CreateLabel现在期望本地化键，因此会作为键去查找）
            // 如果希望正确本地化，请将labelText改为localizationKey
            var label = CreateLabel(row.transform, labelText);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(LabelWidth, RowHeight);
            
            // 创建按键按钮
            var button = CreateKeybindButton(row.transform, configKey, defaultValue);
            // Button的尺寸已在CreateKeybindButton中设置
            
            keybindButtons[configKey] = button.GetComponent<Button>();
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 创建行容器（不变）
        /// </summary>
        private static GameObject CreateRow(Transform parent)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            
            var rectTransform = row.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, RowHeight);
            // 确保anchor设置正确，让HorizontalLayoutGroup能够正确布局
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            
            var horizontalLayout = row.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.spacing = 15f; // 增加间距
            horizontalLayout.padding = new RectOffset(8, 8, 2, 2);
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            
            return row;
        }

        /// <summary>
        /// 创建标签（本地化版本）
        /// </summary>
        private static GameObject CreateLabel(Transform parent, string localizationKey)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, false);

            var rectTransform = label.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);

            var textComponent = label.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = LabelFontSize;
            textComponent.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.enableWordWrapping = true;  // 启用自动换行
            textComponent.text = FPLocalization.Get(localizationKey);

            // 移除固定的 LayoutElement，改为使用 ContentSizeFitter 让高度自适应
            var sizeFitter = label.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // 水平优先扩展（但受父容器限制）
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;   // 垂直自适应

            // 为保证布局不混乱，可设置最小宽度，但允许扩展
            var layout = label.AddComponent<LayoutElement>();
            layout.minWidth = LabelWidth;          // 最小宽度（原固定宽度）
            layout.preferredWidth = -1;             // 不限制首选宽度
            layout.flexibleWidth = 1;                // 允许水平扩展（如果父容器有剩余空间）

            return label;
        }

        /// <summary>
        /// 创建滑块（不变）
        /// </summary>
        private static GameObject CreateSlider(Transform parent, float min, float max, float defaultValue)
        {
            var slider = new GameObject("Slider");
            slider.transform.SetParent(parent, false);

            var rectTransform = slider.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(SliderWidth, RowHeight);

            // 为布局系统提供首选尺寸
            var sliderLayout = slider.AddComponent<LayoutElement>();
            sliderLayout.minWidth = SliderWidth;
            sliderLayout.preferredWidth = SliderWidth;
            sliderLayout.minHeight = RowHeight;
            sliderLayout.preferredHeight = RowHeight;
            sliderLayout.flexibleWidth = 0f;
            
            var sliderComponent = slider.AddComponent<Slider>();
            sliderComponent.minValue = min;
            sliderComponent.maxValue = max;
            sliderComponent.value = defaultValue;
            sliderComponent.wholeNumbers = false;
            sliderComponent.direction = Slider.Direction.LeftToRight;
            
            // 创建背景（轨道）
            var background = new GameObject("Background");
            background.transform.SetParent(slider.transform, false);
            
            var bgRectTransform = background.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = new Vector2(0f, 0.5f);
            bgRectTransform.anchorMax = new Vector2(1f, 0.5f);
            bgRectTransform.pivot = new Vector2(0.5f, 0.5f);
            bgRectTransform.sizeDelta = new Vector2(0f, SliderHeight);
            bgRectTransform.anchoredPosition = Vector2.zero;
            
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            sliderComponent.targetGraphic = bgImage;
            
            // 创建填充区域
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            
            var fillAreaRectTransform = fillArea.AddComponent<RectTransform>();
            fillAreaRectTransform.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRectTransform.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRectTransform.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRectTransform.sizeDelta = new Vector2(-40f, SliderHeight);
            fillAreaRectTransform.anchoredPosition = Vector2.zero;
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            var fillRectTransform = fill.AddComponent<RectTransform>();
            fillRectTransform.anchorMin = Vector2.zero;
            fillRectTransform.anchorMax = new Vector2(1f, 1f);
            fillRectTransform.sizeDelta = Vector2.zero;
            fillRectTransform.anchoredPosition = Vector2.zero;
            
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 0.9f, 1f);
            sliderComponent.fillRect = fillRectTransform;
            
            // 创建手柄滑动区域
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(slider.transform, false);
            
            var handleAreaRectTransform = handleArea.AddComponent<RectTransform>();
            handleAreaRectTransform.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRectTransform.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRectTransform.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRectTransform.sizeDelta = new Vector2(-40f, SliderHeight);
            handleAreaRectTransform.anchoredPosition = Vector2.zero;
            
            // 创建手柄
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            
            var handleRectTransform = handle.AddComponent<RectTransform>();
            handleRectTransform.sizeDelta = new Vector2(20f, 20f);
            handleRectTransform.anchoredPosition = Vector2.zero;
            
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // 添加手柄边框效果
            var handleBorder = new GameObject("Border");
            handleBorder.transform.SetParent(handle.transform, false);
            
            var borderRect = handleBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(16f, 16f);
            borderRect.anchoredPosition = Vector2.zero;
            
            var borderImage = handleBorder.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            
            sliderComponent.handleRect = handleRectTransform;
            
            return slider;
        }

        /// <summary>
        /// 创建数值显示字段（不变）
        /// </summary>
        private static GameObject CreateValueField(Transform parent, float defaultValue)
        {
            var valueField = new GameObject("ValueField");
            valueField.transform.SetParent(parent, false);

            var rectTransform = valueField.AddComponent<RectTransform>();

            // 为布局系统提供首选尺寸
            var valueLayout = valueField.AddComponent<LayoutElement>();
            valueLayout.minWidth = ValueFieldWidth;
            valueLayout.preferredWidth = ValueFieldWidth;
            valueLayout.minHeight = RowHeight;
            valueLayout.preferredHeight = RowHeight;
            valueLayout.flexibleWidth = 0f;
            
            // 添加背景
            var bg = new GameObject("Background");
            bg.transform.SetParent(valueField.transform, false);
            
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            
            var text = valueField.AddComponent<TextMeshProUGUI>();
            text.text = defaultValue.ToString("F2");
            text.fontSize = LabelFontSize;
            text.color = new Color(0.9f, 0.9f, 1f, 1f);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.margin = new Vector4(5f, 0f, 5f, 0f);
            
            return valueField;
        }

        /// <summary>
        /// 创建开关（不变）
        /// </summary>
        private static GameObject CreateToggle(Transform parent)
        {
            var toggle = new GameObject("Toggle");
            toggle.transform.SetParent(parent, false);

            var rectTransform = toggle.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = new Vector2(ToggleSize, ToggleSize);

            var toggleLayout = toggle.AddComponent<LayoutElement>();
            toggleLayout.minWidth = ToggleSize;
            toggleLayout.preferredWidth = ToggleSize;
            toggleLayout.minHeight = RowHeight;
            toggleLayout.preferredHeight = RowHeight;
            toggleLayout.flexibleWidth = 0f;
            
            var toggleComponent = toggle.AddComponent<Toggle>();
            toggleComponent.isOn = false;
            
            // 创建背景
            var background = new GameObject("Background");
            background.transform.SetParent(toggle.transform, false);
            
            var bgRectTransform = background.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = Vector2.zero;
            bgRectTransform.anchorMax = Vector2.one;
            bgRectTransform.sizeDelta = Vector2.zero;
            bgRectTransform.anchoredPosition = Vector2.zero;
            
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            bgImage.raycastTarget = true;
            toggleComponent.targetGraphic = bgImage;
            
            // 创建勾选标记
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            
            var checkmarkRectTransform = checkmark.AddComponent<RectTransform>();
            checkmarkRectTransform.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRectTransform.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRectTransform.sizeDelta = Vector2.zero;
            checkmarkRectTransform.anchoredPosition = Vector2.zero;
            
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.1f, 0.8f, 0.2f, 1f);
            checkmarkImage.enabled = false;
            checkmarkImage.raycastTarget = false;
            
            toggleComponent.graphic = checkmarkImage;
            
            // 添加动画组件
            var toggleAnimator = toggle.AddComponent<ToggleAnimator>();
            toggleAnimator.Initialize(toggleComponent, bgImage, checkmarkImage);
            
            return toggle;
        }

        /// <summary>
        /// 创建按键绑定按钮（不变）
        /// </summary>
        private static GameObject CreateKeybindButton(Transform parent, string configKey, KeyCode defaultValue)
        {
            var button = new GameObject("KeybindButton");
            button.transform.SetParent(parent, false);

            var rectTransform = button.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = new Vector2(180f, RowHeight);

            var keyLayout = button.AddComponent<LayoutElement>();
            keyLayout.minWidth = 180f;
            keyLayout.preferredWidth = 180f;
            keyLayout.minHeight = RowHeight;
            keyLayout.preferredHeight = RowHeight;
            keyLayout.flexibleWidth = 0f;
            
            var bgImage = button.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            bgImage.raycastTarget = true;
            
            var buttonComponent = button.AddComponent<Button>();
            buttonComponent.targetGraphic = bgImage;
            
            var colors = buttonComponent.colors;
            colors.normalColor = new Color(0.2f, 0.25f, 0.35f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.35f, 0.45f, 1f);
            colors.pressedColor = new Color(0.15f, 0.2f, 0.3f, 1f);
            colors.selectedColor = new Color(0.25f, 0.3f, 0.4f, 1f);
            colors.colorMultiplier = 1f;
            buttonComponent.colors = colors;
            
            var text = new GameObject("Text");
            text.transform.SetParent(button.transform, false);
            
            var textRectTransform = text.AddComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0f, 0f);
            textRectTransform.anchorMax = new Vector2(1f, 1f);
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.anchoredPosition = Vector2.zero;
            textRectTransform.SetAsLastSibling();
            
            var textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 20f;
            textComponent.color = new Color(1f, 1f, 1f, 1f);
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.raycastTarget = false;
            textComponent.fontStyle = FontStyles.Bold;
            
            KeyCode loadedKey = OptionsHelper.LoadKeyCode(configKey, defaultValue);
            textComponent.text = loadedKey.ToString();
            
            var rebinder = button.AddComponent<StandaloneKeyRebinder>();
            rebinder.Initialize(configKey, textComponent, buttonComponent, defaultValue);
            
            return button;
        }
        #endregion
    }

    /// <summary>
    /// 开关动画组件（不变）
    /// </summary>
    public class ToggleAnimator : MonoBehaviour
    {
        private Toggle toggle;
        private Image background;
        private Image checkmark;
        private bool isAnimating = false;
        private float animationTime = 0.15f;
        private float elapsedTime = 0f;
        private Color startBgColor;
        private Color endBgColor;
        private bool targetValue;

        public void Initialize(Toggle t, Image bg, Image ck)
        {
            toggle = t;
            background = bg;
            checkmark = ck;
            
            // 立即更新初始状态
            if (toggle != null && checkmark != null)
            {
                UpdateVisuals(toggle.isOn, false);
            }
        }
        
        public void OnValueChanged(bool value)
        {
            if (toggle == null || background == null || checkmark == null) return;
            
            targetValue = value;
            UpdateVisuals(value, true);
        }

        private void UpdateVisuals(bool isOn, bool animate)
        {
            if (background == null || checkmark == null) return;
            
            if (animate)
            {
                isAnimating = true;
                elapsedTime = 0f;
                startBgColor = background.color;
                endBgColor = isOn ? new Color(0.1f, 0.6f, 0.25f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            else
            {
                background.color = isOn ? new Color(0.1f, 0.6f, 0.25f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
                if (checkmark != null)
                {
                    checkmark.enabled = isOn;
                    checkmark.color = new Color(0.1f, 0.8f, 0.2f, isOn ? 1f : 0f);
                }
            }
        }

        private void Update()
        {
            if (!isAnimating || background == null || checkmark == null) return;
            
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationTime);
            float smoothT = t * t * (3f - 2f * t);
            
            background.color = Color.Lerp(startBgColor, endBgColor, smoothT);
            
            if (checkmark != null)
            {
                checkmark.enabled = targetValue;
                checkmark.color = new Color(0.1f, 0.8f, 0.2f, targetValue ? smoothT : (1f - smoothT));
            }
            
            if (t >= 1f)
            {
                isAnimating = false;
                if (checkmark != null)
                {
                    checkmark.enabled = targetValue;
                    checkmark.color = new Color(0.1f, 0.8f, 0.2f, targetValue ? 1f : 0f);
                }
            }
        }
    }
}