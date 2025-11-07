using HarmonyLib;
using UnityEngine;
using Duckov.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 弹出文本颜色修改
    /// 将暴击飘字的颜色统一改成红色，提高视觉识别度
    /// </summary>
    [HarmonyPatch(typeof(FX.PopText))]
    [HarmonyPatch("Pop")]
    internal static class PopTextColorPatch
    {
        #region 常量定义
        /// <summary>
        /// 暴击文本颜色（红色）
        /// </summary>
        private static readonly Color CritTextColor = Color.red;
        #endregion

        #region Harmony补丁方法
        /// <summary>
        /// 弹出文本的前置补丁
        /// 检测是否为暴击文本，如果是则将其颜色改为红色
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="color">文本颜色（引用参数，可修改）</param>
        /// <param name="size">文本大小</param>
        /// <param name="sprite">文本精灵（可选）</param>
        public static void Prefix(
            string text,
            Vector3 worldPosition,
            ref Color color,
            float size,
            Sprite sprite = null)
        {
            try
            {
                // 检查是否为暴击文本精灵
                var critSprite = GameplayDataSettings.UIStyle.CritPopSprite;
                if (sprite != null && critSprite != null && ReferenceEquals(sprite, critSprite))
                {
                    // 将暴击文本颜色统一改为红色
                    color = CritTextColor;
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }
        #endregion
    }
}

