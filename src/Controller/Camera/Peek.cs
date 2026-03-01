using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - Peek偏头系统模块
    /// 负责处理左右偏头功能，支持长按/切换模式选择
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region Peek偏头系统字段和配置
        [SerializeField] private float peekMaxOffset = 0.35f;      // 最大偏移距离
        [SerializeField] private float peekMaxRotation = 15f;      // 最大旋转角度
        [SerializeField] private float peekLerpSpeed = 6f;         // 平滑速度

        private KeyCode peekLeftKey = KeyCode.Q;
        private KeyCode peekRightKey = KeyCode.E;

        private const int PEEK_KEY_LOAD_INTERVAL = 30;             // 按键加载间隔

        private GameObject peekColliderProxy;                       // 碰撞代理

        // 模式相关
        private int peekMode = 0;                                   // 0=长按,1=切换
        private int peekToggleState = 0;                            // 切换模式下的状态：-1左偏,0居中,1右偏
        #endregion

        #region 公共方法
        public void SetPeekLeftKey(KeyCode key) => peekLeftKey = key;
        public void SetPeekRightKey(KeyCode key) => peekRightKey = key;

        /// <summary>
        /// 从选项加载偏头按键配置
        /// </summary>
        public void LoadPeekKeysFromOptions()
        {
            try
            {
                peekLeftKey = FirstPersonCamera.Utilities.OptionsHelper.LoadKeyCode(
                    "FirstPersonCamera_PeekLeftKeyCode", KeyCode.Q);
                peekRightKey = FirstPersonCamera.Utilities.OptionsHelper.LoadKeyCode(
                    "FirstPersonCamera_PeekRightKeyCode", KeyCode.E);
            }
            catch
            {
                peekLeftKey = KeyCode.Q;
                peekRightKey = KeyCode.E;
            }
        }

        /// <summary>
        /// 从选项加载偏头模式
        /// </summary>
        private void LoadPeekModeFromOptions()
        {
            try
            {
                peekMode = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(
                    OptionsUIConstants.PeekModeKey, OptionsUIConstants.PeekModeDefault);
            }
            catch
            {
                peekMode = 0;
            }
        }
        #endregion

        #region 核心更新逻辑
        /// <summary>
        /// 更新偏头输入
        /// </summary>
        private void UpdatePeekInput(bool uiBlocking)
        {
            // 定期加载键位和模式
            if (Time.frameCount % PEEK_KEY_LOAD_INTERVAL == 0)
            {
                LoadPeekKeysFromOptions();
                LoadPeekModeFromOptions();
            }

            if (uiBlocking)
            {
                // UI阻挡时，无论什么模式都强制归零
                if (peekMode == 0) // 长按模式直接归零
                {
                    float t = 1f - Mathf.Exp(-peekLerpSpeed * Time.unscaledDeltaTime);
                    peekOffset = Mathf.Lerp(peekOffset, 0f, t);
                }
                else // 切换模式：清除状态并归零
                {
                    peekToggleState = 0;
                    float t = 1f - Mathf.Exp(-peekLerpSpeed * Time.unscaledDeltaTime);
                    peekOffset = Mathf.Lerp(peekOffset, 0f, t);
                }
                return;
            }

            // 非UI阻挡
            if (peekMode == 0) // 长按模式
            {
                float targetPeek = 0f;
                bool leftPressed = Input.GetKey(peekLeftKey);
                bool rightPressed = Input.GetKey(peekRightKey);

                if (leftPressed && !rightPressed) targetPeek = -1f;
                else if (!leftPressed && rightPressed) targetPeek = 1f;
                // 其余情况保持0

                float t = 1f - Mathf.Exp(-peekLerpSpeed * Time.unscaledDeltaTime);
                peekOffset = Mathf.Lerp(peekOffset, targetPeek, t);
            }
            else // 切换模式
            {
                bool leftDown = Input.GetKeyDown(peekLeftKey);
                bool rightDown = Input.GetKeyDown(peekRightKey);

                if (leftDown && !rightDown)
                {
                    // 左键按下：如果当前为左偏则关闭，否则开启左偏
                    peekToggleState = (peekToggleState == -1) ? 0 : -1;
                }
                else if (!leftDown && rightDown)
                {
                    // 右键按下：如果当前为右偏则关闭，否则开启右偏
                    peekToggleState = (peekToggleState == 1) ? 0 : 1;
                }
                else if (leftDown && rightDown)
                {
                    // 同时按下：归零
                    peekToggleState = 0;
                }
                // 无按键时保持状态不变

                float targetPeek = peekToggleState; // -1,0,1
                float t = 1f - Mathf.Exp(-peekLerpSpeed * Time.unscaledDeltaTime);
                peekOffset = Mathf.Lerp(peekOffset, targetPeek, t);
            }
        }
        #endregion

        #region 碰撞代理管理
        private void CreatePeekColliderProxy()
        {
            if (peekColliderProxy != null) return;
            if (mainCharacter == null) return;

            peekColliderProxy = new GameObject("PeekColliderProxy");
            peekColliderProxy.transform.SetParent(mainCamera != null ? mainCamera.transform : transform, false);
            peekColliderProxy.transform.localPosition = Vector3.zero;

            var sphereCollider = peekColliderProxy.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.18f;
            sphereCollider.isTrigger = false;

            int damageReceiverLayer = LayerMask.NameToLayer("DamageReceiver");
            if (damageReceiverLayer >= 0)
                peekColliderProxy.layer = damageReceiverLayer;
            else
                peekColliderProxy.layer = gameObject.layer;

            var damageReceiver = peekColliderProxy.AddComponent<DamageReceiver>();
            if (damageReceiver != null && mainCharacter != null && mainCharacter.Health != null)
            {
                damageReceiver.health = mainCharacter.Health;
                damageReceiver.useSimpleHealth = false;
            }
        }

        private void DestroyPeekColliderProxy()
        {
            if (peekColliderProxy != null)
            {
                Destroy(peekColliderProxy);
                peekColliderProxy = null;
            }
        }
        #endregion
    }
}