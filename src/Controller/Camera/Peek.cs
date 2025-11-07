using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - Peek偏头系统模块
    /// 负责处理左右偏头功能，包括按键检测和平滑插值
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region Peek偏头系统字段和配置
        /// <summary>
        /// 偏头最大偏移距离（米）
        /// </summary>
        [SerializeField] private float peekMaxOffset = 0.35f;
        
        /// <summary>
        /// 偏头最大旋转角度（度）
        /// </summary>
        [SerializeField] private float peekMaxRotation = 15f;
        
        /// <summary>
        /// 偏头平滑插值速度（每秒）
        /// </summary>
        [SerializeField] private float peekLerpSpeed = 6f;
        
        /// <summary>
        /// 左偏头按键（默认Q）
        /// </summary>
        private KeyCode peekLeftKey = KeyCode.Q;
        
        /// <summary>
        /// 右偏头按键（默认E）
        /// </summary>
        private KeyCode peekRightKey = KeyCode.E;
        
        /// <summary>
        /// 偏头按键加载频率（每N帧加载一次，避免每帧都加载）
        /// </summary>
        private const int PEEK_KEY_LOAD_INTERVAL = 30;
        
        /// <summary>
        /// Peek偏头时的碰撞代理对象（用于头部受击检测）
        /// 在相机位置创建一个球形碰撞器，作为头部受击检测的代理
        /// </summary>
        private GameObject peekColliderProxy;
        #endregion

        #region Peek偏头输入处理
        /// <summary>
        /// 更新偏头输入
        /// 检测左右偏头按键，平滑插值到目标偏头值
        /// </summary>
        /// <param name="uiBlocking">是否被UI阻挡</param>
        private void UpdatePeekInput(bool uiBlocking)
        {
            float targetPeek = 0f;

            if (!uiBlocking)
            {
                // 只在首次或定期加载侧头键位（避免每帧都加载）
                if (Time.frameCount % PEEK_KEY_LOAD_INTERVAL == 0)
                {
                    LoadPeekKeysFromOptions();
                }
                
                // 检测配置的按键
                bool leftPressed = false;
                bool rightPressed = false;

                try
                {
                    if (useNewInputSystem)
                    {
                        var keyboard = Keyboard.current;
                        if (keyboard != null)
                        {
                            leftPressed = GetKeyPressed(keyboard, peekLeftKey);
                            rightPressed = GetKeyPressed(keyboard, peekRightKey);
                        }
                    }
                    else
                    {
                        leftPressed = Input.GetKey(peekLeftKey);
                        rightPressed = Input.GetKey(peekRightKey);
                    }
                }
                catch
                {
                    // 按键检测失败时使用默认值
                }

                // 根据按键状态设置目标偏头值
                if (leftPressed && !rightPressed)
                {
                    targetPeek = -1f;  // 左偏
                }
                else if (!leftPressed && rightPressed)
                {
                    targetPeek = 1f;  // 右偏
                }
                else if (leftPressed && rightPressed)
                {
                    targetPeek = 0f;  // 同时按归零
                }
                else
                {
                    targetPeek = 0f;  // 都不按归零
                }
            }

            // 平滑插值到目标偏头值
            float t = 1f - Mathf.Exp(-peekLerpSpeed * Time.unscaledDeltaTime);
            peekOffset = Mathf.Lerp(peekOffset, targetPeek, t);
        }

        /// <summary>
        /// 从Keyboard获取按键状态（辅助方法）
        /// </summary>
        /// <param name="keyboard">键盘输入对象</param>
        /// <param name="keyCode">要检测的按键码</param>
        /// <returns>按键是否按下</returns>
        private bool GetKeyPressed(Keyboard keyboard, KeyCode keyCode)
        {
            try
            {
                switch (keyCode)
                {
                    case KeyCode.Q: return keyboard.qKey.isPressed;
                    case KeyCode.E: return keyboard.eKey.isPressed;
                    case KeyCode.A: return keyboard.aKey.isPressed;
                    case KeyCode.S: return keyboard.sKey.isPressed;
                    case KeyCode.D: return keyboard.dKey.isPressed;
                    case KeyCode.W: return keyboard.wKey.isPressed;
                    case KeyCode.Z: return keyboard.zKey.isPressed;
                    case KeyCode.X: return keyboard.xKey.isPressed;
                    case KeyCode.C: return keyboard.cKey.isPressed;
                    case KeyCode.V: return keyboard.vKey.isPressed;
                    case KeyCode.R: return keyboard.rKey.isPressed;
                    case KeyCode.T: return keyboard.tKey.isPressed;
                    case KeyCode.G: return keyboard.gKey.isPressed;
                    case KeyCode.B: return keyboard.bKey.isPressed;
                    case KeyCode.F: return keyboard.fKey.isPressed;
                    case KeyCode.Tab: return keyboard.tabKey.isPressed;
                    case KeyCode.LeftShift: return keyboard.leftShiftKey.isPressed;
                    case KeyCode.RightShift: return keyboard.rightShiftKey.isPressed;
                    case KeyCode.LeftControl: return keyboard.leftCtrlKey.isPressed;
                    case KeyCode.RightControl: return keyboard.rightCtrlKey.isPressed;
                    case KeyCode.LeftAlt: return keyboard.leftAltKey.isPressed;
                    case KeyCode.RightAlt: return keyboard.rightAltKey.isPressed;
                    case KeyCode.Space: return keyboard.spaceKey.isPressed;
                    default: return Input.GetKey(keyCode);
                }
            }
            catch
            {
                return Input.GetKey(keyCode);
            }
        }

        /// <summary>
        /// 从选项加载偏头按键配置
        /// </summary>
        public void LoadPeekKeysFromOptions()
        {
            try
            {
                KeyCode leftCode = FirstPersonCamera.Utilities.OptionsHelper.LoadKeyCode(
                    "FirstPersonCamera_PeekLeftKeyCode", KeyCode.Q);
                KeyCode rightCode = FirstPersonCamera.Utilities.OptionsHelper.LoadKeyCode(
                    "FirstPersonCamera_PeekRightKeyCode", KeyCode.E);
                peekLeftKey = leftCode;
                peekRightKey = rightCode;
            }
            catch
            {
                // 加载失败时使用默认值
                peekLeftKey = KeyCode.Q;
                peekRightKey = KeyCode.E;
            }
        }

        /// <summary>
        /// 设置左偏头按键
        /// </summary>
        /// <param name="key">按键码</param>
        public void SetPeekLeftKey(KeyCode key) => peekLeftKey = key;

        /// <summary>
        /// 设置右偏头按键
        /// </summary>
        /// <param name="key">按键码</param>
        public void SetPeekRightKey(KeyCode key) => peekRightKey = key;
        #endregion

        #region Peek碰撞代理管理
        /// <summary>
        /// 创建Peek偏头时的碰撞代理对象
        /// 在相机位置创建一个球形碰撞器，用于头部受击检测
        /// 当玩家偏头时，头部位置会移动，这个代理碰撞器会跟随相机位置
        /// </summary>
        private void CreatePeekColliderProxy()
        {
            if (peekColliderProxy != null) return; // 已存在，直接返回
            if (mainCharacter == null) return;

            // 创建代理对象，挂载到相机上
            peekColliderProxy = new GameObject("PeekColliderProxy");
            peekColliderProxy.transform.SetParent(mainCamera != null ? mainCamera.transform : transform, false);
            peekColliderProxy.transform.localPosition = Vector3.zero; // 位于相机中心

            // 添加球形碰撞器用于头部受击检测
            var sphereCollider = peekColliderProxy.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.18f; // 略大于典型头部半径
            sphereCollider.isTrigger = false; // 非触发器，用于碰撞检测

            // 设置图层为"DamageReceiver"，使子弹可以命中
            int damageReceiverLayer = LayerMask.NameToLayer("DamageReceiver");
            if (damageReceiverLayer >= 0)
            {
                peekColliderProxy.layer = damageReceiverLayer;
            }
            else
            {
                // 如果找不到DamageReceiver层，使用游戏对象默认层
                peekColliderProxy.layer = gameObject.layer;
            }

            // 添加DamageReceiver组件，链接到主角色的生命值
            var damageReceiver = peekColliderProxy.AddComponent<DamageReceiver>();
            if (damageReceiver != null && mainCharacter != null && mainCharacter.Health != null)
            {
                damageReceiver.health = mainCharacter.Health;
                damageReceiver.useSimpleHealth = false;
            }
        }

        /// <summary>
        /// 销毁Peek偏头时的碰撞代理对象
        /// </summary>
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

