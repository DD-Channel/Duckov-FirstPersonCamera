// 禁用所有着色器/材质Harmony钩子，避免修改着色器
#define DISABLE_SHADER_PATCHES

using System;
using HarmonyLib;
using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 遮挡效果控制辅助类
    /// 提供用于识别和控制遮挡相关着色器属性、关键字和全局变量的方法
    /// </summary>
    internal static class OcclusionShim
    {
        #region 私有字段
        /// <summary>
        /// 是否完全阻止遮挡效果（所有OC_*属性）
        /// </summary>
        private static volatile bool blocked;
        
        /// <summary>
        /// 是否只阻止准星虚化效果（仅OC_AimViewDir和OC_AimPos）
        /// </summary>
        private static volatile bool aimOcclusionBlocked;
        #endregion

        #region 公共属性
        /// <summary>
        /// 是否完全阻止遮挡效果
        /// </summary>
        public static bool IsBlocked => blocked;
        
        /// <summary>
        /// 是否只阻止准星虚化效果
        /// </summary>
        public static bool IsAimOcclusionBlocked => aimOcclusionBlocked;
        #endregion

        #region 公共方法
        /// <summary>
        /// 设置是否完全阻止遮挡效果
        /// </summary>
        /// <param name="on">是否阻止</param>
        public static void SetBlocked(bool on)
        {
            blocked = on;
        }

        /// <summary>
        /// 设置是否只阻止准星虚化效果
        /// </summary>
        /// <param name="on">是否阻止</param>
        public static void SetAimOcclusionBlocked(bool on)
        {
            aimOcclusionBlocked = on;
        }

        /// <summary>
        /// 检查是否是遮挡相关的着色器关键字
        /// </summary>
        /// <param name="keyword">关键字名称</param>
        /// <returns>如果是遮挡关键字则返回true，否则返回false</returns>
        public static bool IsOcclusionKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return false;
            }

            var lowerKeyword = keyword.ToLowerInvariant();
            return lowerKeyword.Contains("occlusion") ||
                   lowerKeyword.Contains("see_through") ||
                   lowerKeyword.Contains("see-through") ||
                   lowerKeyword.Contains("aim_occlusion");
        }

        /// <summary>
        /// 检查是否是遮挡相关的着色器全局变量
        /// </summary>
        /// <param name="name">全局变量名称</param>
        /// <returns>如果是遮挡全局变量则返回true，否则返回false</returns>
        public static bool IsOcclusionGlobal(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // 常见的OC_*着色器全局变量（来自游戏）
            if (name.StartsWith("OC_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 后备匹配
            var lowerName = name.ToLowerInvariant();
            return lowerName.Contains("occlusion") ||
                   lowerName.Contains("see_through") ||
                   lowerName.Contains("see-through");
        }

        /// <summary>
        /// 检查是否是准星相关的着色器属性（只阻止这些，保留房屋虚化）
        /// </summary>
        /// <param name="name">全局变量名称</param>
        /// <returns>如果是准星遮挡属性则返回true，否则返回false</returns>
        public static bool IsAimOcclusionGlobal(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // 只阻止准星相关的属性
            return name.Equals("OC_AimViewDir", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("OC_AimPos", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查是否是遮挡相关的材质属性
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <returns>如果是遮挡属性则返回true，否则返回false</returns>
        public static bool IsOcclusionProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var lowerName = name.ToLowerInvariant();
            
            // 常见用于穿透/虚化的属性名关键词
            return lowerName.Contains("occlusion") ||
                   lowerName.Contains("seethrough") ||
                   lowerName.Contains("see_through") ||
                   lowerName.Contains("see-through") ||
                   lowerName.Contains("dither") ||
                   lowerName.Contains("fade") ||
                   lowerName.Contains("transpar") ||
                   lowerName.Contains("cutoff") ||
                   lowerName.Contains("xray") ||
                   lowerName.Contains("obstruction");
        }
        #endregion
    }

    // 当DISABLE_SHADER_PATCHES被定义时，跳过安装任何Harmony补丁
#if !DISABLE_SHADER_PATCHES
    /// <summary>
    /// Harmony补丁 - 着色器全局向量设置
    /// 阻止游戏更新OC_*着色器向量（当被阻止时）
    /// </summary>
    [HarmonyPatch(typeof(Shader), nameof(Shader.SetGlobalVector), new Type[] { typeof(string), typeof(Vector4) })]
    internal static class Patch_Shader_SetGlobalVector
    {
        /// <summary>
        /// SetGlobalVector的前置补丁
        /// </summary>
        /// <param name="name">全局变量名称</param>
        /// <param name="value">向量值</param>
        /// <returns>如果返回false则阻止设置，如果返回true则允许设置</returns>
        static bool Prefix(string name, Vector4 value)
        {
            try
            {
                // 如果启用了准星虚化阻止，只阻止准星相关的属性
                if (OcclusionShim.IsAimOcclusionBlocked && OcclusionShim.IsAimOcclusionGlobal(name))
                {
                    return false;
                }

                // 如果启用了完全阻止，阻止所有OC_*属性
                if (OcclusionShim.IsBlocked && OcclusionShim.IsOcclusionGlobal(name))
                {
                    return false;
                }
            }
            catch
            {
                // 处理失败，允许设置
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony补丁 - 着色器全局浮点数设置
    /// 阻止游戏更新OC_*着色器浮点数（当被阻止时）
    /// </summary>
    [HarmonyPatch(typeof(Shader), nameof(Shader.SetGlobalFloat), new Type[] { typeof(string), typeof(float) })]
    internal static class Patch_Shader_SetGlobalFloat
    {
        /// <summary>
        /// SetGlobalFloat的前置补丁
        /// </summary>
        /// <param name="name">全局变量名称</param>
        /// <param name="value">浮点数值</param>
        /// <returns>如果返回false则阻止设置，如果返回true则允许设置</returns>
        static bool Prefix(string name, float value)
        {
            try
            {
                if (OcclusionShim.IsBlocked && OcclusionShim.IsOcclusionGlobal(name))
                {
                    return false;
                }
            }
            catch
            {
                // 处理失败，允许设置
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony补丁 - 着色器关键字启用
    /// 阻止启用遮挡关键字（当被阻止时），禁用更昂贵的渲染通道
    /// </summary>
    [HarmonyPatch(typeof(Shader), nameof(Shader.EnableKeyword), new Type[] { typeof(string) })]
    internal static class Patch_Shader_EnableKeyword
    {
        /// <summary>
        /// EnableKeyword的前置补丁
        /// </summary>
        /// <param name="keyword">关键字名称</param>
        /// <returns>如果返回false则阻止启用，如果返回true则允许启用</returns>
        static bool Prefix(string keyword)
        {
            try
            {
                if (OcclusionShim.IsBlocked && OcclusionShim.IsOcclusionKeyword(keyword))
                {
                    return false;
                }
            }
            catch
            {
                // 处理失败，允许启用
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony补丁 - 材质浮点数属性设置
    /// 阻止更新可能驱动第一人称透视/虚化的材质属性（当被阻止时）
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.SetFloat), new Type[] { typeof(string), typeof(float) })]
    internal static class Patch_Material_SetFloat
    {
        /// <summary>
        /// SetFloat的前置补丁
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="value">浮点数值</param>
        /// <returns>如果返回false则阻止设置，如果返回true则允许设置</returns>
        static bool Prefix(string name, float value)
        {
            try
            {
                if (OcclusionShim.IsBlocked && OcclusionShim.IsOcclusionProperty(name))
                {
                    return false;
                }
            }
            catch
            {
                // 处理失败，允许设置
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony补丁 - 材质整数属性设置
    /// 阻止更新可能驱动第一人称透视/虚化的材质属性（当被阻止时）
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.SetInt), new Type[] { typeof(string), typeof(int) })]
    internal static class Patch_Material_SetInt
    {
        /// <summary>
        /// SetInt的前置补丁
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="value">整数值</param>
        /// <returns>如果返回false则阻止设置，如果返回true则允许设置</returns>
        static bool Prefix(string name, int value)
        {
            try
            {
                if (OcclusionShim.IsBlocked && OcclusionShim.IsOcclusionProperty(name))
                {
                    return false;
                }
            }
            catch
            {
                // 处理失败，允许设置
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony补丁 - 材质关键字启用
    /// 阻止启用遮挡关键字（当被阻止时）
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.EnableKeyword), new Type[] { typeof(string) })]
    internal static class Patch_Material_EnableKeyword
    {
        /// <summary>
        /// EnableKeyword的前置补丁
        /// </summary>
        /// <param name="keyword">关键字名称</param>
        /// <returns>如果返回false则阻止启用，如果返回true则允许启用</returns>
        static bool Prefix(string keyword)
        {
            try
            {
                if (OcclusionShim.IsBlocked && OcclusionShim.IsOcclusionKeyword(keyword))
                {
                    return false;
                }
            }
            catch
            {
                // 处理失败，允许启用
            }

            return true;
        }
    }
#endif
}

