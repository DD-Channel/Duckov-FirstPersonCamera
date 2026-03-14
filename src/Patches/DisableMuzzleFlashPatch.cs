using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Patches
{
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    [HarmonyPatch("TransToFire")]
    internal static class DisableMuzzleFlashPatch
    {
        private static FieldInfo muzzleFxField;
        private static readonly Dictionary<ItemSetting_Gun, GameObject> originalMuzzleFx = new Dictionary<ItemSetting_Gun, GameObject>();

        static void Prefix(ItemAgent_Gun __instance)
        {
            // 仅在第一人称模式且为主角时生效
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode)
                return;

            if (__instance.Holder == null || !__instance.Holder.IsMainCharacter)
                return;

            // 读取选项（0=显示火光，1=禁用）
            bool disableFlash = OptionsHelper.LoadInt(OptionsUIConstants.DisableMuzzleFlashKey, 0) == 1;
            var gunSetting = __instance.GunItemSetting;
            if (gunSetting == null) return;

            // 缓存 FieldInfo
            if (muzzleFxField == null)
            {
                muzzleFxField = typeof(ItemSetting_Gun).GetField("muzzleFxPfb",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (muzzleFxField == null) return;
            }

            if (disableFlash)
            {
                // 如果尚未保存该武器的原始火光预制体，先保存
                if (!originalMuzzleFx.ContainsKey(gunSetting))
                {
                    originalMuzzleFx[gunSetting] = muzzleFxField.GetValue(gunSetting) as GameObject;
                }
                // 禁用火光：临时设为 null
                muzzleFxField.SetValue(gunSetting, null);
            }
            else
            {
                // 如果之前保存过原始值，则恢复
                if (originalMuzzleFx.TryGetValue(gunSetting, out GameObject original))
                {
                    muzzleFxField.SetValue(gunSetting, original);
                    // 可以选择不移除，以便下次禁用时再次使用（节省反射）
                }
                // 否则保持原样（未保存过，说明从未禁用过，字段本来就是原始值）
            }
        }
    }
}