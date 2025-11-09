using UnityEngine;
using FirstPersonCamera.Utilities;
using Duckov.Options;
using Duckov.Options.UI;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.StandaloneUI;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称选项UI主控制器（简化版）
    /// 只在游戏原版设置中添加一个标签页按钮，用于打开独立的设置窗口
    /// </summary>
    public class FirstPersonOptionsUI : MonoBehaviour
    {
        #region 向后兼容的常量引用
        /// <summary>
        /// 选项键：启用后坐力（向后兼容，实际定义在OptionsUIConstants中）
        /// </summary>
        public const string EnableRecoilKey = OptionsUIConstants.EnableRecoilKey;
        
        /// <summary>
        /// 选项键：启用ADS武器位移（向后兼容，实际定义在OptionsUIConstants中）
        /// </summary>
        public const string EnableAdsOffsetKey = OptionsUIConstants.EnableAdsOffsetKey;
        
        /// <summary>
        /// 选项键：后坐力强度（向后兼容，实际定义在OptionsUIConstants中）
        /// </summary>
        public const string RecoilStrengthKey = OptionsUIConstants.RecoilStrengthKey;
        
        /// <summary>
        /// 选项键：禁用模糊（向后兼容，实际定义在OptionsUIConstants中）
        /// </summary>
        public const string DisableBlurKey = OptionsUIConstants.DisableBlurKey;
        
        /// <summary>
        /// 选项键：禁用遮挡透视（向后兼容，实际定义在OptionsUIConstants中）
        /// </summary>
        public const string DisableSeeThroughKey = OptionsUIConstants.DisableSeeThroughKey;
        
        /// <summary>
        /// 选项键：禁用准星虚化（向后兼容，实际定义在OptionsUIConstants中）
        /// </summary>
        public const string DisableAimOcclusionFadeKey = OptionsUIConstants.DisableAimOcclusionFadeKey;
        #endregion

        #region 私有字段
        /// <summary>
        /// 缓存的选项面板
        /// </summary>
        private OptionsPanel cachedOptionsPanel;
        
        /// <summary>
        /// 按钮是否已注入
        /// </summary>
        private bool buttonInjected = false;
        
        /// <summary>
        /// 缓存的按钮引用
        /// </summary>
        private GameObject cachedButton;
        
        /// <summary>
        /// 缓存的按钮文本组件
        /// </summary>
        private TMP_Text cachedButtonText;
        #endregion

        #region Unity生命周期方法
        /// <summary>
        /// Unity Update方法：检查并注入打开设置按钮
        /// </summary>
        private void Update()
        {
            // 查找选项面板
            if (cachedOptionsPanel == null)
            {
                cachedOptionsPanel = Object.FindObjectOfType<OptionsPanel>(true);
            }

            // 如果选项面板存在且激活，则注入按钮
            if (cachedOptionsPanel != null && cachedOptionsPanel.gameObject.activeInHierarchy)
            {
                if (!buttonInjected)
                {
                    TryInjectOpenSettingsButton(cachedOptionsPanel);
                }
                else
                {
                    // 持续检查并修复按钮文本
                    KeepButtonTextCorrect();
                }
            }
            else
            {
                // 如果选项面板关闭，也关闭我们的设置窗口
                if (cachedOptionsPanel != null && !cachedOptionsPanel.gameObject.activeInHierarchy)
                {
                    try
                    {
                        StandaloneOptionsWindow.Instance?.Hide();
                    }
                    catch { }
                }
            }
        }
        #endregion

        #region 按钮注入方法
        /// <summary>
        /// 尝试注入打开设置按钮（作为标签页按钮）
        /// </summary>
        /// <param name="panel">选项面板</param>
        private void TryInjectOpenSettingsButton(OptionsPanel panel)
        {
            try
            {
                // 检查是否已存在按钮
                var existingButton = panel.transform.Find("FirstPersonCamera_OpenSettingsTabButton");
                if (existingButton != null)
                {
                    buttonInjected = true;
                    return;
                }

                // 使用反射获取所有标签按钮
                var tabButtonsField = typeof(OptionsPanel).GetField("tabButtons", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (tabButtonsField == null) return;

                var tabButtons = tabButtonsField.GetValue(panel) as System.Collections.IList;
                if (tabButtons == null || tabButtons.Count == 0) return;

                // 获取第一个标签按钮作为模板
                var templateButton = tabButtons[0] as OptionsPanel_TabButton;
                if (templateButton == null) return;

                // 获取标签按钮的父容器
                var buttonParent = templateButton.transform.parent;
                if (buttonParent == null) return;

                // 创建新的标签按钮
                var buttonGO = Object.Instantiate(templateButton.gameObject, buttonParent);
                buttonGO.name = "FirstPersonCamera_OpenSettingsTabButton";

                // 获取按钮组件
                var newTabButton = buttonGO.GetComponent<OptionsPanel_TabButton>();
                if (newTabButton == null) return;

                // 设置按钮文本并缓存
                var buttonText = buttonGO.GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null)
                {
                    buttonText.text = "第一人称相机";
                    cachedButtonText = buttonText;
                    cachedButton = buttonGO;
                }

                // 创建一个空的标签页内容（实际上不会显示，只是为了让按钮正常工作）
                var templateTab = UIHelpers.GetPrivateField<GameObject>(templateButton, "tab");
                GameObject emptyTab = null;
                if (templateTab != null)
                {
                    emptyTab = Object.Instantiate(templateTab, templateTab.transform.parent);
                    emptyTab.name = "FirstPersonCamera_EmptyTab";
                    emptyTab.SetActive(false);
                    
                    // 清空内容
                    foreach (Transform child in emptyTab.transform)
                    {
                        Object.Destroy(child.gameObject);
                    }
                }

                // 绑定空的标签页
                if (emptyTab != null)
                {
                    UIHelpers.SetPrivateField(newTabButton, "tab", emptyTab);
                }

                // 修改按钮点击事件，打开独立设置窗口（只显示，不切换）
                newTabButton.onClicked = (btn, ev) =>
                {
                    try
                    {
                        ev.Use(); // 阻止默认行为，避免触发标签页切换
                        FPLogger.Log("点击第一人称相机按钮，准备打开设置窗口");
                        var window = StandaloneOptionsWindow.Instance;
                        if (window != null)
                        {
                            window.Show();
                            FPLogger.Log("设置窗口已调用Show()");
                        }
                        else
                        {
                            FPLogger.LogError("StandaloneOptionsWindow.Instance 为空");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        FPLogger.LogException(ex, "打开设置窗口失败");
                    }
                };

                // 添加到标签按钮列表
                tabButtons.Add(newTabButton);

                buttonInjected = true;
                FPLogger.Log("已在原版设置中添加第一人称相机标签页按钮");
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "注入打开设置按钮失败");
            }
        }

        /// <summary>
        /// 持续保持按钮文本正确
        /// </summary>
        private void KeepButtonTextCorrect()
        {
            if (cachedButton != null && cachedButtonText != null)
            {
                if (cachedButtonText.text != "第一人称相机")
                {
                    cachedButtonText.text = "第一人称相机";
                }
            }
            else if (cachedOptionsPanel != null)
            {
                // 重新查找按钮
                var button = cachedOptionsPanel.transform.Find("FirstPersonCamera_OpenSettingsTabButton");
                if (button != null)
                {
                    cachedButton = button.gameObject;
                    cachedButtonText = button.GetComponentInChildren<TMP_Text>(true);
                    if (cachedButtonText != null)
                    {
                        cachedButtonText.text = "第一人称相机";
                    }
                }
            }
        }
        #endregion
    }
}
