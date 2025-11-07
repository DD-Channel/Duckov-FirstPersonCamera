using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 鼠标输入捕获模块
    /// 负责捕获鼠标增量输入，在Update中捕获，在LateUpdate中消费
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 鼠标输入捕获
        /// <summary>
        /// 捕获鼠标增量（在Update中调用）
        /// 在LateUpdate中消费，防止1帧延迟导致的峰值
        /// </summary>
        /// <param name="uiBlocking">是否被UI阻挡</param>
        private void CaptureMouseDelta(bool uiBlocking)
        {
            if (uiBlocking)
            {
                pendingMouseX = 0f;
                pendingMouseY = 0f;
                return;
            }
            
            float mx = 0f, my = 0f;
            try
            {
                if (useNewInputSystem)
                {
                    var mouse = Mouse.current;
                    if (mouse != null)
                    {
                        Vector2 d = mouse.delta.ReadValue();
                        mx = d.x;
                        my = d.y;
                    }
                }
                else
                {
                    mx = Input.GetAxis("Mouse X");
                    my = Input.GetAxis("Mouse Y");
                }
            }
            catch
            {
                // 获取鼠标输入失败时使用默认值
                mx = 0f;
                my = 0f;
            }
            
            // 累加鼠标增量（在LateUpdate中消费）
            pendingMouseX += mx;
            pendingMouseY += my;
        }
        #endregion
    }
}

