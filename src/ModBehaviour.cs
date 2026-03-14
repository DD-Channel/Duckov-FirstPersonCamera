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
using Duckov.Modding;
using Duckov.Scenes;
using HarmonyLib;
using FirstPersonCamera.Utilities;
using System.IO;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机 Mod 主类：初始化和场景切换处理
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private FirstPersonCameraController fpsController;
        private bool isInitialized = false;

        protected override void OnAfterSetup()
        {
            FPLogger.Log("Mod已加载！");

            DontDestroyOnLoad(gameObject);

            // 安装 Harmony 补丁（只装一次）
            try
            {
                var harmony = new Harmony("firstpersoncamera.aimpatch");
                harmony.PatchAll();
                FPLogger.Log("Harmony 补丁已安装");
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "安装 Harmony 失败");
            }

            // 监听场景事件
            SceneLoader.onAfterSceneInitialize += OnSceneInitialized;
            // 初始化本地化
            string configDir = Path.GetDirectoryName(ConfigManager.GetConfigFilePath());
            FPLocalization.Initialize(configDir);

            // 初始化相机控制器
            InitializeController();

            // 使用控制器内部的血条逻辑（不再安装全局抑制器）

            // 安装性能调优器
            if (gameObject.GetComponent<FpsPerfTuner>() == null)
            {
                gameObject.AddComponent<FpsPerfTuner>();
            }
            gameObject.AddComponent<VehicleCameraController>();
            // 初始化锁定系统
            _ = LockOnSystem.Instance;
            // 初始化入侵控制器
            gameObject.AddComponent<HackPossessionController>();
        }

        protected override void OnBeforeDeactivate()
        {
            FPLogger.Log("Mod正在卸载...");

            // 保存所有配置数据（包括动态保存的数据）
            try
            {
                FirstPersonCamera.Utilities.ConfigManager.SaveAll();
                FPLogger.Log("Mod卸载前已保存所有配置数据");
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "保存配置数据失败");
            }
            
            // 关闭日志系统
            try
            {
                FPLogger.Shutdown();
            }
            catch { }

            // 取消事件监听
            SceneLoader.onAfterSceneInitialize -= OnSceneInitialized;

            // 清理相机控制器
            if (fpsController != null)
            {
                Destroy(fpsController);
                fpsController = null;
            }

            isInitialized = false;
        }

        private void OnSceneInitialized(SceneLoadingContext context)
        {
            // 场景初始化后，等待角色就绪
            StartCoroutine(WaitForCharacterAndReinitialize());
        }

        private System.Collections.IEnumerator WaitForCharacterAndReinitialize()
        {
            // 等待角色加载完成
            yield return WaitForCharacterReady();

            // 等待一帧确保所有系统初始化完成
            yield return null;

            // 重新初始化相机控制器，并强制启用第一人称模式（原为 false，改为 true）
            if (fpsController != null)
            {
                fpsController.Reinitialize();
                fpsController.SetFirstPerson(true); // <--- 修改此处，自动启用第一人称
            }

            // 第三人称默认显示血条；FPS 切换时再由控制器抑制
            HealthBarSuppressor.RemoveControllerHandler();
        }

        private System.Collections.IEnumerator WaitForCharacterReady()
        {
            while (CharacterMainControl.Main == null)
            {
                yield return null;
            }

            var main = CharacterMainControl.Main;

            while (main.CharacterItem == null)
            {
                yield return null;
            }

            while (main.CharacterItem.Slots == null)
            {
                yield return null;
            }
        }

        private void InitializeController()
        {
            if (fpsController == null)
            {
                fpsController = gameObject.AddComponent<FirstPersonCameraController>();
                isInitialized = true;
                FPLogger.Log("相机控制器已初始化");
            }

            // 初始化独立UI系统
            try
            {
                StandaloneUI.StandaloneOptionsWindow.Instance.SetVisible(false);
                FPLogger.Log("独立UI系统已初始化");
            }
            catch (System.Exception ex)
            {
                FPLogger.LogException(ex, "初始化独立UI系统失败");
            }
            
            // 初始化简化版UI（只在原版设置中添加一个打开按钮）
            if (gameObject.GetComponent<FirstPersonOptionsUI>() == null)
            {
                gameObject.AddComponent<FirstPersonOptionsUI>();
                FPLogger.Log("简化版UI系统已初始化");
            }
        gameObject.AddComponent<SniperEnhancementController>();
        }
    }
}