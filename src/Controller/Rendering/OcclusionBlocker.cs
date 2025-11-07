using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 遮挡效果禁用模块
    /// 负责在第一人称模式下禁用游戏的原始遮挡/透视效果，
    /// 避免与第一人称相机系统产生冲突
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 遮挡扫描冷却时间（秒）
        /// 避免频繁进行全场景扫描，提高性能
        /// </summary>
        private const float OcclusionScanCooldown = 5f;
        
        /// <summary>
        /// 初始扫描时间戳（用于首次扫描）
        /// </summary>
        private const float InitialScanTime = -999f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 已禁用的遮挡行为组件列表（用于恢复）
        /// </summary>
        private readonly List<Behaviour> disabledOcclusionBehaviours = new List<Behaviour>();
        
        /// <summary>
        /// 上次执行遮挡扫描的时间戳
        /// </summary>
        private float lastOcclusionScanTime = InitialScanTime;
        
        /// <summary>
        /// 是否已缓存遮挡行为组件
        /// 如果已缓存，则只需检查缓存的组件，无需全场景扫描
        /// </summary>
        private bool hasCachedOcclusionBehaviours;
        #endregion

        #region 遮挡效果禁用方法
        /// <summary>
        /// 强制禁用准星透视效果
        /// 在第一人称模式下，每帧确保游戏的原始遮挡/透视效果被禁用
        /// 使用缓存机制避免频繁的全场景扫描
        /// </summary>
        private void ForceDisableCrosshairSeeThroughEffects()
        {
            try
            {
                // 如果已有缓存的组件，只需确保它们保持禁用状态（性能开销小）
                if (hasCachedOcclusionBehaviours && disabledOcclusionBehaviours.Count > 0)
                {
                    EnsureCachedBehavioursDisabled();
                    return;
                }

                // 限流的全场景扫描：仅在冷却时间后执行，以发现新的遮挡行为组件
                if (!ShouldPerformFullScan())
                {
                    return;
                }

                lastOcclusionScanTime = Time.unscaledTime;
                PerformFullSceneScan();
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 确保缓存的遮挡行为组件保持禁用状态
        /// </summary>
        private void EnsureCachedBehavioursDisabled()
        {
            for (int i = 0; i < disabledOcclusionBehaviours.Count; i++)
            {
                var behaviour = disabledOcclusionBehaviours[i];
                if (behaviour == null) continue;

                try
                {
                    if (behaviour.enabled)
                    {
                        behaviour.enabled = false;
                    }
                }
                catch
                {
                    // 禁用失败，静默处理
                }
            }
        }

        /// <summary>
        /// 判断是否应该执行全场景扫描
        /// </summary>
        /// <returns>如果应该执行扫描则返回true，否则返回false</returns>
        private bool ShouldPerformFullScan()
        {
            return Time.unscaledTime - lastOcclusionScanTime >= OcclusionScanCooldown;
        }

        /// <summary>
        /// 执行全场景扫描，查找并禁用遮挡效果组件
        /// </summary>
        private void PerformFullSceneScan()
        {
            var behaviours = UnityEngine.Object.FindObjectsOfType<Behaviour>(true);
            
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                if (!behaviour.enabled) continue;

                // 跳过自身
                if (behaviour is FirstPersonCameraController) continue;

                // 检查类型名称是否看起来像遮挡效果组件
                if (!IsOcclusionBehaviour(behaviour)) continue;

                // 禁用组件并添加到缓存列表
                try
                {
                    behaviour.enabled = false;
                    disabledOcclusionBehaviours.Add(behaviour);
                }
                catch
                {
                    // 禁用失败，静默处理
                }
            }

            // 更新缓存标志
            hasCachedOcclusionBehaviours = disabledOcclusionBehaviours.Count > 0;
        }

        /// <summary>
        /// 检查指定的Behaviour是否看起来像遮挡效果组件
        /// 通过分析类型名称来判断（使用关键词匹配）
        /// </summary>
        /// <param name="behaviour">要检查的Behaviour组件</param>
        /// <returns>如果看起来像遮挡效果组件则返回true，否则返回false</returns>
        private bool IsOcclusionBehaviour(Behaviour behaviour)
        {
            if (behaviour == null) return false;

            var typeName = behaviour.GetType().Name;
            if (string.IsNullOrEmpty(typeName)) return false;

            var lowerTypeName = typeName.ToLowerInvariant();

            // 检查是否包含遮挡相关的关键词
            return ContainsOcclusionKeywords(lowerTypeName);
        }

        /// <summary>
        /// 检查类型名称是否包含遮挡相关的关键词
        /// </summary>
        /// <param name="lowerTypeName">小写的类型名称</param>
        /// <returns>如果包含遮挡关键词则返回true，否则返回false</returns>
        private bool ContainsOcclusionKeywords(string lowerTypeName)
        {
            // 直接匹配的关键词
            if (lowerTypeName.Contains("occlusion") ||
                lowerTypeName.Contains("xray") ||
                lowerTypeName.Contains("obstruction") ||
                lowerTypeName.Contains("transparent") ||
                lowerTypeName.Contains("aimocclusion") ||
                lowerTypeName.Contains("cameraclipping") ||
                lowerTypeName.Contains("clipavoid"))
            {
                return true;
            }

            // 组合匹配：see + through/thru
            if (lowerTypeName.Contains("see") &&
                (lowerTypeName.Contains("through") || lowerTypeName.Contains("thru")))
            {
                return true;
            }

            // 组合匹配：wall + fade/transparen/see
            if (lowerTypeName.Contains("wall") &&
                (lowerTypeName.Contains("fade") ||
                 lowerTypeName.Contains("transparen") ||
                 lowerTypeName.Contains("see")))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 确保着色器遮挡效果关闭
        /// 注意：此方法故意留空，因为我们不再修改着色器关键字/全局变量
        /// 保留此方法是为了向后兼容性
        /// </summary>
        private void EnsureShaderOcclusionOff()
        {
            // 故意留空：我们不再修改着色器关键字/全局变量
        }
        #endregion

        #region 恢复遮挡效果方法
        /// <summary>
        /// 恢复准星透视效果
        /// 在退出第一人称模式时调用，恢复所有被禁用的遮挡效果组件
        /// </summary>
        private void RestoreCrosshairSeeThroughEffects()
        {
            try
            {
                foreach (var behaviour in disabledOcclusionBehaviours)
                {
                    if (behaviour != null)
                    {
                        try
                        {
                            behaviour.enabled = true;
                        }
                        catch
                        {
                            // 恢复失败，静默处理
                        }
                    }
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
            finally
            {
                // 清空缓存列表
                disabledOcclusionBehaviours.Clear();
                hasCachedOcclusionBehaviours = false;
            }
        }
        #endregion
    }
}
