using HarmonyLib;
using UnityEngine;
using FirstPersonCamera;

namespace FirstPersonCamera.Patches
{
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    [HarmonyPatch("ShootOneBullet")]
    internal static class NoSpreadPatch
    {
        // 霰弹枪瞄准时的散布缩放系数
        private static float adsShotgunSpreadMultiplier = 1f;

        static void Prefix(ItemAgent_Gun __instance, ref Vector3 _shootDirection, Vector3 _muzzlePoint)
        {
            var controller = FirstPersonCameraController.Instance;
            if (controller == null || !controller.IsFirstPersonMode) return;
            if (__instance.Holder == null || !__instance.Holder.IsMainCharacter) return;

            Camera mainCam = controller.MainCamera;
            if (mainCam == null) return;

            // 判断是否为霰弹枪（ShotCount > 1）
            if (__instance.ShotCount > 1)
            {
                // 获取枪械的原始 ShotAngle（腰射时的角度）
                float baseAngle = __instance.ShotAngle;
                // 如果当前是瞄准状态，应用自定义缩放系数
                if (__instance.IsInAds)
                {
                    baseAngle *= adsShotgunSpreadMultiplier;
                }
                float coneAngleRad = baseAngle * Mathf.Deg2Rad;

                // 生成在圆锥内的随机方向（均匀立体角分布）
                float randomAngle = Random.Range(0, 2 * Mathf.PI);
                float randomCos = 1f - Random.value * (1f - Mathf.Cos(coneAngleRad));
                float randomSin = Mathf.Sqrt(1f - randomCos * randomCos);

                Vector3 up = mainCam.transform.up;
                Vector3 right = mainCam.transform.right;
                Vector3 dir = mainCam.transform.forward * randomCos +
                              (up * Mathf.Sin(randomAngle) + right * Mathf.Cos(randomAngle)) * randomSin;

                _shootDirection = dir.normalized;
            }
            else
            {
                // 其他枪：无散布，直接指向屏幕中心
                _shootDirection = mainCam.transform.forward;
            }
        }
    }
}