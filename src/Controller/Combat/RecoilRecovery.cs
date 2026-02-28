using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 后坐力恢复系统模块
    /// 负责处理后坐力的恢复逻辑，使用指数衰减实现平滑回弹
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 后坐力恢复系统
        /// <summary>
        /// 更新后坐力恢复
        /// 后坐力回弹系统：现代FPS设计思路
        /// - 连续射击时：后坐力累积，不恢复
        /// - 停止射击时：松开鼠标后开始恢复，使用指数衰减
        /// - 基于枪械属性：使用RecoilRecover和RecoilRecoverTime
        /// </summary>
        private void UpdateRecoilRecovery()
        {
            // 如果没有累积后坐力，直接返回
            if (accumulatedRecoilV == 0f && accumulatedRecoilH == 0f) return;
            
            // 如果正在射击，不恢复后坐力（让后坐力累积）
            // 注意：isShooting在Core.cs中定义，通过partial class共享
            if (isShooting) return;
            
            // 获取枪械的恢复属性
            float recoverRate = 4f; // 默认恢复速度（每秒恢复系数）
            float recoverTime = 0.5f; // 默认恢复时间（秒）
            
            if (currentRecoilGun != null)
            {
                try
                {
                    // 使用枪械的RecoilRecover属性（如果存在）
                    float gunRecover = currentRecoilGun.RecoilRecover;
                    if (gunRecover > 0f)
                    {
                        // RecoilRecover通常是一个速度值，转换为恢复系数
                        recoverRate = gunRecover * 6f; // 调整系数以匹配实际游戏体验
                    }
                    
                    // 使用枪械的RecoilRecoverTime属性
                    float gunRecoverTime = currentRecoilGun.RecoilRecoverTime;
                    if (gunRecoverTime > 0f)
                    {
                        recoverTime = gunRecoverTime;
                        // 根据恢复时间调整恢复速度（约3倍时间常数）
                        recoverRate = 1f / Mathf.Max(0.1f, recoverTime);
                    }
                }
                catch
                {
                    // 获取枪械属性失败时使用默认值
                }
            }
            
            // 计算恢复系数：使用指数衰减公式
            // 公式：newValue = oldValue * exp(-rate * deltaTime)
            // 这样恢复速度会随时间逐渐变慢，符合现代FPS游戏的手感
            float recoverFactor = Mathf.Exp(-recoverRate * Time.unscaledDeltaTime);
            
            // 恢复垂直后坐力（向上回弹）
            if (accumulatedRecoilV > 0.001f)
            {
                float oldV = accumulatedRecoilV;
                accumulatedRecoilV *= recoverFactor; // 指数衰减
                float recoveredV = oldV - accumulatedRecoilV;
                pitch += recoveredV; // 恢复pitch（向上回弹）
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                
                // 清理微小值
                if (accumulatedRecoilV < 0.001f)
                {
                    accumulatedRecoilV = 0f;
                }
            }
            
            // 恢复水平后坐力（向中心回弹）
            if (Mathf.Abs(accumulatedRecoilH) > 0.001f)
            {
                float oldH = accumulatedRecoilH;
                accumulatedRecoilH *= recoverFactor; // 指数衰减
                float recoveredH = oldH - accumulatedRecoilH;
                yaw -= recoveredH; // 恢复yaw（向中心回弹）
                
                // 清理微小值
                if (Mathf.Abs(accumulatedRecoilH) < 0.001f)
                {
                    accumulatedRecoilH = 0f;
                }
            }
        }
        #endregion
    }
}

