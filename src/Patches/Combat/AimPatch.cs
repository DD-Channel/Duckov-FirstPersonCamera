using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// Harmony补丁 - 瞄准输入管理
    /// 在第一人称模式下拦截游戏的瞄准输入设置，避免与第一人称相机控制器产生冲突
    /// 确保准星和瞄准完全由FirstPersonCameraController管理，避免闪烁和争用
    /// </summary>
    [HarmonyPatch(typeof(InputManager))]
    [HarmonyPatch("SetAimInputUsingMouse")]
    internal static class AimPatch
    {
        #region 私有字段
        /// <summary>
        /// 是否已初始化
        /// </summary>
        private static bool initialized;
        
        /// <summary>
        /// 屏幕中心点（用于瞄准检查）
        /// </summary>
        private static Vector2 screenCenter;
        
        /// <summary>
        /// 瞄准检查图层掩码（用于射线检测）
        /// </summary>
        private static int aimCheckLayerMask;
        
        /// <summary>
        /// InputManager.inputAimPoint字段的反射信息
        /// </summary>
        private static FieldInfo inputAimPointField;
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查第一人称模式是否激活
        /// </summary>
        /// <returns>如果第一人称模式激活则返回true，否则返回false</returns>
        private static bool IsFirstPersonActive()
        {
            try
            {
                return FirstPersonCameraController.Instance != null && 
                       FirstPersonCameraController.Instance.IsFirstPersonMode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 初始化瞄准检查相关设置
        /// </summary>
        private static void InitializeAimCheck()
        {
            // 计算屏幕中心
            screenCenter.x = Screen.width * 0.5f;
            screenCenter.y = Screen.height * 0.5f;
            
            // 构建瞄准检查图层掩码
            aimCheckLayerMask = BuildAimCheckLayerMask();
            
            // 获取InputManager的inputAimPoint字段
            inputAimPointField = typeof(InputManager).GetField(
                "inputAimPoint",
                BindingFlags.Instance | BindingFlags.NonPublic);
            
            initialized = true;
        }

        /// <summary>
        /// 构建瞄准检查图层掩码
        /// </summary>
        /// <returns>图层掩码值</returns>
        private static int BuildAimCheckLayerMask()
        {
            int mask = 0;
            
            // 添加各种阻挡图层
            int layer;
            layer = LayerMask.NameToLayer("FowBlock");
            if (layer >= 0) mask |= (1 << layer);
            
            layer = LayerMask.NameToLayer("Ground");
            if (layer >= 0) mask |= (1 << layer);
            
            layer = LayerMask.NameToLayer("Wall_FowBlock");
            if (layer >= 0) mask |= (1 << layer);
            
            layer = LayerMask.NameToLayer("Door");
            if (layer >= 0) mask |= (1 << layer);
            
            layer = LayerMask.NameToLayer("HeadCollider");
            if (layer >= 0) mask |= (1 << layer);
            
            layer = LayerMask.NameToLayer("HalfObsticle");
            if (layer >= 0) mask |= (1 << layer);
            
            return mask;
        }
        #endregion

        #region Harmony补丁方法
        /// <summary>
        /// 瞄准输入设置的前置补丁
        /// 在第一人称模式下拦截原逻辑，完全交由FirstPersonCameraController进行瞄准与准星管理
        /// </summary>
        /// <param name="mouseDelta">鼠标增量（引用参数）</param>
        /// <returns>如果返回false则跳过原方法，如果返回true则执行原方法</returns>
        public static bool Prefix(ref Vector2 mouseDelta)
        {
            // 延迟初始化
            if (!initialized)
            {
                InitializeAimCheck();
            }

            // 仅在第一人称模式下拦截原逻辑
            if (!IsFirstPersonActive())
            {
                // 第三人称：不干预，走原始逻辑
                return true;
            }

            // 第一人称：跳过原方法，避免与控制器重复设置导致闪烁/争用
            return false;
        }

        /// <summary>
        /// 瞄准输入设置的后置补丁
        /// 第一人称模式下我们跳过原始方法，准星/瞄准完全由FirstPersonCameraController更新
        /// </summary>
        /// <param name="__instance">InputManager实例</param>
        public static void Postfix(InputManager __instance)
        {
            // 第一人称模式下我们跳过原始方法，准星/瞄准完全由FirstPersonCameraController更新
            // 第三人称模式保留原方法行为，因此此处不做任何修改，避免造成闪烁与准星错乱
            if (IsFirstPersonActive())
            {
                return;
            }
        }
        #endregion
    }
}

