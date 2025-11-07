using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.StandaloneUI
{
    /// <summary>
    /// 独立UI按键重绑定组件
    /// </summary>
    public class StandaloneKeyRebinder : MonoBehaviour
    {
        private string configKey;
        private TextMeshProUGUI buttonText;
        private Button button;
        private KeyCode defaultKey;
        private bool isWaitingForInput = false;
        private string originalText;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(string key, TextMeshProUGUI text, Button btn, KeyCode defaultKeyCode)
        {
            configKey = key;
            buttonText = text;
            button = btn;
            defaultKey = defaultKeyCode;
            originalText = text.text;
            
            button.onClick.AddListener(StartRebinding);
        }

        /// <summary>
        /// 开始重绑定
        /// </summary>
        private void StartRebinding()
        {
            if (isWaitingForInput) return;
            
            isWaitingForInput = true;
            buttonText.text = "按任意键...";
            button.interactable = false;
        }

        private void Update()
        {
            if (!isWaitingForInput) return;
            
            // 使用unscaledDeltaTime确保在暂停时也能工作
            // 检测按键输入
            if (Input.anyKeyDown)
            {
                KeyCode pressedKey = GetPressedKey();
                if (pressedKey != KeyCode.None && pressedKey != KeyCode.Mouse0 && pressedKey != KeyCode.Mouse1 && pressedKey != KeyCode.Mouse2)
                {
                    // 保存按键
                    ConfigManager.Save<KeyCode>(configKey, pressedKey);
                    buttonText.text = pressedKey.ToString();
                    originalText = pressedKey.ToString();
                    
                    // 更新控制器
                    UpdateController();
                    
                    // 立即应用设置变更
                    OptionsHelper.ApplySettingChange(configKey);
                    
                    isWaitingForInput = false;
                    button.interactable = true;
                }
            }
            
            // ESC取消
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                buttonText.text = originalText;
                isWaitingForInput = false;
                button.interactable = true;
            }
        }

        /// <summary>
        /// 获取按下的按键
        /// </summary>
        private KeyCode GetPressedKey()
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    return keyCode;
                }
            }
            return KeyCode.None;
        }

        /// <summary>
        /// 更新控制器
        /// </summary>
        private void UpdateController()
        {
            try
            {
                KeyCode key = OptionsHelper.LoadKeyCode(configKey, defaultKey);
                
                if (configKey == "FirstPersonCamera_ToggleKeyCode")
                {
                    FirstPersonCameraController.Instance?.SetToggleKey(key);
                }
                else if (configKey == "FirstPersonCamera_PeekLeftKeyCode")
                {
                    FirstPersonCameraController.Instance?.SetPeekLeftKey(key);
                }
                else if (configKey == "FirstPersonCamera_PeekRightKeyCode")
                {
                    FirstPersonCameraController.Instance?.SetPeekRightKey(key);
                }
            }
            catch { }
        }
    }
}

