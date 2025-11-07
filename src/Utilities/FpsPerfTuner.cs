using System;
using System.Reflection;
using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称性能调优器
    /// 轻量级运行时优化，用于降低第一人称模式下的每帧开销
    /// 通过调整帧末对齐频率和相机设置来提升性能
    /// </summary>
    [DefaultExecutionOrder(9999)]
    public class FpsPerfTuner : MonoBehaviour
    {
        #region 常量定义
        /// <summary>
        /// 反射字段名：endOfFrameAlignEveryNFrames（帧末对齐每N帧执行）
        /// </summary>
        private const string ReflectionFieldNameEndOfFrameAlign = "endOfFrameAlignEveryNFrames";
        
        /// <summary>
        /// 性能优化后的帧末对齐频率（每4帧执行一次，降低UI/瞄准开销）
        /// </summary>
        private const int OptimizedEndOfFrameAlignInterval = 4;
        
        /// <summary>
        /// 相机近裁剪面最小值（用于优化光栅化性能）
        /// </summary>
        private const float CameraNearClipMin = 0.05f;
        
        /// <summary>
        /// 相机视野角度最大值（用于优化光栅化性能）
        /// </summary>
        private const float CameraFovMax = 70f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 是否已在本会话中应用优化
        /// 用于确保优化只应用一次，避免重复设置
        /// </summary>
        private bool appliedInThisSession;
        
        /// <summary>
        /// endOfFrameAlignEveryNFrames字段的反射信息（延迟初始化）
        /// </summary>
        private FieldInfo endOfFrameAlignFieldInfo;
        #endregion

        #region Unity生命周期方法
        /// <summary>
        /// Unity Awake方法：初始化组件
        /// 使组件在场景切换时保持存活
        /// </summary>
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Unity Update方法：检查并应用性能优化
        /// 在第一人称模式下应用轻量级性能优化
        /// </summary>
        private void Update()
        {
            // 检查第一人称控制器是否存在
            var controller = FirstPersonCameraController.Instance;
            if (controller == null)
            {
                appliedInThisSession = false;
                return;
            }

            // 检查是否处于第一人称模式
            bool isFirstPersonMode = IsInFirstPersonMode(controller);
            if (!isFirstPersonMode)
            {
                appliedInThisSession = false;
                return;
            }

            // 如果尚未应用优化，则应用一次
            if (!appliedInThisSession)
            {
                ApplyPerformanceOptimizations(controller);
                appliedInThisSession = true;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查是否处于第一人称模式
        /// </summary>
        /// <param name="controller">第一人称相机控制器</param>
        /// <returns>如果处于第一人称模式则返回true，否则返回false</returns>
        private bool IsInFirstPersonMode(FirstPersonCameraController controller)
        {
            try
            {
                return controller.IsFirstPersonMode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 应用性能优化
        /// 包括：
        /// 1. 降低帧末对齐频率以降低UI/瞄准开销
        /// 2. 调整相机近裁剪面和FOV以优化光栅化性能
        /// </summary>
        /// <param name="controller">第一人称相机控制器</param>
        private void ApplyPerformanceOptimizations(FirstPersonCameraController controller)
        {
            // 优化1：降低帧末对齐频率
            OptimizeEndOfFrameAlignment(controller);

            // 优化2：调整相机设置
            OptimizeCameraSettings();
        }

        /// <summary>
        /// 优化帧末对齐频率
        /// 通过反射将endOfFrameAlignEveryNFrames设置为更大值，
        /// 降低UI/瞄准的每帧开销
        /// </summary>
        /// <param name="controller">第一人称相机控制器</param>
        private void OptimizeEndOfFrameAlignment(FirstPersonCameraController controller)
        {
            try
            {
                // 延迟获取字段信息
                if (endOfFrameAlignFieldInfo == null)
                {
                    endOfFrameAlignFieldInfo = typeof(FirstPersonCameraController)
                        .GetField(
                            ReflectionFieldNameEndOfFrameAlign,
                            BindingFlags.NonPublic | BindingFlags.Instance);
                }

                // 设置帧末对齐频率为每4帧执行一次（默认是2帧）
                if (endOfFrameAlignFieldInfo != null)
                {
                    endOfFrameAlignFieldInfo.SetValue(controller, OptimizedEndOfFrameAlignInterval);
                }
            }
            catch
            {
                // 优化失败，静默处理
            }
        }

        /// <summary>
        /// 优化相机设置
        /// 调整相机的近裁剪面和视野角度，以降低光栅化开销
        /// </summary>
        private void OptimizeCameraSettings()
        {
            try
            {
                var gameCamera = GameCamera.Instance;
                var camera = gameCamera != null ? gameCamera.renderCamera : null;
                
                if (camera != null)
                {
                    // 确保近裁剪面不小于最小值（优化光栅化性能）
                    if (camera.nearClipPlane < CameraNearClipMin)
                    {
                        camera.nearClipPlane = CameraNearClipMin;
                    }

                    // 限制视野角度不超过最大值（降低渲染开销）
                    if (camera.fieldOfView > CameraFovMax)
                    {
                        camera.fieldOfView = CameraFovMax;
                    }
                }
            }
            catch
            {
                // 优化失败，静默处理
            }
        }
        #endregion
    }
}


