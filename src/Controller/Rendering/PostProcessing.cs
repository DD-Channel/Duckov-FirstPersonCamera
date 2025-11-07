using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 后处理模糊效果管理模块
    /// 负责在第一人称模式下禁用后处理中的模糊效果（景深和运动模糊），
    /// 以提供更清晰的视野，并在退出第一人称模式时恢复这些效果
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 私有字段
        /// <summary>
        /// 已禁用的景深效果组件列表（用于恢复）
        /// </summary>
        private readonly List<DepthOfField> disabledDofs = new List<DepthOfField>();
        
        /// <summary>
        /// 景深效果组件之前的激活状态列表（用于恢复）
        /// </summary>
        private readonly List<bool> disabledDofsPrevActive = new List<bool>();
        
        /// <summary>
        /// 已禁用的运动模糊效果组件列表（用于恢复）
        /// </summary>
        private readonly List<MotionBlur> disabledMotionBlurs = new List<MotionBlur>();
        
        /// <summary>
        /// 运动模糊效果组件之前的激活状态列表（用于恢复）
        /// </summary>
        private readonly List<bool> disabledMotionPrevActive = new List<bool>();
        #endregion

        #region 后处理模糊效果管理方法
        /// <summary>
        /// 禁用后处理模糊效果
        /// 在第一人称模式下禁用所有Volume中的景深和运动模糊效果，
        /// 以提供更清晰的视野
        /// </summary>
        private void DisablePostProcessingBlurEffects()
        {
            try
            {
                // 清空之前的缓存
                disabledDofs.Clear();
                disabledDofsPrevActive.Clear();
                disabledMotionBlurs.Clear();
                disabledMotionPrevActive.Clear();

                // 查找场景中所有Volume组件
                var volumes = UnityEngine.Object.FindObjectsOfType<Volume>(true);
                
                foreach (var volume in volumes)
                {
                    if (volume == null || volume.profile == null) continue;

                    // 处理景深效果
                    ProcessDepthOfField(volume);

                    // 处理运动模糊效果
                    ProcessMotionBlur(volume);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 处理Volume中的景深效果
        /// </summary>
        /// <param name="volume">要处理的Volume组件</param>
        private void ProcessDepthOfField(Volume volume)
        {
            if (volume.profile.TryGet<DepthOfField>(out var dof) && dof != null)
            {
                // 保存当前激活状态
                disabledDofs.Add(dof);
                disabledDofsPrevActive.Add(dof.active);
                
                // 如果当前激活，则禁用
                if (dof.active)
                {
                    dof.active = false;
                }
            }
        }

        /// <summary>
        /// 处理Volume中的运动模糊效果
        /// </summary>
        /// <param name="volume">要处理的Volume组件</param>
        private void ProcessMotionBlur(Volume volume)
        {
            if (volume.profile.TryGet<MotionBlur>(out var motionBlur) && motionBlur != null)
            {
                // 保存当前激活状态
                disabledMotionBlurs.Add(motionBlur);
                disabledMotionPrevActive.Add(motionBlur.active);
                
                // 如果当前激活，则禁用
                if (motionBlur.active)
                {
                    motionBlur.active = false;
                }
            }
        }

        /// <summary>
        /// 恢复后处理模糊效果
        /// 在退出第一人称模式时调用，恢复所有被禁用的景深和运动模糊效果
        /// </summary>
        private void RestorePostProcessingBlurEffects()
        {
            try
            {
                // 恢复景深效果
                RestoreDepthOfFieldEffects();

                // 恢复运动模糊效果
                RestoreMotionBlurEffects();
            }
            catch
            {
                // 处理失败，静默处理
            }
            finally
            {
                // 清空所有缓存列表
                disabledDofs.Clear();
                disabledDofsPrevActive.Clear();
                disabledMotionBlurs.Clear();
                disabledMotionPrevActive.Clear();
            }
        }

        /// <summary>
        /// 恢复景深效果
        /// </summary>
        private void RestoreDepthOfFieldEffects()
        {
            for (int i = 0; i < disabledDofs.Count; i++)
            {
                var dof = disabledDofs[i];
                if (dof == null) continue;

                // 恢复之前的激活状态（如果保存了状态，则使用保存的状态，否则保持当前状态）
                bool previousState = i < disabledDofsPrevActive.Count 
                    ? disabledDofsPrevActive[i] 
                    : dof.active;
                dof.active = previousState;
            }
        }

        /// <summary>
        /// 恢复运动模糊效果
        /// </summary>
        private void RestoreMotionBlurEffects()
        {
            for (int i = 0; i < disabledMotionBlurs.Count; i++)
            {
                var motionBlur = disabledMotionBlurs[i];
                if (motionBlur == null) continue;

                // 恢复之前的激活状态（如果保存了状态，则使用保存的状态，否则保持当前状态）
                bool previousState = i < disabledMotionPrevActive.Count 
                    ? disabledMotionPrevActive[i] 
                    : motionBlur.active;
                motionBlur.active = previousState;
            }
        }
        #endregion
    }
}

