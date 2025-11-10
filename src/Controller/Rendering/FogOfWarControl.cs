using System;
using System.Reflection;
using UnityEngine;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    /// <summary>
    /// 战争迷雾控制模块
    /// 在第一人称模式下控制战争迷雾的显示/隐藏
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 战争迷雾控制字段
        /// <summary>
        /// FogOfWarManager 实例（通过反射获取）
        /// </summary>
        private MonoBehaviour fogOfWarManager;
        
        /// <summary>
        /// FogOfWarManager 的 allVision 字段（通过反射获取）
        /// </summary>
        private FieldInfo allVisionField;
        
        /// <summary>
        /// 原始 allVision 值（用于恢复）
        /// </summary>
        private bool? originalAllVision;
        
        /// <summary>
        /// 是否已初始化战争迷雾控制
        /// </summary>
        private bool fogOfWarControlInitialized;
        #endregion
        
        #region 战争迷雾控制方法
        /// <summary>
        /// 初始化战争迷雾控制
        /// </summary>
        private void InitializeFogOfWarControl()
        {
            if (fogOfWarControlInitialized)
            {
                return;
            }
            
            try
            {
                // 在所有 MonoBehaviour 中查找 FogOfWarManager
                MonoBehaviour[] allMonoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();
                fogOfWarManager = null;
                
                foreach (MonoBehaviour mb in allMonoBehaviours)
                {
                    if (mb != null && mb.GetType().Name == "FogOfWarManager")
                    {
                        fogOfWarManager = mb;
                        break;
                    }
                }
                
                if (fogOfWarManager == null)
                {
                    FPLogger.Log("未找到 FogOfWarManager，跳过战争迷雾控制初始化");
                    fogOfWarControlInitialized = true;
                    return;
                }
                
                // 获取类型
                Type fogOfWarManagerType = fogOfWarManager.GetType();
                
                // 获取 allVision 字段
                allVisionField = fogOfWarManagerType.GetField("allVision", 
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                
                if (allVisionField == null)
                {
                    FPLogger.LogWarning("未找到 FogOfWarManager.allVision 字段，跳过战争迷雾控制初始化");
                    fogOfWarControlInitialized = true;
                    return;
                }
                
                fogOfWarControlInitialized = true;
                FPLogger.Log("战争迷雾控制初始化成功");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "初始化战争迷雾控制失败");
                fogOfWarControlInitialized = true;
            }
        }
        
        /// <summary>
        /// 更新战争迷雾状态（在第一人称模式切换时调用）
        /// </summary>
        private void UpdateFogOfWarState()
        {
            // 如果未初始化，尝试初始化
            if (!fogOfWarControlInitialized)
            {
                InitializeFogOfWarControl();
            }
            
            // 如果初始化失败，直接返回
            if (fogOfWarManager == null || allVisionField == null)
            {
                return;
            }
            
            try
            {
                // 检查设置：是否启用去除战争迷雾
                bool disableFogOfWar = OptionsHelper.LoadInt(OptionsUIConstants.DisableFogOfWarKey, 0) == 1;
                
                // 仅在第一人称模式下应用
                if (isFirstPersonMode && disableFogOfWar)
                {
                    // 保存原始值（如果还没有保存）
                    if (originalAllVision == null)
                    {
                        originalAllVision = (bool)allVisionField.GetValue(fogOfWarManager);
                    }
                    
                    // 设置全视野（去除战争迷雾）
                    allVisionField.SetValue(fogOfWarManager, true);
                }
                else
                {
                    // 恢复原始值
                    if (originalAllVision != null)
                    {
                        allVisionField.SetValue(fogOfWarManager, originalAllVision.Value);
                        originalAllVision = null;
                    }
                }
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "更新战争迷雾状态失败");
            }
        }
        
        /// <summary>
        /// 清理战争迷雾控制（在禁用第一人称模式时调用）
        /// </summary>
        private void CleanupFogOfWarControl()
        {
            try
            {
                // 恢复原始值
                if (fogOfWarManager != null && allVisionField != null && originalAllVision != null)
                {
                    allVisionField.SetValue(fogOfWarManager, originalAllVision.Value);
                    originalAllVision = null;
                }
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "清理战争迷雾控制失败");
            }
        }
        #endregion
    }
}

