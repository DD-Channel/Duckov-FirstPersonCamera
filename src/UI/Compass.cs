using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 指南针UI模块
    /// 负责在第一人称模式下显示指南针，显示当前朝向和方向
    /// 使用双段无缝循环显示，确保指南针始终平滑滚动
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 每度的像素数（用于计算刻度位置）
        /// </summary>
        private const float PixelsPerDegree = 8f;
        
        /// <summary>
        /// 每个段落的宽度（像素）= 360度 * 每度像素数
        /// </summary>
        private float segmentWidthPx => 360f * PixelsPerDegree;
        
        /// <summary>
        /// Canvas排序顺序（确保在HUD之上显示）
        /// </summary>
        private const int CompassCanvasSortOrder = 32760;
        
        /// <summary>
        /// Canvas参考分辨率（用于缩放）
        /// </summary>
        private static readonly Vector2 CanvasReferenceResolution = new Vector2(1920, 1080);
        
        /// <summary>
        /// Canvas缩放匹配宽度或高度的比例（0.5表示居中）
        /// </summary>
        private const float CompassCanvasScaleMatch = 0.5f;
        
        /// <summary>
        /// 视口高度（像素）
        /// </summary>
        private const float ViewportHeight = 30f;
        
        /// <summary>
        /// 视口顶部偏移（像素）
        /// </summary>
        private const float ViewportTopOffset = -13f;
        
        /// <summary>
        /// 背景颜色透明度
        /// </summary>
        private const float BackgroundAlpha = 0.12f;
        
        /// <summary>
        /// 刻度间隔（度）
        /// </summary>
        private const int TickInterval = 5;
        
        /// <summary>
        /// 主要刻度间隔（度，用于显示方向标签）
        /// </summary>
        private const int MajorTickInterval = 30;
        
        /// <summary>
        /// 中等刻度间隔（度）
        /// </summary>
        private const int MidTickInterval = 15;
        
        /// <summary>
        /// 主要刻度透明度
        /// </summary>
        private const float MajorTickAlpha = 0.8f;
        
        /// <summary>
        /// 中等刻度透明度
        /// </summary>
        private const float MidTickAlpha = 0.8f;
        
        /// <summary>
        /// 次要刻度透明度
        /// </summary>
        private const float MinorTickAlpha = 0.8f;
        
        /// <summary>
        /// 主要刻度宽度（像素）
        /// </summary>
        private const float MajorTickWidth = 2f;
        
        /// <summary>
        /// 主要刻度高度（像素）
        /// </summary>
        private const float MajorTickHeight = 18f;
        
        /// <summary>
        /// 中等刻度宽度（像素）
        /// </summary>
        private const float MidTickWidth = 1f;
        
        /// <summary>
        /// 中等刻度高度（像素）
        /// </summary>
        private const float MidTickHeight = 12f;
        
        /// <summary>
        /// 次要刻度宽度（像素）
        /// </summary>
        private const float MinorTickWidth = 1f;
        
        /// <summary>
        /// 次要刻度高度（像素）
        /// </summary>
        private const float MinorTickHeight = 8f;
        
        /// <summary>
        /// 刻度Y位置偏移（像素）
        /// </summary>
        private const float TickYOffset = -3f;
        
        /// <summary>
        /// 标签Y位置偏移（像素）
        /// </summary>
        private const float LabelYOffset = -32f;
        
        /// <summary>
        /// 标签宽度（像素）
        /// </summary>
        private const float LabelWidth = 60f;
        
        /// <summary>
        /// 标签高度（像素）
        /// </summary>
        private const float LabelHeight = 12f;
        
        /// <summary>
        /// 标签字体大小
        /// </summary>
        private const float LabelFontSize = 12f;
        
        /// <summary>
        /// 标签颜色透明度
        /// </summary>
        private const float LabelAlpha = 0.8f;
        
        /// <summary>
        /// 东西南北方向标签字体大小（放大）
        /// </summary>
        private const float CardinalLabelFontSize = 16f;
        
        /// <summary>
        /// 东西南北方向标签颜色（浅红）
        /// </summary>
        private static readonly Color CardinalLabelColor = new Color(1f, 0.6f, 0.6f, LabelAlpha);
        
        /// <summary>
        /// 中心标记线宽度（像素）
        /// </summary>
        private const float CenterMarkerWidth = 2f;
        
        /// <summary>
        /// 中心标记线高度（像素）
        /// </summary>
        private const float CenterMarkerHeight = 16f;
        
        /// <summary>
        /// 中心标记线Y位置（像素）
        /// </summary>
        private const float CenterMarkerYPosition = 34f;
        
        /// <summary>
        /// 中心标记线颜色（红色）
        /// </summary>
        private static readonly Color CenterMarkerColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        
        /// <summary>
        /// 缝合线偏移系数（用于避免缝合线出现在中心）
        /// </summary>
        private const float SeamBiasFactor = 0.25f;
        
        /// <summary>
        /// 内置资源路径：UI背景精灵
        /// </summary>
        private const string BuiltinResourcePathUIBackground = "UI/Skin/Background.psd";
        
        /// <summary>
        /// 内置资源路径：Arial字体
        /// </summary>
        private const string BuiltinResourcePathArialFont = "Arial.ttf";
        #endregion

        #region 私有字段
        /// <summary>
        /// 指南针Canvas
        /// </summary>
        private Canvas compassCanvas;
        
        /// <summary>
        /// 指南针视口RectTransform
        /// </summary>
        private RectTransform compassViewport;
        
        /// <summary>
        /// 指南针内容RectTransform（包含两个段）
        /// </summary>
        private RectTransform compassContent;
        
        /// <summary>
        /// 指南针段A RectTransform
        /// </summary>
        private RectTransform compassSegA;
        
        /// <summary>
        /// 指南针段B RectTransform
        /// </summary>
        private RectTransform compassSegB;
        
        /// <summary>
        /// 指南针朝向文本（已移除，保留字段用于兼容性）
        /// </summary>
        private TextMeshProUGUI compassHeadingText;
        
        /// <summary>
        /// 中心标记线RectTransform
        /// </summary>
        private RectTransform centerMarker;
        
        /// <summary>
        /// UI默认精灵（用于创建背景）
        /// </summary>
        private Sprite uiDefaultSprite;
        
        /// <summary>
        /// 白色精灵（用于创建刻度线和标记）
        /// </summary>
        private Sprite whiteSprite;
        
        /// <summary>
        /// 默认字体（用于创建标签）
        /// </summary>
        private Font defaultFont;
        #endregion

        #region 指南针初始化
        /// <summary>
        /// 确保指南针已初始化
        /// 如果未初始化，则创建所有必要的UI元素
        /// </summary>
        private void EnsureCompass()
        {
            if (compassCanvas != null) return;

            // 加载必要的资源
            LoadCompassResources();

            // 创建Canvas
            CreateCompassCanvas();

            // 创建视口
            CreateCompassViewport();

            // 创建内容区域和段
            CreateCompassContent();

            // 创建中心标记
            CreateCenterMarker();
        }

        /// <summary>
        /// 加载指南针所需的资源
        /// </summary>
        private void LoadCompassResources()
        {
            // 加载UI默认精灵
            if (uiDefaultSprite == null)
            {
                try
                {
                    uiDefaultSprite = Resources.GetBuiltinResource<Sprite>(BuiltinResourcePathUIBackground);
                }
                catch
                {
                    uiDefaultSprite = null;
                }
            }

            // 创建白色精灵
            if (whiteSprite == null)
            {
                var rect = new Rect(0, 0, 1, 1);
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, rect, new Vector2(0.5f, 0.5f));
            }

            // 加载默认字体
            if (defaultFont == null)
            {
                try
                {
                    defaultFont = Resources.GetBuiltinResource<Font>(BuiltinResourcePathArialFont);
                }
                catch
                {
                    defaultFont = null;
                }
            }
        }

        /// <summary>
        /// 创建指南针Canvas
        /// </summary>
        private void CreateCompassCanvas()
        {
            var canvasGO = new GameObject("FPC_CompassCanvas");
            Object.DontDestroyOnLoad(canvasGO);
            compassCanvas = canvasGO.AddComponent<Canvas>();
            compassCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            compassCanvas.overrideSorting = true;
            compassCanvas.sortingOrder = CompassCanvasSortOrder;

            // 添加CanvasScaler用于自适应缩放
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CanvasReferenceResolution;
            scaler.matchWidthOrHeight = CompassCanvasScaleMatch;

            // 添加GraphicRaycaster（禁用，因为不需要交互）
            var raycaster = canvasGO.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            // 初始仅在第一人称时激活
            canvasGO.SetActive(isFirstPersonMode);
        }

        /// <summary>
        /// 创建指南针视口
        /// </summary>
        private void CreateCompassViewport()
        {
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(compassCanvas.transform, false);
            compassViewport = viewportGO.AddComponent<RectTransform>();
            
            // 设置视口锚点和位置（顶部全宽）
            compassViewport.anchorMin = new Vector2(0f, 1f);
            compassViewport.anchorMax = new Vector2(1f, 1f);
            compassViewport.pivot = new Vector2(0.5f, 1f);
            compassViewport.anchoredPosition = new Vector2(0f, ViewportTopOffset);
            compassViewport.sizeDelta = new Vector2(0f, ViewportHeight);

            // 添加背景
            var background = viewportGO.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.type = Image.Type.Simple;
            background.raycastTarget = false;
            background.color = new Color(0f, 0f, 0f, BackgroundAlpha);
        }

        /// <summary>
        /// 创建指南针内容区域和段
        /// </summary>
        private void CreateCompassContent()
        {
            // 创建内容容器
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(compassViewport, false);
            compassContent = contentGO.AddComponent<RectTransform>();
            
            // 设置内容锚点和位置
            compassContent.anchorMin = new Vector2(0f, 0f);
            compassContent.anchorMax = new Vector2(0f, 1f);
            compassContent.pivot = new Vector2(0f, 0.5f);
            compassContent.anchoredPosition = Vector2.zero;
            
            // 设置内容大小（双段宽度，用于无缝循环）
            compassContent.sizeDelta = new Vector2(segmentWidthPx * 2f, ViewportHeight);

            // 创建两个相同的段（用于无缝循环）
            compassSegA = BuildCompassSegment("SegmentA", contentGO.transform);
            compassSegB = BuildCompassSegment("SegmentB", contentGO.transform);
            
            // 设置段的位置
            compassSegA.anchoredPosition = new Vector2(0f, 0f);
            compassSegB.anchoredPosition = new Vector2(segmentWidthPx, 0f);
        }

        /// <summary>
        /// 创建中心标记线
        /// </summary>
        private void CreateCenterMarker()
        {
            var markerGO = new GameObject("CenterMarker");
            markerGO.transform.SetParent(compassViewport, false);
            
            var markerImage = markerGO.AddComponent<Image>();
            markerImage.sprite = whiteSprite;
            markerImage.type = Image.Type.Simple;
            markerImage.color = CenterMarkerColor;
            
            centerMarker = markerImage.rectTransform;
            centerMarker.anchorMin = new Vector2(0.5f, 0f);
            centerMarker.anchorMax = new Vector2(0.5f, 0f);
            centerMarker.pivot = new Vector2(0.5f, 1f);
            centerMarker.anchoredPosition = new Vector2(0f, CenterMarkerYPosition);
            centerMarker.sizeDelta = new Vector2(CenterMarkerWidth, CenterMarkerHeight);
        }
        #endregion

        #region 指南针段构建
        /// <summary>
        /// 构建指南针段（包含刻度和标签）
        /// </summary>
        /// <param name="name">段名称</param>
        /// <param name="parent">父Transform</param>
        /// <returns>段的RectTransform</returns>
        private RectTransform BuildCompassSegment(string name, Transform parent)
        {
            var segmentGO = new GameObject(name);
            segmentGO.transform.SetParent(parent, false);
            var segmentRect = segmentGO.AddComponent<RectTransform>();
            
            // 设置段的锚点和大小
            segmentRect.anchorMin = new Vector2(0f, 1f);
            segmentRect.anchorMax = new Vector2(0f, 1f);
            segmentRect.pivot = new Vector2(0f, 1f);
            segmentRect.sizeDelta = new Vector2(segmentWidthPx, ViewportHeight);

            // 生成刻度（0到360度，每5度一个）
            for (int degree = 0; degree <= 360; degree += TickInterval)
            {
                CreateCompassTick(segmentGO.transform, degree);
            }

            return segmentRect;
        }

        /// <summary>
        /// 创建指南针刻度
        /// </summary>
        /// <param name="parent">父Transform</param>
        /// <param name="degree">角度（度）</param>
        private void CreateCompassTick(Transform parent, int degree)
        {
            float xPosition = degree * PixelsPerDegree;
            
            // 判断刻度类型
            bool isMajorTick = (degree % MajorTickInterval) == 0;
            bool isMidTick = !isMajorTick && (degree % MidTickInterval) == 0;

            // 创建刻度GameObject
            var tickGO = new GameObject($"Tick_{degree}");
            tickGO.transform.SetParent(parent, false);
            
            var tickImage = tickGO.AddComponent<Image>();
            tickImage.sprite = whiteSprite;
            tickImage.type = Image.Type.Simple;
            
            // 设置刻度颜色和透明度
            float alpha = isMajorTick ? MajorTickAlpha : (isMidTick ? MidTickAlpha : MinorTickAlpha);
            tickImage.color = new Color(1f, 1f, 1f, alpha);

            // 设置刻度RectTransform
            var tickRect = tickImage.rectTransform;
            tickRect.anchorMin = new Vector2(0f, 1f);
            tickRect.anchorMax = new Vector2(0f, 1f);
            tickRect.pivot = new Vector2(0.5f, 1f);
            tickRect.anchoredPosition = new Vector2(xPosition, TickYOffset);
            
            // 设置刻度大小
            float tickWidth = isMajorTick ? MajorTickWidth : MidTickWidth;
            float tickHeight = isMajorTick ? MajorTickHeight : (isMidTick ? MidTickHeight : MinorTickHeight);
            tickRect.sizeDelta = new Vector2(tickWidth, tickHeight);

            // 如果是主要刻度，创建方向标签
            if (isMajorTick)
            {
                CreateCompassLabel(parent, degree, xPosition);
            }
        }

        /// <summary>
        /// 创建指南针方向标签
        /// </summary>
        /// <param name="parent">父Transform</param>
        /// <param name="degree">角度（度）</param>
        /// <param name="xPosition">X位置（像素）</param>
        private void CreateCompassLabel(Transform parent, int degree, float xPosition)
        {
            var labelGO = new GameObject($"Lab_{degree}");
            labelGO.transform.SetParent(parent, false);
            
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Top;
            labelText.enableWordWrapping = false;
            
            // 获取方向标签文本（如果是指向方向则显示中文，否则显示角度）
            string directionLabel = CardinalLabel(degree);
            bool isCardinal = !string.IsNullOrEmpty(directionLabel);
            
            // 如果是东西南北方向，使用放大字体和浅红色
            if (isCardinal)
            {
                labelText.fontSize = CardinalLabelFontSize;
                labelText.color = CardinalLabelColor;
            }
            else
            {
                labelText.fontSize = LabelFontSize;
                labelText.color = new Color(1f, 1f, 1f, LabelAlpha);
            }
            
            labelText.text = isCardinal ? directionLabel : $"{degree}\u00B0";
            
            // 设置标签RectTransform
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(xPosition, LabelYOffset);
            labelRect.sizeDelta = new Vector2(LabelWidth, LabelHeight);
        }
        #endregion

        #region 方向标签
        /// <summary>
        /// 获取基本方向标签（北、东、南、西）
        /// </summary>
        /// <param name="degree">角度（度）</param>
        /// <returns>方向标签，如果不是基本方向则返回null</returns>
        private static string CardinalLabel(int degree)
        {
            // 规范化角度到0-360范围
            int normalizedDegree = ((degree % 360) + 360) % 360;
            
            switch (normalizedDegree)
            {
                case 0: return "北";
                case 90: return "东";
                case 180: return "南";
                case 270: return "西";
                default: return null;
            }
        }

        /// <summary>
        /// 获取8方向名称（用于调试或其他用途）
        /// </summary>
        /// <param name="heading">朝向角度（度）</param>
        /// <returns>方向名称</returns>
        private static string Dir8Name(float heading)
        {
            float normalizedHeading = Mathf.Repeat(heading, 360f);
            int directionIndex = Mathf.RoundToInt(normalizedHeading / 45f) % 8;
            
            switch (directionIndex)
            {
                case 0: return "北";
                case 1: return "东北";
                case 2: return "东";
                case 3: return "东南";
                case 4: return "南";
                case 5: return "西南";
                case 6: return "西";
                default: return "西北";
            }
        }
        #endregion

        #region 指南针更新
        /// <summary>
        /// 更新指南针显示（在LateUpdate中调用）
        /// 根据相机朝向更新指南针内容位置，实现平滑滚动效果
        /// </summary>
        private void LateUpdateCompass()
        {
            // 仅在第一人称模式下显示
            if (!isFirstPersonMode)
            {
                if (compassCanvas != null && compassCanvas.gameObject != null)
                {
                    if (compassCanvas.gameObject.activeSelf)
                    {
                        compassCanvas.gameObject.SetActive(false);
                    }
                }
                return;
            }

            // 确保指南针已初始化
            EnsureCompass();
            if (mainCamera == null) return;

            // 确保Canvas激活
            if (compassCanvas != null && compassCanvas.gameObject != null)
            {
                if (!compassCanvas.gameObject.activeSelf)
                {
                    compassCanvas.gameObject.SetActive(true);
                }
            }

            // 更新指南针位置
            UpdateCompassPosition();
        }

        /// <summary>
        /// 更新指南针内容位置
        /// 使用双段无缝循环，确保指南针始终平滑滚动
        /// </summary>
        private void UpdateCompassPosition()
        {
            // 获取相机朝向角度（0-360度）
            float heading = Mathf.Repeat(mainCamera.transform.eulerAngles.y, 360f);

            // 计算视口宽度和中心偏移
            float viewportWidth = compassViewport.rect.width;
            float centerOffset = viewportWidth * 0.5f;

            // 计算朝向对应的像素位置（0到segmentWidthPx之间）
            float pixelHeading = heading * PixelsPerDegree;

            // 计算缝合线偏移（避免缝合线出现在中心）
            float seamBias = segmentWidthPx * SeamBiasFactor;

            // 计算内容位置：使得当前朝向的刻度位于视口中心
            // 内容左边缘应该位于：中心位置 - 当前朝向的像素位置
            float targetContentLeft = centerOffset - pixelHeading;

            // 使用模运算将内容位置限制在合理范围内，实现无缝循环
            // 内容位置应该在 -segmentWidthPx 到 0 之间，这样两个段（0和segmentWidthPx）能够覆盖视口
            float normalizedPosition = Mathf.Repeat(targetContentLeft + segmentWidthPx + seamBias, segmentWidthPx) - seamBias;
            float contentLeft = normalizedPosition - segmentWidthPx;

            // 更新内容位置
            compassContent.anchoredPosition = new Vector2(contentLeft, 0f);

            // 动态调整段的位置，确保无缝循环
            // 段的位置是相对于内容容器的，所以需要计算段的绝对位置（相对于视口）
            float segAPos = compassSegA.anchoredPosition.x;
            float segBPos = compassSegB.anchoredPosition.x;
            float segAAbsoluteX = contentLeft + segAPos;
            float segBAbsoluteX = contentLeft + segBPos;
            
            // 计算视口边界（扩大范围以确保及时调整）
            float viewportLeft = -viewportWidth * 0.5f - segmentWidthPx * 0.5f;
            float viewportRight = viewportWidth * 1.5f + segmentWidthPx * 0.5f;
            
            // 如果段A完全移出视口左侧，将其移动到段B的右侧
            if (segAAbsoluteX + segmentWidthPx < viewportLeft)
            {
                compassSegA.anchoredPosition = new Vector2(segBPos + segmentWidthPx, 0f);
            }
            // 如果段A完全移出视口右侧，将其移动到段B的左侧
            else if (segAAbsoluteX > viewportRight)
            {
                compassSegA.anchoredPosition = new Vector2(segBPos - segmentWidthPx, 0f);
            }
            
            // 重新计算段B的绝对位置（因为段A可能已经移动）
            segBAbsoluteX = contentLeft + compassSegB.anchoredPosition.x;
            
            // 如果段B完全移出视口左侧，将其移动到段A的右侧
            if (segBAbsoluteX + segmentWidthPx < viewportLeft)
            {
                compassSegB.anchoredPosition = new Vector2(compassSegA.anchoredPosition.x + segmentWidthPx, 0f);
            }
            // 如果段B完全移出视口右侧，将其移动到段A的左侧
            else if (segBAbsoluteX > viewportRight)
            {
                compassSegB.anchoredPosition = new Vector2(compassSegA.anchoredPosition.x - segmentWidthPx, 0f);
            }
        }
        #endregion
    }
}
