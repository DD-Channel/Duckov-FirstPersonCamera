using HarmonyLib;
using FirstPersonCamera;

namespace FirstPersonCamera.Patches
{
    [HarmonyPatch(typeof(ADSAimMarker))]
    [HarmonyPatch("SetScatter")]
    internal static class ForceAdsCrosshairMinimalPatch
    {
        private static readonly System.Collections.Generic.HashSet<int> forcedMinimalScopes = new System.Collections.Generic.HashSet<int>
        {
            572, // 快速瞄具 Lv4
            // 可添加其他需要强制保持最小状态的倍镜 TypeID
        };

        static void Prefix(ADSAimMarker __instance, ref float _currentScatter, float _minScatter)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode) return;

            var gun = controller.GetCurrentGun();
            if (gun == null || gun.Item == null) return;

            var slot = gun.Item.Slots?.GetSlot("Scope");
            if (slot == null || slot.Content == null) return;

            int scopeTypeID = slot.Content.TypeID;

            if (forcedMinimalScopes.Contains(scopeTypeID))
            {
                // 将当前散布强制设为 0，使准星完全不扩散
                _currentScatter = 0f;
                // 若希望保留最小扩散形态，可用下一行
                // _currentScatter = _minScatter;
            }
        }
    }
}