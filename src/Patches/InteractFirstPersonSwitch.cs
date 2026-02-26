using HarmonyLib;
using System.Reflection;
using FirstPersonCamera;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Patches
{
    /// <summary>
    /// 当玩家在第一人称下与特定的建造台交互时，显示双语言红色大字体提示并阻止建造界面打开
    /// 仅对名称匹配 "BuildingAreaInvoker" 或 "BuildingAreaInvoker_1" 的实例生效
    /// </summary>
    [HarmonyPatch(typeof(BuilderViewInvoker))]
    [HarmonyPatch("OnInteractFinished")]
    public static class BuilderViewInvoker_OnInteractFinished_Patch
    {
        static bool Prefix(BuilderViewInvoker __instance)
        {
            // 仅对特定的建造台实例生效
            if (__instance.name != "BuildingAreaInvoker" && __instance.name != "BuildingAreaInvoker_1")
            {
                return true; // 其他名称的 BuilderViewInvoker 允许正常打开
            }

            var controller = FirstPersonCameraController.Instance;
            if (controller != null && controller.IsFirstPersonMode)
            {
                // 显示双语言红色大字体提示（中文在上，英文在下）
                string message = "<color=red><size=130%>当前视角下无法使用，请先按F5切换视角后重新交互\nCannot use in this view, please press F5 to switch view and try again</size></color>";
                ShowBubbleMessage(controller, message);
                // 阻止原方法执行，不打开建造界面
                return false;
            }
            // 允许原方法执行（第三人称时正常打开建造界面）
            return true;
        }

        /// <summary>
        /// 通过反射调用 FirstPersonCameraController 的私有方法 ShowDialogueBubble
        /// </summary>
        private static void ShowBubbleMessage(FirstPersonCameraController controller, string message)
        {
            try
            {
                MethodInfo method = typeof(FirstPersonCameraController).GetMethod("ShowDialogueBubble",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(controller, new object[] { message });
                }
                else
                {
                    FPLogger.LogWarning("[BuilderViewInvoker] 未找到 ShowDialogueBubble 方法，请检查 ScavDialogueBubbleUI.cs 是否包含该方法");
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogError($"[BuilderViewInvoker] 显示提示失败: {ex.Message}");
            }
        }
    }

    // ========== GamingConsole 和 Interact_CustomFace 的补丁（基于类名过滤） ==========

    [HarmonyPatch(typeof(InteractableBase))]
    [HarmonyPatch("StartInteract")]
    public static class SpecificInteract_StartInteract_ExitFirstPerson
    {
        static void Postfix(InteractableBase __instance, CharacterMainControl _interactCharacter, bool __result)
        {
            if (!__result || _interactCharacter == null || !_interactCharacter.IsMainCharacter)
                return;

            string typeName = __instance.GetType().Name;
            bool isTarget = typeName == "GamingConsole" || typeName == "Interact_CustomFace";
            if (!isTarget)
                return;

            var controller = FirstPersonCameraController.Instance;
            if (controller == null) return;

            if (controller.IsFirstPersonMode)
            {
                controller.SetFirstPerson(false);
                FPLogger.Log($"[{typeName}] StartInteract 触发，立即退出第一人称 - {__instance.name}");
            }
        }
    }

    [HarmonyPatch(typeof(InteractableBase))]
    [HarmonyPatch("StopInteract")]
    public static class SpecificInteract_StopInteract_EnterFirstPerson
    {
        static void Prefix(InteractableBase __instance)
        {
            string typeName = __instance.GetType().Name;
            bool isTarget = typeName == "GamingConsole" || typeName == "Interact_CustomFace";
            if (!isTarget)
                return;

            var controller = FirstPersonCameraController.Instance;
            if (controller == null) return;

            if (!controller.IsFirstPersonMode)
            {
                controller.SetFirstPerson(true);
                FPLogger.Log($"[{typeName}] StopInteract 触发，恢复第一人称 - {__instance.name}");
            }
        }
    }

    [HarmonyPatch(typeof(InteractableBase))]
    [HarmonyPatch("InternalStopInteract")]
    public static class SpecificInteract_InternalStopInteract_EnterFirstPerson
    {
        static void Prefix(InteractableBase __instance)
        {
            string typeName = __instance.GetType().Name;
            bool isTarget = typeName == "GamingConsole" || typeName == "Interact_CustomFace";
            if (!isTarget)
                return;

            var controller = FirstPersonCameraController.Instance;
            if (controller == null) return;

            if (!controller.IsFirstPersonMode)
            {
                controller.SetFirstPerson(true);
                FPLogger.Log($"[{typeName}] InternalStopInteract 触发，恢复第一人称 - {__instance.name}");
            }
        }
    }
}