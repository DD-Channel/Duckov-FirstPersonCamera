using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FirstPersonCamera.OptionsUI;
using FirstPersonCamera.Utilities;
using System.Reflection;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 渲染管理模块
    /// 负责在第一人称模式下优化渲染设置，包括：
    /// - 禁用第三人称的遮挡/透视效果
    /// - 调整渲染距离和裁剪面
    /// - 优化URP渲染管线设置
    /// - 控制准星虚化效果
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 选项键：渲染距离（引用OptionsUIConstants）
        /// </summary>
        private const string RenderDistanceKey = OptionsUIConstants.RenderDistanceKey;
        
        /// <summary>
        /// 渲染距离最小值（米）（引用OptionsUIConstants）
        /// </summary>
        private const float RenderDistanceMin = OptionsUIConstants.RenderDistanceMin;
        
        /// <summary>
        /// 渲染距离最大值（米）（引用OptionsUIConstants）
        /// </summary>
        private const float RenderDistanceMax = OptionsUIConstants.RenderDistanceMax;
        
        /// <summary>
        /// 渲染距离默认值（米）（引用OptionsUIConstants）
        /// </summary>
        private const float RenderDistanceDefault = OptionsUIConstants.RenderDistanceDefault;
        
        /// <summary>
        /// URP阴影距离上限（米）
        /// </summary>
        private const float UrpShadowDistanceMax = 100f;
        
        /// <summary>
        /// Unity图层数量
        /// </summary>
        private const int UnityLayerCount = 32;
        
        /// <summary>
        /// 未初始化的裁剪掩码值（用于标记未保存状态）
        /// </summary>
        private const int UninitializedCullingMask = -1;
        
        /// <summary>
        /// 未初始化的浮点值（用于标记未保存状态）
        /// </summary>
        private const float UninitializedFloat = -1f;
        
        /// <summary>
        /// 遮挡/透视相关的类型名称关键词数组
        /// 用于识别需要禁用的遮挡效果组件
        /// </summary>
        private static readonly string[] ObstructionTypeNameKeywords = new[]
        {
            "Obstruction", "Occlusion", "SeeThrough", "SeeThru", "Transparent", "Transparency",
            "Fade", "Fader", "Dither", "XRay", "WallFade", "ObstacleFade"
        };
        #endregion

        #region 私有字段
        /// <summary>
        /// 已禁用的遮挡行为组件列表（用于恢复）
        /// </summary>
        private readonly List<Behaviour> disabledObstructionBehaviours = new List<Behaviour>();
        
        /// <summary>
        /// 遮挡行为组件之前的启用状态列表（用于恢复）
        /// </summary>
        private readonly List<bool> disabledObstructionPrevEnabled = new List<bool>();
        
        /// <summary>
        /// 备份：相机裁剪掩码（用于恢复）
        /// </summary>
        private int prevCullingMask = UninitializedCullingMask;
        
        /// <summary>
        /// 备份：每层裁剪距离数组（用于恢复）
        /// </summary>
        private float[] prevLayerCullDistances;
        
        /// <summary>
        /// 备份：是否使用球形裁剪（用于恢复）
        /// </summary>
        private bool prevLayerCullSpherical;
        
        /// <summary>
        /// 备份：URP阴影距离（用于恢复）
        /// </summary>
        private float prevUrpShadowDistance = UninitializedFloat;
        
        /// <summary>
        /// 是否使用了后备渲染配置文件
        /// </summary>
        private bool usedFallbackProfile;
        
        /// <summary>
        /// 备份：相机远裁剪面（用于恢复）
        /// </summary>
        private float prevFarClip = UninitializedFloat;
        #endregion

        #region 渲染配置文件管理方法
        /// <summary>
        /// 应用第一人称渲染配置文件
        /// 为第一人称视图应用更轻量的渲染配置：
        /// - 排除不相关的图层并设置每层裁剪距离
        /// - 调整渲染距离和阴影距离
        /// - 禁用第三人称遮挡/透视效果（如果选项启用）
        /// - 控制准星虚化效果
        /// </summary>
        private void ApplyFpsCullingProfile()
        {
            if (mainCamera == null) return;

            // 备份当前相机设置
            BackupCameraSettings();

            // 应用URP优化设置
            ApplyUrpOptimizations();

            // 应用全局距离渲染限制
            ApplyRenderDistanceLimit();

            // 禁用第三人称遮挡/透视效果（如果选项启用）
            TryDisableObstructionSeeThrough();

            // 控制准星虚化效果
            TryControlAimOcclusionFade();
        }

        /// <summary>
        /// 备份相机设置
        /// </summary>
        private void BackupCameraSettings()
        {
            // 备份裁剪掩码
            if (prevCullingMask < 0)
            {
                prevCullingMask = mainCamera.cullingMask;
            }

            // 备份每层裁剪距离
            if (prevLayerCullDistances == null)
            {
                prevLayerCullDistances = new float[UnityLayerCount];
                try
                {
                    mainCamera.layerCullDistances.CopyTo(prevLayerCullDistances, 0);
                }
                catch
                {
                    // 备份失败，静默处理
                }
            }

            // 备份球形裁剪设置
            prevLayerCullSpherical = mainCamera.layerCullSpherical;
        }

        /// <summary>
        /// 应用URP优化设置
        /// 注意：不修改裁剪掩码或每层距离（mod无法确定层名，避免误伤Boss等）
        /// </summary>
        private void ApplyUrpOptimizations()
        {
            usedFallbackProfile = true;
            
            if (usedFallbackProfile)
            {
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urp != null)
                {
                    try
                    {
                        // 备份并限制阴影距离以提高性能
                        if (prevUrpShadowDistance < 0f)
                        {
                            prevUrpShadowDistance = urp.shadowDistance;
                        }
                        float targetShadowDistance = Mathf.Min(urp.shadowDistance, UrpShadowDistanceMax);
                        urp.shadowDistance = targetShadowDistance;
                    }
                    catch
                    {
                        // 设置失败，静默处理
                    }
                }

                // 启用球形裁剪（始终安全，不改变距离设置）
                mainCamera.layerCullSpherical = true;
            }
        }

        /// <summary>
        /// 应用全局距离渲染限制
        /// 限制远裁剪面，使超过指定距离的对象不被渲染
        /// </summary>
        private void ApplyRenderDistanceLimit()
        {
            try
            {
                // 备份远裁剪面
                if (prevFarClip < 0f)
                {
                    prevFarClip = mainCamera.farClipPlane;
                }

                // 从选项加载渲染距离，并限制在合理范围内
                float targetFarClip = Mathf.Clamp(
                    OptionsHelper.LoadFloatClamped(RenderDistanceKey, RenderDistanceDefault, RenderDistanceMin, RenderDistanceMax),
                    RenderDistanceMin,
                    RenderDistanceMax);
                mainCamera.farClipPlane = targetFarClip;
            }
            catch
            {
                // 设置失败，静默处理
            }
        }

        /// <summary>
        /// 尝试禁用第三人称遮挡/透视效果
        /// 根据选项设置决定是否禁用
        /// </summary>
        private void TryDisableObstructionSeeThrough()
        {
            try
            {
                int optionValue = OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableSeeThroughKey, 1);
                if (optionValue == 1)
                {
                    DisableObstructionSeeThroughEffects();
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 尝试控制准星虚化效果
        /// 根据选项设置决定是否禁用准星虚化
        /// </summary>
        private void TryControlAimOcclusionFade()
        {
            try
            {
                int optionValue = OptionsHelper.LoadInt(FirstPersonOptionsUI.DisableAimOcclusionFadeKey, 1);
                if (optionValue == 1)
                {
                    DisableAimOcclusionFade();
                }
                else
                {
                    RestoreAimOcclusionFade();
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 恢复渲染配置文件
        /// 在退出第一人称模式时调用，恢复所有被修改的渲染设置
        /// </summary>
        private void RestoreCullingProfile()
        {
            if (mainCamera == null) return;

            // 恢复相机裁剪设置
            RestoreCameraCullingSettings();

            // 恢复URP设置
            RestoreUrpSettings();

            // 恢复相机远裁剪面
            RestoreCameraFarClip();

            // 恢复第三人称遮挡处理器（如果之前被禁用）
            try
            {
                RestoreObstructionSeeThroughEffects();
            }
            catch
            {
                // 恢复失败，静默处理
            }
        }

        /// <summary>
        /// 恢复相机裁剪设置
        /// </summary>
        private void RestoreCameraCullingSettings()
        {
            // 恢复裁剪掩码
            if (prevCullingMask >= 0)
            {
                mainCamera.cullingMask = prevCullingMask;
                prevCullingMask = UninitializedCullingMask;
            }

            // 恢复每层裁剪距离
            if (prevLayerCullDistances != null)
            {
                try
                {
                    mainCamera.layerCullDistances = prevLayerCullDistances;
                }
                catch
                {
                    // 恢复失败，静默处理
                }
                prevLayerCullDistances = null;
            }

            // 恢复球形裁剪设置
            mainCamera.layerCullSpherical = prevLayerCullSpherical;
        }

        /// <summary>
        /// 恢复URP设置
        /// </summary>
        private void RestoreUrpSettings()
        {
            if (usedFallbackProfile)
            {
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urp != null && prevUrpShadowDistance > 0f)
                {
                    try
                    {
                        urp.shadowDistance = prevUrpShadowDistance;
                    }
                    catch
                    {
                        // 恢复失败，静默处理
                    }
                }
                prevUrpShadowDistance = UninitializedFloat;
                usedFallbackProfile = false;
            }
        }

        /// <summary>
        /// 恢复相机远裁剪面
        /// </summary>
        private void RestoreCameraFarClip()
        {
            try
            {
                if (mainCamera != null && prevFarClip > 0f)
                {
                    mainCamera.farClipPlane = prevFarClip;
                }
            }
            catch
            {
                // 恢复失败，静默处理
            }
            finally
            {
                prevFarClip = UninitializedFloat;
            }
        }
        #endregion

        #region 遮挡/透视效果管理方法
        /// <summary>
        /// 禁用遮挡/透视效果
        /// 在相机相关Transform下查找并禁用遮挡/透视相关的行为组件
        /// 限制搜索范围以避免误识别
        /// </summary>
        private void DisableObstructionSeeThroughEffects()
        {
            try
            {
                // 清空之前的缓存
                disabledObstructionBehaviours.Clear();
                disabledObstructionPrevEnabled.Clear();

                // 限制搜索范围到相机相关的Transform，避免误识别
                var searchRoots = GetCameraRelatedRoots();

                // 在每个根节点下搜索并禁用遮挡效果组件
                foreach (var root in searchRoots)
                {
                    if (root == null) continue;
                    ProcessObstructionBehavioursInTransform(root);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 获取相机相关的根Transform列表
        /// </summary>
        /// <returns>相机相关的Transform列表</returns>
        private List<Transform> GetCameraRelatedRoots()
        {
            var roots = new List<Transform>();
            if (mainCamera != null)
            {
                roots.Add(mainCamera.transform);
            }
            if (cameraArm != null)
            {
                roots.Add(cameraArm.transform);
            }
            if (gameCamera != null)
            {
                roots.Add(gameCamera.transform);
            }
            return roots;
        }

        /// <summary>
        /// 处理指定Transform下的遮挡行为组件
        /// </summary>
        /// <param name="root">根Transform</param>
        private void ProcessObstructionBehavioursInTransform(Transform root)
        {
            var behaviours = root.GetComponentsInChildren<Behaviour>(true);
            
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;

                // 跳过mod自身的组件
                var type = behaviour.GetType();
                if (type.Namespace != null && type.Namespace.StartsWith("FirstPersonCamera"))
                {
                    continue;
                }

                // 检查类型名称是否匹配遮挡关键词
                string typeName = type.FullName ?? type.Name;
                if (!MatchesObstructionKeywords(typeName))
                {
                    continue;
                }

                // 禁用组件并保存状态
                disabledObstructionBehaviours.Add(behaviour);
                disabledObstructionPrevEnabled.Add(behaviour.enabled);
                
                if (behaviour.enabled)
                {
                    behaviour.enabled = false;
                }
            }
        }

        /// <summary>
        /// 检查类型名称是否匹配遮挡关键词
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>如果匹配则返回true，否则返回false</returns>
        private bool MatchesObstructionKeywords(string typeName)
        {
            foreach (var keyword in ObstructionTypeNameKeywords)
            {
                if (typeName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 恢复遮挡/透视效果
        /// 恢复所有被禁用的遮挡/透视行为组件
        /// </summary>
        private void RestoreObstructionSeeThroughEffects()
        {
            try
            {
                for (int i = 0; i < disabledObstructionBehaviours.Count; i++)
                {
                    var behaviour = disabledObstructionBehaviours[i];
                    if (behaviour == null) continue;

                    // 恢复之前的启用状态（如果保存了状态，则使用保存的状态，否则保持当前状态）
                    bool previousEnabled = i < disabledObstructionPrevEnabled.Count
                        ? disabledObstructionPrevEnabled[i]
                        : behaviour.enabled;
                    behaviour.enabled = previousEnabled;
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
            finally
            {
                // 清空缓存列表
                disabledObstructionBehaviours.Clear();
                disabledObstructionPrevEnabled.Clear();
            }
        }
        #endregion

        #region 准星虚化控制方法
        /// <summary>
        /// 禁用准星虚化效果（只禁用准星相关的虚化，保留房屋虚化）
        /// 通过Harmony补丁阻止OcclusionFadeManager更新准星相关的着色器属性
        /// 不修改任何着色器属性，只阻止更新
        /// </summary>
        private void DisableAimOcclusionFade()
        {
            try
            {
                // 使用Harmony补丁系统，只阻止准星相关的着色器属性更新
                // 这样不会影响其他着色器功能（颜色、渲染等）
                // 避免直接修改着色器；改为禁用遮挡相关的行为组件
                ForceDisableCrosshairSeeThroughEffects();
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 恢复准星虚化效果
        /// 恢复之前被禁用的遮挡相关行为组件
        /// </summary>
        private void RestoreAimOcclusionFade()
        {
            try
            {
                // 恢复之前被禁用的遮挡相关行为组件
                RestoreCrosshairSeeThroughEffects();
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion
    }
}

