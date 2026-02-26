/*
 * FirstPersonCamera - 第一人称相机 Mod
 * Copyright (C) 2024 FirstPersonCamera Contributors
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机静态API
    /// 用于与其他mod兼容，提供统一的接口
    /// </summary>
    public static class FirstPersonCameraAPI
    {
        #region 委托定义
        /// <summary>
        /// 获取是否处于第一人称模式的委托
        /// </summary>
        public static System.Func<bool> GetIsFirstPersonMode;
        
        /// <summary>
        /// 获取切换键位的委托
        /// </summary>
        public static System.Func<KeyCode> GetToggleKey;
        
        /// <summary>
        /// 切换到第三人称俯视角的委托
        /// </summary>
        public static System.Action SwitchToThirdPersonTopDown;
        
        /// <summary>
        /// 当从第一人称切换到第三人称俯视角时触发的事件
        /// </summary>
        public static event System.Action OnSwitchToThirdPersonTopDown;
        #endregion
        
        #region 公共API方法
        /// <summary>
        /// 获取是否处于第一人称模式
        /// </summary>
        /// <returns>如果处于第一人称模式返回true，否则返回false</returns>
        public static bool IsFirstPersonMode()
        {
            if (GetIsFirstPersonMode != null)
            {
                try
                {
                    return GetIsFirstPersonMode();
                }
                catch
                {
                    return false;
                }
            }
            
            // 如果没有注册委托，尝试通过单例访问
            if (FirstPersonCameraController.Instance != null)
            {
                return FirstPersonCameraController.Instance.IsFirstPersonMode;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取第一人称切换键位
        /// </summary>
        /// <returns>切换键位的KeyCode</returns>
        public static KeyCode GetToggleKeyCode()
        {
            if (GetToggleKey != null)
            {
                try
                {
                    return GetToggleKey();
                }
                catch
                {
                    return KeyCode.F5;
                }
            }
            
            // 如果没有注册委托，尝试通过单例访问
            if (FirstPersonCameraController.Instance != null)
            {
                return FirstPersonCameraController.Instance.toggleKey;
            }
            
            return KeyCode.F5; // 默认值
        }
        
        /// <summary>
        /// 切换到第三人称俯视角
        /// </summary>
        public static void SwitchToTopDown()
        {
            if (SwitchToThirdPersonTopDown != null)
            {
                try
                {
                    SwitchToThirdPersonTopDown();
                }
                catch { }
            }
        }
        
        /// <summary>
        /// 触发切换到第三人称俯视角事件
        /// 内部使用，由FirstPersonCameraController调用
        /// </summary>
        internal static void InvokeSwitchToThirdPersonTopDown()
        {
            try
            {
                OnSwitchToThirdPersonTopDown?.Invoke();
            }
            catch { }
        }
        #endregion
    }
}