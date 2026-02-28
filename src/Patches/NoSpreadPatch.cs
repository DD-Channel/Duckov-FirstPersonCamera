// using HarmonyLib;
// using UnityEngine;
// using FirstPersonCamera;

// namespace FirstPersonCamera.Patches
// {
//     [HarmonyPatch(typeof(ItemAgent_Gun))]
//     [HarmonyPatch("ShootOneBullet")]
//     internal static class NoSpreadPatch
//     {
//         // 霰弹枪瞄准时的散布缩放系数（可调整）
//         private static float adsShotgunSpreadMultiplier = 1f;

//         static void Prefix(ItemAgent_Gun __instance, ref Vector3 _shootDirection, Vector3 _muzzlePoint)
//         {
//             var controller = FirstPersonCameraController.Instance;
//             if (controller == null || !controller.IsFirstPersonMode) return;
//             if (__instance.Holder == null || !__instance.Holder.IsMainCharacter) return;

//             Camera mainCam = controller.MainCamera;
//             if (mainCam == null) return;

//             // 获取当前准星的世界点
//             Vector3 aimWorldPoint = controller.GetCurrentAimWorldPoint();

//             // 计算从枪口指向瞄准点的方向（用于霰弹枪的中心方向）
//             Vector3 directionToAim = (aimWorldPoint - _muzzlePoint).normalized;

//             // 仅处理霰弹枪（ShotCount > 1）
//             if (__instance.ShotCount > 1)
//             {
//                 // 获取枪械的原始 ShotAngle（腰射时的角度）
//                 float baseAngle = __instance.ShotAngle;

//                 // 获取枪械的最大射程
//                 float maxRange = __instance.BulletDistance;
//                 float distanceToAim = Vector3.Distance(_muzzlePoint, aimWorldPoint);
//                 float distanceFactor = Mathf.Clamp01(distanceToAim / maxRange); // 归一化距离因子

//                 // 根据距离动态调整角度：距离越近角度越小
//                 float adjustedAngle = baseAngle * distanceFactor;

//                 // 如果当前是瞄准状态，应用自定义缩放系数
//                 if (__instance.IsInAds)
//                 {
//                     adjustedAngle *= adsShotgunSpreadMultiplier;
//                 }

//                 float coneAngleRad = adjustedAngle * Mathf.Deg2Rad;

//                 // 生成在圆锥内的随机方向（均匀立体角分布）
//                 float randomAngle = Random.Range(0, 2 * Mathf.PI);
//                 float randomCos = 1f - Random.value * (1f - Mathf.Cos(coneAngleRad));
//                 float randomSin = Mathf.Sqrt(1f - randomCos * randomCos);

//                 // 构建局部坐标系：以 directionToAim 为 Z 轴
//                 Vector3 up = Vector3.up;
//                 Vector3 right = Vector3.right;
//                 // 避免与 directionToAim 平行导致叉积为零
//                 if (Mathf.Abs(Vector3.Dot(directionToAim, up)) > 0.99f)
//                     up = Vector3.forward;
//                 right = Vector3.Cross(directionToAim, up).normalized;
//                 up = Vector3.Cross(right, directionToAim).normalized;

//                 Vector3 randomDir = directionToAim * randomCos +
//                                     (up * Mathf.Sin(randomAngle) + right * Mathf.Cos(randomAngle)) * randomSin;

//                 _shootDirection = randomDir.normalized;
//             }
//             else
//             {
//                 // 如果不是霰弹枪，使用原始射击方向
//                 _shootDirection = directionToAim;
//             }
//         }
//     }
// }