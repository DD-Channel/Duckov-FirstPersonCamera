using UnityEngine;
using Duckov.Modding;
using Duckov.Scenes;
using HarmonyLib;

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
            Debug.Log("[FirstPersonCamera] Mod已加载！");

            DontDestroyOnLoad(gameObject);

            // 安装 Harmony 补丁（只装一次）
            try
            {
                var harmony = new Harmony("firstpersoncamera.aimpatch");
                harmony.PatchAll();
                Debug.Log("[FirstPersonCamera] Harmony 补丁已安装");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FirstPersonCamera] 安装 Harmony 失败: " + ex);
            }

            // 监听场景事件
            SceneLoader.onAfterSceneInitialize += OnSceneInitialized;

            // 初始化相机控制器
            InitializeController();

            // 使用控制器内部的血条逻辑（不再安装全局抑制器）

            // 安装性能调优器
            if (gameObject.GetComponent<FpsPerfTuner>() == null)
            {
                gameObject.AddComponent<FpsPerfTuner>();
            }
        }

        protected override void OnBeforeDeactivate()
        {
            Debug.Log("[FirstPersonCamera] Mod正在卸载...");

            // 保存所有配置数据（包括动态保存的数据）
            try
            {
                FirstPersonCamera.Utilities.ConfigManager.SaveAll();
                Debug.Log("[FirstPersonCamera] Mod卸载前已保存所有配置数据");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FirstPersonCamera] 保存配置数据失败: {ex.Message}");
            }

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

            // 重新初始化相机控制器，并强制回到第三人称
            if (fpsController != null)
            {
                fpsController.Reinitialize();
                fpsController.SetFirstPerson(false);
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
                Debug.Log("[FirstPersonCamera] 相机控制器已初始化");
            }

            // 初始化独立UI系统
            try
            {
                StandaloneUI.StandaloneOptionsWindow.Instance.SetVisible(false);
                Debug.Log("[FirstPersonCamera] 独立UI系统已初始化");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FirstPersonCamera] 初始化独立UI系统失败: " + ex);
            }
            
            // 初始化简化版UI（只在原版设置中添加一个打开按钮）
            if (gameObject.GetComponent<FirstPersonOptionsUI>() == null)
            {
                gameObject.AddComponent<FirstPersonOptionsUI>();
                Debug.Log("[FirstPersonCamera] 简化版UI系统已初始化");
            }
        }
    }
}
