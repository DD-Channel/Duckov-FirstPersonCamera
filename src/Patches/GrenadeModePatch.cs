using HarmonyLib;
using UnityEngine;
using FirstPersonCamera;
using FirstPersonCamera.Utilities;
using System.Collections.Generic;
using System.Reflection;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        // 投掷模式：false = 近投，true = 远投
        public bool grenadeFarMode = false;
        private float lastGrenadeModeSwitchTime;
        private const float GRENADE_MODE_COOLDOWN = 0.2f;

        // 力度乘数（整体缩放）
        public float grenadeFarPowerMultiplier = 0.9f;
        public float grenadeNearPowerMultiplier = 0.9f;

        // 垂直速度独立乘数（1.0 = 保持原物理）
        public float grenadeFarVerticalMultiplier = 1.1f;
        public float grenadeNearVerticalMultiplier = 1.7f;

        // 手雷物品 TypeID 集合（所有手雷，请根据实际游戏补充）
        private static readonly HashSet<int> GRENADE_TYPE_IDS = new HashSet<int>
        {
            23,24,66,67,660,933,941,942,1366,12406,12407,12409,12410
        };

        /// <summary>
        /// 检查当前手持物品是否为手雷
        /// </summary>
        public bool IsHoldingGrenade()
        {
            if (mainCharacter == null) return false;
            var holdItemAgent = mainCharacter.CurrentHoldItemAgent;
            if (holdItemAgent == null || holdItemAgent.Item == null) return false;
            return GRENADE_TYPE_IDS.Contains(holdItemAgent.Item.TypeID);
        }

        /// <summary>
        /// 更新手雷模式切换输入（在 Update 中调用）
        /// </summary>
        public void UpdateGrenadeMode()
        {
            if (!IsHoldingGrenade()) return;

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (Time.unscaledTime - lastGrenadeModeSwitchTime > GRENADE_MODE_COOLDOWN)
                {
                    grenadeFarMode = !grenadeFarMode;
                    lastGrenadeModeSwitchTime = Time.unscaledTime;
                    ShowGrenadeModeMessage();
                    FPLogger.Log($"[Grenade] 切换模式: {(grenadeFarMode ? "低抛" : "高抛")}");
                }
            }
        }

        /// <summary>
        /// 显示当前投掷模式（使用对话气泡）
        /// </summary>
        private void ShowGrenadeModeMessage()
        {
            string mode = grenadeFarMode ? "低抛" : "高抛";
            string msg = $"<color=yellow>{mode}模式</color>";
            ShowDialogueBubble(msg);
            FPLogger.Log($"[Grenade] 显示模式: {mode}");
        }
    }
}

namespace FirstPersonCamera.Patches
{
    /// <summary>
    /// 在投掷物发射后应用力度乘数和垂直速度独立控制
    /// </summary>
    [HarmonyPatch(typeof(Grenade))]
    [HarmonyPatch("Launch")]
    public static class Grenade_Launch_Patch
    {
        private static FieldInfo rbField;

        static void Postfix(Grenade __instance, CharacterMainControl fromCharacter)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null) return;
            if (fromCharacter == null || !fromCharacter.IsMainCharacter) return;

            // 获取 Rigidbody
            if (rbField == null)
                rbField = typeof(Grenade).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rbField == null) return;
            Rigidbody rb = rbField.GetValue(__instance) as Rigidbody;
            if (rb == null) return;

            // 应用力度乘数
            float powerMultiplier = controller.grenadeFarMode ? controller.grenadeFarPowerMultiplier : controller.grenadeNearPowerMultiplier;
            rb.velocity *= powerMultiplier;

            // 单独调整垂直分量
            float verticalMultiplier = controller.grenadeFarMode ? controller.grenadeFarVerticalMultiplier : controller.grenadeNearVerticalMultiplier;
            Vector3 vel = rb.velocity;
            vel.y *= verticalMultiplier;
            rb.velocity = vel;

            FPLogger.Log($"[Grenade] Launch: 应用力度乘数={powerMultiplier}, 垂直乘数={verticalMultiplier}, 速度={rb.velocity}");
        }
    }

    /// <summary>
    /// 修改手雷轨迹预览线，使其匹配当前投掷模式
    /// </summary>
    [HarmonyPatch(typeof(SkillProjectileLineHUD))]
    [HarmonyPatch("UpdateLine")]
    public static class SkillProjectileLineHUD_UpdateLine_Patch
    {
        static void Prefix(ref float verticleSpeed, ref Vector3 target)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null) return;
            if (!controller.IsHoldingGrenade()) return;

            float verticalMultiplier = controller.grenadeFarMode ? controller.grenadeFarVerticalMultiplier : controller.grenadeNearVerticalMultiplier;
            verticleSpeed *= verticalMultiplier;
            FPLogger.Log($"[Grenade] 预览线: 垂直速度乘数={verticalMultiplier}");
        }
    }

    /// <summary>
    /// 在 FirstPersonCameraController 的 Update 中注入手雷模式检测
    /// </summary>
    [HarmonyPatch(typeof(FirstPersonCameraController))]
    [HarmonyPatch("Update")]
    public static class FirstPersonCameraController_Update_Patch
    {
        static void Postfix(FirstPersonCameraController __instance)
        {
            __instance.UpdateGrenadeMode();
        }
    }
}