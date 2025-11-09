using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 对话气泡 UI 显示组件
    /// 在第一人称模式下显示 RandomNpc 的对话气泡消息
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 对话气泡UI常量定义
        /// <summary>
        /// 对话气泡Canvas排序顺序（高于体力条）
        /// </summary>
        private const int DialogueBubbleCanvasSortOrder = 32001;
        
        /// <summary>
        /// Canvas参考分辨率宽度
        /// </summary>
        private const float DialogueBubbleCanvasReferenceWidth = 1920f;
        
        /// <summary>
        /// Canvas参考分辨率高度
        /// </summary>
        private const float DialogueBubbleCanvasReferenceHeight = 1080f;
        
        /// <summary>
        /// Canvas缩放匹配宽度或高度的比例
        /// </summary>
        private const float DialogueBubbleCanvasScaleMatch = 0.5f;
        
        /// <summary>
        /// 对话气泡Y位置（体力条上方，体力条在170f，高度16f，标签偏移18f，再加30f间距）
        /// </summary>
        private const float DialogueBubbleY = 234f;
        
        /// <summary>
        /// 对话气泡宽度
        /// </summary>
        private const float DialogueBubbleWidth = 600f;
        
        /// <summary>
        /// 对话气泡字体大小
        /// </summary>
        private const float DialogueBubbleFontSize = 20f;
        
        /// <summary>
        /// 对话气泡文字颜色（白色）
        /// </summary>
        private static readonly Color DialogueBubbleTextColor = Color.white;
        
        /// <summary>
        /// 对话气泡文字边缘颜色（蓝色）
        /// </summary>
        private static readonly Color DialogueBubbleOutlineColor = new Color(0f, 0.5f, 1f, 1f); // 蓝色
        
        /// <summary>
        /// 对话气泡边缘宽度
        /// </summary>
        private const float DialogueBubbleOutlineWidth = 0.3f;
        
        /// <summary>
        /// 对话气泡显示持续时间（秒）
        /// </summary>
        private const float DialogueBubbleDisplayDuration = 5f;
        
        /// <summary>
        /// 对话气泡淡入淡出时间（秒）
        /// </summary>
        private const float DialogueBubbleFadeDuration = 0.3f;
        #endregion
        
        #region 对话气泡UI字段
        /// <summary>
        /// 对话气泡UI Canvas组件
        /// </summary>
        private Canvas dialogueBubbleCanvas;
        
        /// <summary>
        /// 对话气泡文本组件
        /// </summary>
        private TextMeshProUGUI dialogueBubbleText;
        
        /// <summary>
        /// 当前显示的消息
        /// </summary>
        private string currentDialogueMessage;
        
        /// <summary>
        /// 消息显示开始时间
        /// </summary>
        private float messageStartTime;
        
        /// <summary>
        /// 是否正在显示消息
        /// </summary>
        private bool isShowingDialogue;
        #endregion
        
        #region 对话气泡UI初始化
        /// <summary>
        /// 确保对话气泡UI已初始化
        /// </summary>
        private void EnsureDialogueBubbleUI()
        {
            if (dialogueBubbleCanvas != null) return;
            
            // 创建对话气泡Canvas
            CreateDialogueBubbleCanvas();
            
            // 创建对话气泡文本
            CreateDialogueBubbleText();
            
            // 初始隐藏
            if (dialogueBubbleText != null)
            {
                dialogueBubbleText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 创建对话气泡Canvas
        /// </summary>
        private void CreateDialogueBubbleCanvas()
        {
            var canvasGO = new GameObject("FPC_DialogueBubbleCanvas");
            Object.DontDestroyOnLoad(canvasGO);
            
            dialogueBubbleCanvas = canvasGO.AddComponent<Canvas>();
            dialogueBubbleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueBubbleCanvas.overrideSorting = true;
            dialogueBubbleCanvas.sortingOrder = DialogueBubbleCanvasSortOrder;
            
            // 添加CanvasScaler以支持不同分辨率
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DialogueBubbleCanvasReferenceWidth, DialogueBubbleCanvasReferenceHeight);
            scaler.matchWidthOrHeight = DialogueBubbleCanvasScaleMatch;
            
            // 添加GraphicRaycaster但禁用以避免阻挡输入
            var graphicRaycaster = canvasGO.AddComponent<GraphicRaycaster>();
            graphicRaycaster.enabled = false;
        }
        
        /// <summary>
        /// 创建对话气泡文本
        /// </summary>
        private void CreateDialogueBubbleText()
        {
            var textGO = new GameObject("DialogueBubbleText");
            textGO.transform.SetParent(dialogueBubbleCanvas.transform, false);
            
            dialogueBubbleText = textGO.AddComponent<TextMeshProUGUI>();
            dialogueBubbleText.text = "";
            dialogueBubbleText.color = DialogueBubbleTextColor;
            dialogueBubbleText.fontSize = DialogueBubbleFontSize;
            dialogueBubbleText.alignment = TextAlignmentOptions.Center;
            dialogueBubbleText.enableWordWrapping = true;
            
            // 设置蓝色边缘（Outline）
            dialogueBubbleText.outlineWidth = DialogueBubbleOutlineWidth;
            dialogueBubbleText.outlineColor = DialogueBubbleOutlineColor;
            
            // 设置RectTransform
            var rectTransform = dialogueBubbleText.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, DialogueBubbleY);
            rectTransform.sizeDelta = new Vector2(DialogueBubbleWidth, 0f); // 高度自适应
        }
        #endregion
        
        #region 对话气泡UI更新
        /// <summary>
        /// 更新对话气泡UI（在LateUpdate中调用）
        /// </summary>
        private void LateUpdateDialogueBubble()
        {
            // 仅在第一人称模式下显示
            if (!isFirstPersonMode)
            {
                if (dialogueBubbleCanvas != null && dialogueBubbleCanvas.gameObject.activeSelf)
                {
                    dialogueBubbleCanvas.gameObject.SetActive(false);
                }
                return;
            }
            
            // 确保UI已初始化
            EnsureDialogueBubbleUI();
            
            // 确保Canvas处于激活状态
            if (dialogueBubbleCanvas != null && !dialogueBubbleCanvas.gameObject.activeSelf)
            {
                dialogueBubbleCanvas.gameObject.SetActive(true);
            }
            
            // 更新消息显示
            if (isShowingDialogue && dialogueBubbleText != null)
            {
                float elapsedTime = Time.unscaledTime - messageStartTime;
                float totalDuration = DialogueBubbleDisplayDuration;
                
                if (elapsedTime >= totalDuration)
                {
                    // 消息显示时间已到，隐藏
                    HideDialogueBubble();
                }
                else
                {
                    // 计算透明度（淡入淡出效果）
                    float alpha = 1f;
                    if (elapsedTime < DialogueBubbleFadeDuration)
                    {
                        // 淡入
                        alpha = elapsedTime / DialogueBubbleFadeDuration;
                    }
                    else if (elapsedTime > totalDuration - DialogueBubbleFadeDuration)
                    {
                        // 淡出
                        alpha = (totalDuration - elapsedTime) / DialogueBubbleFadeDuration;
                    }
                    
                    // 应用透明度
                    Color textColor = DialogueBubbleTextColor;
                    textColor.a = alpha;
                    dialogueBubbleText.color = textColor;
                    
                    Color outlineColor = DialogueBubbleOutlineColor;
                    outlineColor.a = alpha;
                    dialogueBubbleText.outlineColor = outlineColor;
                }
            }
        }
        
        /// <summary>
        /// 显示对话气泡消息
        /// </summary>
        /// <param name="message">消息内容</param>
        private void ShowDialogueBubble(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            
            try
            {
                EnsureDialogueBubbleUI();
                
                if (dialogueBubbleText != null)
                {
                    dialogueBubbleText.text = message;
                    dialogueBubbleText.gameObject.SetActive(true);
                    
                    // 重置透明度
                    Color textColor = DialogueBubbleTextColor;
                    textColor.a = 0f; // 初始透明度为0，会通过LateUpdate逐渐显示
                    dialogueBubbleText.color = textColor;
                    
                    Color outlineColor = DialogueBubbleOutlineColor;
                    outlineColor.a = 0f;
                    dialogueBubbleText.outlineColor = outlineColor;
                    
                    currentDialogueMessage = message;
                    messageStartTime = Time.unscaledTime;
                    isShowingDialogue = true;
                    
                    FPLogger.Log("显示对话气泡: {0}", message);
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "显示对话气泡时出错");
            }
        }
        
        /// <summary>
        /// 隐藏对话气泡
        /// </summary>
        private void HideDialogueBubble()
        {
            if (dialogueBubbleText != null)
            {
                dialogueBubbleText.gameObject.SetActive(false);
                dialogueBubbleText.text = "";
            }
            
            currentDialogueMessage = null;
            isShowingDialogue = false;
        }
        #endregion
    }
}

