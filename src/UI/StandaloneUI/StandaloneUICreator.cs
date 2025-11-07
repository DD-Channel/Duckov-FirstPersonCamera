using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.Utilities;
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
        /// 创建分组标题
        /// </summary>
        public static GameObject CreateSectionTitle(Transform parent, string title)
        {
            var section = new GameObject("SectionTitle");
            section.transform.SetParent(parent, false);
            
            var rectTransform = section.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 30f);
            
            var text = section.AddComponent<TextMeshProUGUI>();
            text.text = title;
            text.fontSize = SectionTitleFontSize;
            text.color = new Color(0.7f, 0.8f, 1f, 1f); // 浅蓝色
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            
            // 添加下划线
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
        /// 创建分隔线
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
        /// 创建滑块行
        /// </summary>
        public static void CreateSliderRow(
            Transform parent,
            string labelText,
            string configKey,
            float minValue,
            float maxValue,
            float defaultValue,
            Dictionary<string, Slider> sliders)
        {
            var row = CreateRow(parent);
            
            // 创建标签
            var label = CreateLabel(row.transform, labelText);
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
        /// 创建开关行
        /// </summary>
        public static void CreateToggleRow(
            Transform parent,
            string labelText,
            string configKey,
            bool defaultValue,
            Dictionary<string, Toggle> toggles)
        {
            var row = CreateRow(parent);
            
            // 创建标签
            var label = CreateLabel(row.transform, labelText);
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
            
            // 先绑定事件（在设置值之前，避免触发）
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
            
            // 然后设置初始值（这会触发上面的监听器，但此时ToggleAnimator已经初始化）
            toggleComponent.isOn = loadedValue;
            
            // 确保ToggleAnimator的视觉效果正确（如果上面的设置没有触发）
            if (toggleAnimator != null)
            {
                toggleAnimator.OnValueChanged(loadedValue);
            }
            
            toggles[configKey] = toggleComponent;
        }

        /// <summary>
        /// 创建按键绑定行
        /// </summary>
        public static void CreateKeybindRow(
            Transform parent,
            string labelText,
            string configKey,
            KeyCode defaultValue,
            Dictionary<string, Button> keybindButtons)
        {
            var row = CreateRow(parent);
            
            // 创建标签
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
        /// 创建行容器
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
        /// 创建标签
        /// </summary>
        private static GameObject CreateLabel(Transform parent, string text)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, false);

            var rectTransform = label.AddComponent<RectTransform>();
            // 确保anchor设置正确
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);

            var textComponent = label.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = LabelFontSize;
            textComponent.color = new Color(0.95f, 0.95f, 0.95f, 1f); // 接近白色
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.raycastTarget = false; // 标签不阻挡点击
            // TextMeshProUGUI会自动使用默认字体，不需要手动设置

            // 关键：为布局系统提供固定的首选尺寸，避免被布局压缩为0宽
            var layout = label.AddComponent<LayoutElement>();
            layout.minWidth = LabelWidth;
            layout.preferredWidth = LabelWidth;
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 0f;

            return label;
        }

        /// <summary>
        /// 创建滑块
        /// </summary>
        private static GameObject CreateSlider(Transform parent, float min, float max, float defaultValue)
        {
            var slider = new GameObject("Slider");
            slider.transform.SetParent(parent, false);

            var rectTransform = slider.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(SliderWidth, RowHeight); // 设置明确的尺寸

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
            
            // 创建背景（轨道）- 必须可见
            var background = new GameObject("Background");
            background.transform.SetParent(slider.transform, false);
            
            var bgRectTransform = background.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = new Vector2(0f, 0.5f);
            bgRectTransform.anchorMax = new Vector2(1f, 0.5f);
            bgRectTransform.pivot = new Vector2(0.5f, 0.5f);
            bgRectTransform.sizeDelta = new Vector2(0f, SliderHeight);
            bgRectTransform.anchoredPosition = Vector2.zero;
            
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.35f, 1f); // 更亮的背景色，确保可见
            sliderComponent.targetGraphic = bgImage;
            
            // 创建填充区域
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            
            var fillAreaRectTransform = fillArea.AddComponent<RectTransform>();
            fillAreaRectTransform.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRectTransform.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRectTransform.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRectTransform.sizeDelta = new Vector2(-40f, SliderHeight); // 为手柄留出空间
            fillAreaRectTransform.anchoredPosition = Vector2.zero;
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            var fillRectTransform = fill.AddComponent<RectTransform>();
            fillRectTransform.anchorMin = Vector2.zero;
            fillRectTransform.anchorMax = new Vector2(1f, 1f);
            fillRectTransform.sizeDelta = Vector2.zero;
            fillRectTransform.anchoredPosition = Vector2.zero;
            
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 0.9f, 1f); // 蓝色填充
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
            
            // 添加手柄边框效果（内部阴影）
            var handleBorder = new GameObject("Border");
            handleBorder.transform.SetParent(handle.transform, false);
            
            var borderRect = handleBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(16f, 16f);
            borderRect.anchoredPosition = Vector2.zero;
            
            var borderImage = handleBorder.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.4f, 0.45f, 1f); // 浅灰色边框
            
            sliderComponent.handleRect = handleRectTransform;
            
            return slider;
        }

        /// <summary>
        /// 创建数值显示字段
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
            text.color = new Color(0.9f, 0.9f, 1f, 1f); // 浅蓝色文字
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.margin = new Vector4(5f, 0f, 5f, 0f);
            
            return valueField;
        }

        /// <summary>
        /// 创建开关（完全重新设计，确保可见）
        /// </summary>
        private static GameObject CreateToggle(Transform parent)
        {
            var toggle = new GameObject("Toggle");
            toggle.transform.SetParent(parent, false);

            var rectTransform = toggle.AddComponent<RectTransform>();
            // 对于HorizontalLayoutGroup的子元素，使用left-center anchor
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            // 设置明确的尺寸，让HorizontalLayoutGroup自动排列
            rectTransform.sizeDelta = new Vector2(ToggleSize, ToggleSize);

            // 关键：为布局系统提供固定的首选尺寸，避免被压缩为0宽导致“看不见”
            var toggleLayout = toggle.AddComponent<LayoutElement>();
            toggleLayout.minWidth = ToggleSize;
            toggleLayout.preferredWidth = ToggleSize;
            toggleLayout.minHeight = RowHeight;
            toggleLayout.preferredHeight = RowHeight;
            toggleLayout.flexibleWidth = 0f;
            
            var toggleComponent = toggle.AddComponent<Toggle>();
            toggleComponent.isOn = false;
            
            // 创建背景（方框）- 使用更明显的颜色和边框
            var background = new GameObject("Background");
            background.transform.SetParent(toggle.transform, false);
            
            var bgRectTransform = background.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = Vector2.zero;
            bgRectTransform.anchorMax = Vector2.one;
            bgRectTransform.sizeDelta = Vector2.zero;
            bgRectTransform.anchoredPosition = Vector2.zero;
            
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.25f, 0.25f, 0.25f, 1f); // 深灰色背景
            bgImage.raycastTarget = true; // 确保可以接收射线检测
            toggleComponent.targetGraphic = bgImage;
            
            // 添加边框效果 - 使用Outline组件（如果可用）或简单的颜色对比
            // 为了简单，我们直接使用背景颜色的对比来创建边框效果
            // 通过调整背景颜色本身来创建视觉边框
            
            // 创建勾选标记（使用填充方块，更明显）
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            
            var checkmarkRectTransform = checkmark.AddComponent<RectTransform>();
            checkmarkRectTransform.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRectTransform.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRectTransform.sizeDelta = Vector2.zero;
            checkmarkRectTransform.anchoredPosition = Vector2.zero;
            
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.1f, 0.8f, 0.2f, 1f); // 亮绿色，确保可见
            checkmarkImage.enabled = false; // 初始隐藏，由Toggle控制
            checkmarkImage.raycastTarget = false; // 不阻挡点击
            
            // 关键：将checkmark设置为Toggle的graphic，Unity会自动控制显示/隐藏
            toggleComponent.graphic = checkmarkImage;
            
            // 添加动画组件
            var toggleAnimator = toggle.AddComponent<ToggleAnimator>();
            toggleAnimator.Initialize(toggleComponent, bgImage, checkmarkImage);
            
            return toggle;
        }

        /// <summary>
        /// 创建按键绑定按钮（完全重新设计，确保文本可见）
        /// </summary>
        private static GameObject CreateKeybindButton(Transform parent, string configKey, KeyCode defaultValue)
        {
            var button = new GameObject("KeybindButton");
            button.transform.SetParent(parent, false);

            var rectTransform = button.AddComponent<RectTransform>();
            // 对于HorizontalLayoutGroup的子元素，使用left-center anchor
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            // 设置明确的尺寸，让HorizontalLayoutGroup自动排列
            rectTransform.sizeDelta = new Vector2(180f, RowHeight);

            // 为布局系统提供首选尺寸，确保不会被压缩
            var keyLayout = button.AddComponent<LayoutElement>();
            keyLayout.minWidth = 180f;
            keyLayout.preferredWidth = 180f;
            keyLayout.minHeight = RowHeight;
            keyLayout.preferredHeight = RowHeight;
            keyLayout.flexibleWidth = 0f;
            
            // 创建背景图片 - 使用更明显的颜色和边框
            var bgImage = button.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.25f, 0.35f, 1f); // 深蓝灰色背景
            bgImage.raycastTarget = true; // 确保可以接收射线检测
            
            // 边框效果通过背景颜色对比来实现，不需要单独的边框对象
            
            var buttonComponent = button.AddComponent<Button>();
            buttonComponent.targetGraphic = bgImage;
            
            // 添加颜色过渡
            var colors = buttonComponent.colors;
            colors.normalColor = new Color(0.2f, 0.25f, 0.35f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.35f, 0.45f, 1f);
            colors.pressedColor = new Color(0.15f, 0.2f, 0.3f, 1f);
            colors.selectedColor = new Color(0.25f, 0.3f, 0.4f, 1f);
            colors.colorMultiplier = 1f;
            buttonComponent.colors = colors;
            
            // 创建文本 - 确保在背景之上，使用高对比度颜色
            var text = new GameObject("Text");
            text.transform.SetParent(button.transform, false);
            
            var textRectTransform = text.AddComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0f, 0f);
            textRectTransform.anchorMax = new Vector2(1f, 1f);
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.anchoredPosition = Vector2.zero;
            // 确保文本在边框之上
            textRectTransform.SetAsLastSibling();
            
            var textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 20f; // 稍微小一点，确保完整显示
            textComponent.color = new Color(1f, 1f, 1f, 1f); // 纯白色，高对比度
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.raycastTarget = false; // 文本不阻挡点击
            textComponent.fontStyle = FontStyles.Bold; // 加粗，更明显
            
            // 加载初始值
            KeyCode loadedKey = OptionsHelper.LoadKeyCode(configKey, defaultValue);
            textComponent.text = loadedKey.ToString();
            
            // 添加按键重绑定组件
            var rebinder = button.AddComponent<StandaloneKeyRebinder>();
            rebinder.Initialize(configKey, textComponent, buttonComponent, defaultValue);
            
            return button;
        }
        #endregion
    }

    /// <summary>
    /// 开关动画组件（优化版，确保视觉效果正确）
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
            
            // 立即更新初始状态，不等待动画
            if (toggle != null && checkmark != null)
            {
                UpdateVisuals(toggle.isOn, false);
            }
        }
        
        /// <summary>
        /// 手动触发视觉更新（由外部调用）
        /// </summary>
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
                // 开启时：绿色背景；关闭时：深灰色背景
                endBgColor = isOn ? new Color(0.1f, 0.6f, 0.25f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            else
            {
                // 立即设置，不动画
                background.color = isOn ? new Color(0.1f, 0.6f, 0.25f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
                // checkmark的显示由Toggle的graphic属性自动控制，但我们也可以手动设置
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
            
            // 平滑插值
            float smoothT = t * t * (3f - 2f * t); // Smoothstep
            
            background.color = Color.Lerp(startBgColor, endBgColor, smoothT);
            
            // checkmark的显示由Toggle控制，但我们确保颜色正确
            if (checkmark != null)
            {
                checkmark.enabled = targetValue;
                checkmark.color = new Color(0.1f, 0.8f, 0.2f, targetValue ? smoothT : (1f - smoothT));
            }
            
            if (t >= 1f)
            {
                isAnimating = false;
                // 确保最终状态正确
                if (checkmark != null)
                {
                    checkmark.enabled = targetValue;
                    checkmark.color = new Color(0.1f, 0.8f, 0.2f, targetValue ? 1f : 0f);
                }
            }
        }
    }
}
