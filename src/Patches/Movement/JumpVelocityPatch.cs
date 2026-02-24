using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Patches.Movement
{
    // /// <summary>
    // /// 跳跃速度补丁 - 在重力添加后、Move 调用前设置 velocity.y
    // /// 用于触发跳跃动画和状态，不影响手动位置控制
    // /// </summary>
    // [HarmonyPatch(typeof(global::Movement), "UpdateMovement")]
    // public static class JumpVelocityPatch
    // {
    //     private static float? pendingJumpVelocityY = null;
    //     private static int jumpApplicationFrame = -1;

    //     public static void SetPendingJumpVelocityY(float y)
    //     {
    //         pendingJumpVelocityY = y;
    //         jumpApplicationFrame = Time.frameCount;
    //         FPLogger.Log("[JumpPatch] SetPendingJumpVelocityY called: y={0}, frame={1}", y, jumpApplicationFrame);
    //     }

    //     [HarmonyPrefix]
    //     private static void Prefix(global::Movement __instance)
    //     {
    //         if (pendingJumpVelocityY.HasValue && Time.frameCount == jumpApplicationFrame)
    //         {
    //             TryApplyVelocity(__instance, "Prefix");
    //         }
    //     }

    //     [HarmonyTranspiler]
    //     private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //     {
    //         var codes = new List<CodeInstruction>(instructions);
    //         bool inserted = false;

    //         for (int i = 0; i < codes.Count - 5; i++)
    //         {
    //             if (codes[i].opcode == OpCodes.Call && 
    //                 codes[i].operand is System.Reflection.MethodInfo method && 
    //                 method.Name == "get_gravity" && method.DeclaringType == typeof(Physics))
    //             {
    //                 if (i + 4 < codes.Count &&
    //                     codes[i+1].opcode == OpCodes.Ldc_R4 &&
    //                     codes[i+2].opcode == OpCodes.Mul &&
    //                     codes[i+3].opcode == OpCodes.Call &&
    //                     codes[i+4].opcode == OpCodes.Call)
    //                 {
    //                     int insertPos = i + 5;
    //                     codes.Insert(insertPos, new CodeInstruction(OpCodes.Ldarg_0));
    //                     codes.Insert(insertPos + 1, new CodeInstruction(OpCodes.Call, 
    //                         AccessTools.Method(typeof(JumpVelocityPatch), nameof(InjectJumpVelocity))));
    //                     inserted = true;
    //                     FPLogger.Log("[JumpPatch] Transpiler inserted successfully");
    //                     break;
    //                 }
    //             }
    //         }

    //         if (!inserted)
    //         {
    //             FPLogger.LogWarning("[JumpPatch] Transpiler failed to insert, will rely on Prefix only");
    //         }

    //         return codes;
    //     }

    //     private static void InjectJumpVelocity(global::Movement movementInstance)
    //     {
    //         TryApplyVelocity(movementInstance, "Transpiler");
    //     }

    //     private static void TryApplyVelocity(global::Movement movementInstance, string source)
    //     {
    //         if (!pendingJumpVelocityY.HasValue || Time.frameCount != jumpApplicationFrame)
    //             return;

    //         try
    //         {
    //             var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
    //             if (characterMovementField == null) return;

    //             var characterMovement = characterMovementField.GetValue(movementInstance);
    //             if (characterMovement == null) return;

    //             var velocityField = AccessTools.Field(characterMovement.GetType(), "velocity");
    //             if (velocityField == null) return;

    //             Vector3 velocity = (Vector3)velocityField.GetValue(characterMovement);
    //             velocity.y = pendingJumpVelocityY.Value;
    //             velocityField.SetValue(characterMovement, velocity);

    //             FPLogger.Log("[JumpPatch] Velocity set by {0}: y={1}", source, velocity.y);
    //             pendingJumpVelocityY = null; // 只应用一次
    //         }
    //         catch (Exception ex)
    //         {
    //             FPLogger.LogException(ex, "[JumpPatch] TryApplyVelocity exception");
    //         }
    //     }
    // }

    /// <summary>
    /// 此补丁已被 ECM2 跳跃实现取代，保留文件仅为避免删除。
    /// 所有方法均为空，不会影响游戏。
    /// </summary>
    [HarmonyPatch(typeof(global::Movement), "UpdateMovement")]
    public static class JumpVelocityPatch
    {
        public static void SetPendingJumpVelocityY(float y) { }
        public static void PauseGroundConstraint(global::Movement movement, float duration) { }

        [HarmonyPrefix]
        public static void Prefix(global::Movement __instance) { }

        [HarmonyPostfix]
        public static void Postfix(global::Movement __instance) { }
    }

    [HarmonyPatch(typeof(global::Movement), "UpdateForceMove")]
    public static class JumpVelocityForceMovePatch
    {
        [HarmonyPrefix]
        public static void Prefix(global::Movement __instance) { }
    }
}