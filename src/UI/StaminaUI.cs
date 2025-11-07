using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 体力UI模块
    /// 负责在第一人称模式下显示体力条UI，
    /// 提供清晰的体力值可视化反馈
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// Canvas排序顺序：低于指南针，高于HUD
        /// </summary>
        private const int StaminaCanvasSortOrder = 32000;
        
        /// <summary>
        /// Canvas参考分辨率宽度
        /// </summary>
        private const float CanvasReferenceWidth = 1920f;
        
        /// <summary>
        /// Canvas参考分辨率高度
        /// </summary>
        private const float CanvasReferenceHeight = 1080f;
        
        /// <summary>
        /// Canvas缩放匹配宽度或高度的比例（0.5表示平衡）
        /// </summary>
        private const float CanvasScaleMatch = 0.5f;
        
        /// <summary>
        /// 体力条根节点Y位置（位于健康条上方，往上移动40像素）
        /// </summary>
        private const float StaminaRootY = 170f;
        
        /// <summary>
        /// 体力条宽度
        /// </summary>
        private const float StaminaBarWidth = 320f;
        
        /// <summary>
        /// 体力条高度
        /// </summary>
        private const float StaminaBarHeight = 16f;
        
        /// <summary>
        /// 填充边距
        /// </summary>
        private const float FillPadding = 2f;
        
        /// <summary>
        /// 标签Y偏移量
        /// </summary>
        private const float StaminaLabelYOffset = 18f;
        
        /// <summary>
        /// 标签宽度
        /// </summary>
        private const float StaminaLabelWidth = 60f;
        
        /// <summary>
        /// 标签高度
        /// </summary>
        private const float StaminaLabelHeight = 20f;
        
        /// <summary>
        /// 标签字体大小
        /// </summary>
        private const float StaminaLabelFontSize = 14f;
        
        /// <summary>
        /// 背景透明度
        /// </summary>
        private const float StaminaBackgroundAlpha = 0.35f;
        
        /// <summary>
        /// 标签透明度
        /// </summary>
        private const float StaminaLabelAlpha = 0.8f;
        
        /// <summary>
        /// 高体力阈值（绿色）
        /// </summary>
        private const float HighStaminaThreshold = 0.6f;
        
        /// <summary>
        /// 中等体力阈值（黄色）
        /// </summary>
        private const float MediumStaminaThreshold = 0.3f;
        
        /// <summary>
        /// 高体力颜色（绿色）
        /// </summary>
        private static readonly Color HighStaminaColor = new Color(0.25f, 0.9f, 0.35f, 0.95f);
        
        /// <summary>
        /// 中等体力颜色（黄色）
        /// </summary>
        private static readonly Color MediumStaminaColor = new Color(1f, 0.85f, 0.25f, 0.95f);
        
        /// <summary>
        /// 低体力颜色（红色）
        /// </summary>
        private static readonly Color LowStaminaColor = new Color(1f, 0.35f, 0.25f, 0.95f);
        
        /// <summary>
        /// 背景颜色（黑色半透明）
        /// </summary>
            private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, StaminaBackgroundAlpha);
        
        /// <summary>
        /// 标签颜色（白色半透明）
        /// </summary>
        private static readonly Color LabelColor = new Color(1f, 1f, 1f, StaminaLabelAlpha);
        
        /// <summary>
        /// 反射字段名：currentStamina（用于获取当前体力值）
        /// </summary>
        private const string ReflectionFieldNameCurrentStamina = "currentStamina";
        
        /// <summary>
        /// 最小体力值阈值（用于避免除零）
        /// </summary>
        private const float MinStaminaThreshold = 0.0001f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 体力UI Canvas组件
        /// </summary>
        private Canvas staminaCanvas;
        
        /// <summary>
        /// 体力条根节点RectTransform
        /// </summary>
        private RectTransform staminaRoot;
        
        /// <summary>
        /// 体力条背景图像
        /// </summary>
        private Image staminaBg;
        
        /// <summary>
        /// 体力条填充图像
        /// </summary>
        private Image staminaFill;
        
        /// <summary>
        /// 体力条标签文本
        /// </summary>
        private TextMeshProUGUI staminaLabel;
        #endregion

        #region UI初始化方法
        /// <summary>
        /// 确保体力UI已初始化
        /// 如果尚未初始化，则创建Canvas和所有UI元素
        /// </summary>
        private void EnsureStaminaUI()
        {
            if (staminaCanvas != null) return;

            // 创建Canvas
            CreateStaminaCanvas();

            // 创建体力条根节点
            CreateStaminaRoot();

            // 创建背景
            CreateStaminaBackground();

            // 创建填充条
            CreateStaminaFill();

            // 创建标签
            CreateStaminaLabel();

            // 根据第一人称模式状态设置Canvas激活状态
            staminaCanvas.gameObject.SetActive(isFirstPersonMode);
        }

        /// <summary>
        /// 创建体力Canvas
        /// </summary>
        private void CreateStaminaCanvas()
        {
            var canvasGO = new GameObject("FPC_StaminaCanvas");
            Object.DontDestroyOnLoad(canvasGO);
            
            staminaCanvas = canvasGO.AddComponent<Canvas>();
            staminaCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            staminaCanvas.overrideSorting = true;
            staminaCanvas.sortingOrder = StaminaCanvasSortOrder;
            
            // 添加CanvasScaler以支持不同分辨率
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasReferenceWidth, CanvasReferenceHeight);
            scaler.matchWidthOrHeight = CanvasScaleMatch;
            
            // 添加GraphicRaycaster但禁用以避免阻挡输入
            var graphicRaycaster = canvasGO.AddComponent<GraphicRaycaster>();
            graphicRaycaster.enabled = false;
        }

        /// <summary>
        /// 创建体力条根节点
        /// </summary>
        private void CreateStaminaRoot()
        {
            var rootGO = new GameObject("StaminaRoot");
            rootGO.transform.SetParent(staminaCanvas.transform, false);
            
            staminaRoot = rootGO.AddComponent<RectTransform>();
            staminaRoot.anchorMin = new Vector2(0.5f, 0f);
            staminaRoot.anchorMax = new Vector2(0.5f, 0f);
            staminaRoot.pivot = new Vector2(0.5f, 0f);
            staminaRoot.anchoredPosition = new Vector2(0f, StaminaRootY);
            staminaRoot.sizeDelta = new Vector2(StaminaBarWidth, StaminaBarHeight);
        }

        /// <summary>
        /// 创建体力条背景
        /// </summary>
        private void CreateStaminaBackground()
        {
            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(staminaRoot, false);
            
            staminaBg = bgGO.AddComponent<Image>();
            staminaBg.sprite = whiteSprite != null 
                ? whiteSprite 
                : Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            staminaBg.type = Image.Type.Sliced;
            staminaBg.color = BackgroundColor;
            
            var bgRectTransform = staminaBg.rectTransform;
            bgRectTransform.anchorMin = new Vector2(0f, 0f);
            bgRectTransform.anchorMax = new Vector2(1f, 1f);
            bgRectTransform.offsetMin = Vector2.zero;
            bgRectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 创建体力条填充
        /// </summary>
        private void CreateStaminaFill()
        {
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(staminaRoot, false);
            
            staminaFill = fillGO.AddComponent<Image>();
            staminaFill.sprite = whiteSprite != null 
                ? whiteSprite 
                : Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            staminaFill.type = Image.Type.Filled;
            staminaFill.fillMethod = Image.FillMethod.Horizontal;
            staminaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            staminaFill.color = HighStaminaColor;
            
            var fillRectTransform = staminaFill.rectTransform;
            fillRectTransform.anchorMin = new Vector2(0f, 0f);
            fillRectTransform.anchorMax = new Vector2(1f, 1f);
            fillRectTransform.offsetMin = new Vector2(FillPadding, FillPadding);
            fillRectTransform.offsetMax = new Vector2(-FillPadding, -FillPadding);
        }

        /// <summary>
        /// 创建体力条标签（已禁用，不显示"体力"文字）
        /// </summary>
        private void CreateStaminaLabel()
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(staminaRoot, false);
            
            staminaLabel = labelGO.AddComponent<TextMeshProUGUI>();
            staminaLabel.text = ""; // 不显示文字
            staminaLabel.color = LabelColor;
            staminaLabel.fontSize = StaminaLabelFontSize;
            staminaLabel.alignment = TextAlignmentOptions.Center;
            
            var labelRectTransform = staminaLabel.rectTransform;
            labelRectTransform.anchorMin = new Vector2(0.5f, 1f);
            labelRectTransform.anchorMax = new Vector2(0.5f, 1f);
            labelRectTransform.pivot = new Vector2(0.5f, 0f);
            labelRectTransform.anchoredPosition = new Vector2(0f, StaminaLabelYOffset);
            labelRectTransform.sizeDelta = new Vector2(StaminaLabelWidth, StaminaLabelHeight);
            
            // 隐藏标签
            labelGO.SetActive(false);
        }
        #endregion

        #region UI更新方法
        /// <summary>
        /// 延迟更新体力条（在LateUpdate中调用）
        /// 更新体力条的填充量和颜色
        /// </summary>
        private void LateUpdateStaminaBar()
        {
            // 仅在第一人称模式下显示
            if (!isFirstPersonMode)
            {
                if (staminaCanvas != null && staminaCanvas.gameObject.activeSelf)
                {
                    staminaCanvas.gameObject.SetActive(false);
                }
                return;
            }

            // 确保UI已初始化
            EnsureStaminaUI();
            
            // 确保Canvas处于激活状态
            if (staminaCanvas != null && !staminaCanvas.gameObject.activeSelf)
            {
                staminaCanvas.gameObject.SetActive(true);
            }

            // 获取当前体力值
            float currentStamina, maxStamina;
            if (!TryGetStamina(out currentStamina, out maxStamina))
            {
                return;
            }

            // 计算体力百分比
            float staminaPercentage = maxStamina > MinStaminaThreshold
                ? Mathf.Clamp01(currentStamina / maxStamina)
                : 0f;

            // 在满体力时隐藏体力条
            if (staminaPercentage >= 1.0f)
            {
                if (staminaRoot != null && staminaRoot.gameObject.activeSelf)
                {
                    staminaRoot.gameObject.SetActive(false);
                }
                return;
            }
            
            // 体力不满时显示体力条
            if (staminaRoot != null && !staminaRoot.gameObject.activeSelf)
            {
                staminaRoot.gameObject.SetActive(true);
            }

            // 更新填充条
            if (staminaFill != null)
            {
                staminaFill.fillAmount = staminaPercentage;
                
                // 根据体力百分比设置颜色：绿色 -> 黄色 -> 红色
                Color fillColor = GetStaminaColor(staminaPercentage);
                staminaFill.color = fillColor;
            }
        }

        /// <summary>
        /// 根据体力百分比获取对应的颜色
        /// </summary>
        /// <param name="percentage">体力百分比（0.0-1.0）</param>
        /// <returns>对应的颜色</returns>
        private Color GetStaminaColor(float percentage)
        {
            if (percentage > HighStaminaThreshold)
            {
                return HighStaminaColor;
            }
            else if (percentage > MediumStaminaThreshold)
            {
                return MediumStaminaColor;
            }
            else
            {
                return LowStaminaColor;
            }
        }
        #endregion

        #region 体力值获取方法
        /// <summary>
        /// 尝试获取角色当前体力值
        /// 优先使用公共属性，如果不可用则使用反射访问私有字段
        /// </summary>
        /// <param name="current">当前体力值（输出）</param>
        /// <param name="max">最大体力值（输出）</param>
        /// <returns>如果成功获取则返回true，否则返回false</returns>
        private bool TryGetStamina(out float current, out float max)
        {
            current = 0f;
            max = 0f;

            try
            {
                // 获取主角色引用
                var character = mainCharacter != null ? mainCharacter : CharacterMainControl.Main;
                if (character == null)
                {
                    return false;
                }

                // 优先尝试使用公共属性
                if (TryGetStaminaFromProperties(character, out current, out max))
                {
                    return true;
                }

                // 后备方案：使用反射访问私有字段（用于DeltaEscape buff等场景）
                if (TryGetStaminaFromReflection(character, out current, out max))
                {
                    return true;
                }
            }
            catch
            {
                // 获取失败，静默处理
            }

            return false;
        }

        /// <summary>
        /// 尝试从公共属性获取体力值
        /// </summary>
        /// <param name="character">角色控制对象</param>
        /// <param name="current">当前体力值（输出）</param>
        /// <param name="max">最大体力值（输出）</param>
        /// <returns>如果成功获取则返回true，否则返回false</returns>
        private bool TryGetStaminaFromProperties(CharacterMainControl character, out float current, out float max)
        {
            current = 0f;
            max = 0f;

            try
            {
                current = character.CurrentStamina;
                max = character.MaxStamina;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试从反射字段获取体力值
        /// </summary>
        /// <param name="character">角色控制对象</param>
        /// <param name="current">当前体力值（输出）</param>
        /// <param name="max">最大体力值（输出）</param>
        /// <returns>如果成功获取则返回true，否则返回false</returns>
        private bool TryGetStaminaFromReflection(CharacterMainControl character, out float current, out float max)
        {
            current = 0f;
            max = 0f;

            try
            {
                var staminaField = typeof(CharacterMainControl).GetField(
                    ReflectionFieldNameCurrentStamina,
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (staminaField != null)
                {
                    current = (float)staminaField.GetValue(character);
                    max = character.MaxStamina;
                    return true;
                }
            }
            catch
            {
                // 反射访问失败，静默处理
            }

            return false;
        }
        #endregion
    }
}
