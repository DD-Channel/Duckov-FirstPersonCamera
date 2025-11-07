using Duckov.Options;
using UnityEngine;

namespace FirstPersonCamera.Utilities
{
    /// <summary>
    /// 选项加载辅助类
    /// 提供统一的选项加载方法，减少重复代码并统一错误处理
    /// 现在使用独立的配置文件系统
    /// </summary>
    public static class OptionsHelper
    {
        /// <summary>
        /// 安全加载整数选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的整数值，如果失败则返回默认值或保持当前值</returns>
        public static int LoadInt(string key, int defaultValue)
        {
            try
            {
                // 使用独立的配置文件系统，加载失败时保持当前值
                return ConfigManager.Load<int>(key, defaultValue, keepCurrentOnError: true);
            }
            catch
            {
                // 如果ConfigManager也失败，尝试从当前值缓存获取
                return ConfigManager.Load<int>(key, defaultValue, keepCurrentOnError: true);
            }
        }

        /// <summary>
        /// 安全加载浮点数选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的浮点数值，如果失败则返回默认值或保持当前值</returns>
        public static float LoadFloat(string key, float defaultValue)
        {
            try
            {
                // 使用独立的配置文件系统，加载失败时保持当前值
                return ConfigManager.Load<float>(key, defaultValue, keepCurrentOnError: true);
            }
            catch
            {
                // 如果ConfigManager也失败，尝试从当前值缓存获取
                return ConfigManager.Load<float>(key, defaultValue, keepCurrentOnError: true);
            }
        }

        /// <summary>
        /// 安全加载并限制范围的浮点数选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>加载并限制后的浮点数值</returns>
        public static float LoadFloatClamped(string key, float defaultValue, float min, float max)
        {
            try
            {
                float value = ConfigManager.Load<float>(key, defaultValue, keepCurrentOnError: true);
                return Mathf.Clamp(value, min, max);
            }
            catch
            {
                return Mathf.Clamp(defaultValue, min, max);
            }
        }

        /// <summary>
        /// 安全加载并限制在0-1范围的浮点数选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载并限制后的浮点数值（0-1）</returns>
        public static float LoadFloatClamped01(string key, float defaultValue)
        {
            try
            {
                float value = ConfigManager.Load<float>(key, defaultValue, keepCurrentOnError: true);
                return Mathf.Clamp01(value);
            }
            catch
            {
                return Mathf.Clamp01(defaultValue);
            }
        }

        /// <summary>
        /// 安全加载按键码选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="defaultKeyCode">默认按键码</param>
        /// <returns>加载的按键码，如果失败则返回默认值或保持当前值</returns>
        public static KeyCode LoadKeyCode(string key, KeyCode defaultKeyCode)
        {
            try
            {
                // 使用独立的配置文件系统，加载失败时保持当前值
                return ConfigManager.Load<KeyCode>(key, defaultKeyCode, keepCurrentOnError: true);
            }
            catch
            {
                // 如果ConfigManager也失败，尝试从当前值缓存获取
                return ConfigManager.Load<KeyCode>(key, defaultKeyCode, keepCurrentOnError: true);
            }
        }

        /// <summary>
        /// 安全保存整数选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="value">要保存的值</param>
        /// <returns>如果保存成功则返回true，否则返回false</returns>
        public static bool SaveInt(string key, int value)
        {
            try
            {
                ConfigManager.Save<int>(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全保存浮点数选项
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="value">要保存的值</param>
        /// <returns>如果保存成功则返回true，否则返回false</returns>
        public static bool SaveFloat(string key, float value)
        {
            try
            {
                ConfigManager.Save<float>(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 应用配置变更到控制器（通用方法）
        /// </summary>
        /// <param name="key">选项键</param>
        public static void ApplySettingChange(string key)
        {
            try
            {
                var controller = FirstPersonCameraController.Instance;
                if (controller == null) return;
                
                // 使用反射调用OnOptionsChanged方法
                var optionsType = typeof(FirstPersonCameraController);
                var method = optionsType.GetMethod("OnOptionsChanged", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(controller, new object[] { key });
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[FirstPersonCamera] 应用配置变更失败 ({key}): {ex.Message}");
            }
        }

        /// <summary>
        /// 应用开关设置到控制器
        /// </summary>
        /// <param name="key">选项键</param>
        /// <param name="value">开关值</param>
        public static void ApplyToggleSetting(string key, bool value)
        {
            try
            {
                var controller = FirstPersonCameraController.Instance;
                if (controller == null) return;
                
                bool onOptionsChangedCalled = false;
                
                // 尝试使用反射调用OnOptionsChanged方法
                try
                {
                    var optionsType = typeof(FirstPersonCameraController);
                    var method = optionsType.GetMethod("OnOptionsChanged", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        method.Invoke(controller, new object[] { key });
                        onOptionsChangedCalled = true;
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[FirstPersonCamera] OnOptionsChanged调用失败 ({key}): {ex.Message}");
                }
                
                // 如果OnOptionsChanged没有成功调用，或者对于某些特殊选项，使用后备逻辑
                // 对于隐藏选项，无论OnOptionsChanged是否调用成功，都确保重新应用隐藏逻辑
                if (!onOptionsChangedCalled || key.StartsWith("FirstPersonCamera_Hide") || 
                    key == "FirstPersonCamera_ForceShowHeldWeapon")
                {
                    // 后备逻辑：直接根据key调用相应的方法
                    if (key == "FirstPersonCamera_DisableBlur")
                    {
                        // 触发模糊选项变更
                        if (controller.IsFirstPersonMode)
                        {
                            if (value)
                                controller.GetType().GetMethod("DisablePostProcessingBlurEffects", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                            else
                                controller.GetType().GetMethod("RestorePostProcessingBlurEffects", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                        }
                    }
                    else if (key == "FirstPersonCamera_DisableSeeThrough")
                    {
                        // 触发透视选项变更
                        if (controller.IsFirstPersonMode)
                        {
                            if (value)
                                controller.GetType().GetMethod("DisableObstructionSeeThroughEffects", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                            else
                                controller.GetType().GetMethod("RestoreObstructionSeeThroughEffects", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                        }
                    }
                    else if (key == "FirstPersonCamera_DisableAimOcclusionFade")
                    {
                        // 触发准星虚化选项变更
                        if (controller.IsFirstPersonMode)
                        {
                            if (value)
                                controller.GetType().GetMethod("DisableAimOcclusionFade", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                            else
                                controller.GetType().GetMethod("RestoreAimOcclusionFade", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                        }
                    }
                    else if (key == "FirstPersonCamera_EnableRecoil" || 
                             key == "FirstPersonCamera_EnableAdsOffset")
                    {
                        // 这些选项会在下次射击或ADS时自动应用，不需要立即处理
                    }
                    else if (key.StartsWith("FirstPersonCamera_Hide") || 
                             key == "FirstPersonCamera_ForceShowHeldWeapon")
                    {
                        // 触发可视性选项变更（确保执行，即使OnOptionsChanged已调用）
                        if (controller.IsFirstPersonMode)
                        {
                            try
                            {
                                controller.GetType().GetMethod("RestoreFirstPersonObstructions", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                                controller.GetType().GetMethod("HideFirstPersonObstructions", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(controller, null);
                                UnityEngine.Debug.Log($"[FirstPersonCamera] 已重新应用部位隐藏逻辑 ({key})");
                            }
                            catch (System.Exception ex)
                            {
                                UnityEngine.Debug.LogError($"[FirstPersonCamera] 重新应用部位隐藏失败 ({key}): {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[FirstPersonCamera] ApplyToggleSetting失败 ({key}): {ex.Message}");
            }
        }
    }
}

