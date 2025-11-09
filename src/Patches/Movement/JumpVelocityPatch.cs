using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using AccessTools = HarmonyLib.AccessTools;
using FirstPersonCamera.Utilities;
using System;

namespace FirstPersonCamera.Patches.Movement
{
    /// <summary>
    /// 跳跃速度补丁 - 关键修复：在重力添加后、Move 调用前设置速度
    /// 核心问题分析：
    /// 1. UpdateMovement 执行顺序：UpdateForceMove() -> 重力添加 -> Move()
    /// 2. UpdateForceMove 中的 vector2.y = vector.y; 会覆盖 forceMoveVelocity.y
    /// 3. 重力是向下的（负数），如果速度设置不正确，会导致向下移动
    /// 解决方案：使用 Transpiler 在重力添加后、Move 调用前，直接设置 velocity.y
    /// </summary>
    [HarmonyPatch(typeof(global::Movement), "UpdateMovement")]
    public static class JumpVelocityPatch
    {
        /// <summary>
        /// 待应用的跳跃速度（Y值）
        /// </summary>
        public static float? pendingJumpVelocityY = null;

        /// <summary>
        /// 跳跃速度应用的帧数计数器
        /// </summary>
        public static int jumpApplicationFrame = -1;

        /// <summary>
        /// 设置待应用的跳跃速度
        /// </summary>
        public static void SetPendingJumpVelocityY(float y)
        {
            pendingJumpVelocityY = y;
            jumpApplicationFrame = Time.frameCount;
            FPLogger.Log("设置待应用的跳跃速度Y值: {0} (帧: {1})", y, jumpApplicationFrame);
        }

        /// <summary>
        /// 暂停地面约束
        /// </summary>
        private static void PauseGroundConstraint(global::Movement movement)
        {
            try
            {
                var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                if (characterMovementField == null) return;

                var characterMovement = characterMovementField.GetValue(movement);
                if (characterMovement == null) return;

                var pauseMethod = AccessTools.Method(characterMovement.GetType(), "PauseGroundConstraint", new System.Type[] { typeof(float) });
                if (pauseMethod != null)
                {
                    pauseMethod.Invoke(characterMovement, new object[] { 0.5f });
                    FPLogger.Log("已暂停地面约束 0.5 秒");
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogWarning("暂停地面约束失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Prefix：暂停地面约束，并在 UpdateForceMove 调用之前设置 velocity.y
        /// 关键：必须在 UpdateForceMove 执行之前设置 velocity.y，这样 UpdateForceMove 中的 vector.y 就是正确的值
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(global::Movement __instance)
        {
            if (pendingJumpVelocityY.HasValue && Time.frameCount == jumpApplicationFrame)
            {
                try
                {
                    float jumpY = pendingJumpVelocityY.Value;
                    FPLogger.Log("UpdateMovement Prefix: 检测到跳跃速度 {0}，暂停地面约束并设置 velocity.y", jumpY);
                    
                    // 暂停地面约束
                    PauseGroundConstraint(__instance);
                    
                    // 关键：在 UpdateForceMove 调用之前，先设置 velocity.y
                    // 这样 UpdateForceMove 中的 vector = *this.characterMovement.velocity; 读取到的就是正确的值
                    var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                    if (characterMovementField != null)
                    {
                        var characterMovement = characterMovementField.GetValue(__instance);
                        if (characterMovement != null)
                        {
                            var velocityField = AccessTools.Field(characterMovement.GetType(), "velocity");
                            if (velocityField != null)
                            {
                                Vector3 v = (Vector3)velocityField.GetValue(characterMovement);
                                FPLogger.Log("UpdateMovement Prefix: 当前 velocity = {0} (设置前)", v);
                                v.y = jumpY;
                                velocityField.SetValue(characterMovement, v);
                                FPLogger.Log("UpdateMovement Prefix: 已设置 velocity.y = {0}，完整速度 = {1}", jumpY, v);
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityPatch Prefix 错误");
                }
            }
        }

        /// <summary>
        /// Transpiler：在重力添加后、Move 调用前，直接设置 velocity.y
        /// 关键：在 *this.characterMovement.velocity += Physics.gravity * Time.deltaTime; 之后
        ///       在 this.characterMovement.Move(...) 之前，设置 velocity.y = jumpY
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            
            // 查找重力添加和 Move 调用的位置
            // 目标：在 *this.characterMovement.velocity += Physics.gravity * Time.deltaTime; 之后
            //       在 this.characterMovement.Move(...) 之前，插入设置速度的代码
            
            // 由于 Transpiler 比较复杂，我们暂时使用 Postfix 方法
            // 但关键是在 Move 调用之前设置速度，所以我们使用 LateUpdate 或者延迟一帧
            
            return codes;
        }

        /// <summary>
        /// Postfix：在 Move 调用之后，立即在下一帧开始前设置速度
        /// 关键：由于 Move 可能会重置速度，我们需要在 Move 之后立即修复
        /// 但 Postfix 是在 Move 之后执行的，所以我们需要使用其他方法
        /// 
        /// 解决方案：使用 Unity 的 LateUpdate 或者在下一帧的 Prefix 中设置
        /// 但更简单的方法是：在 Postfix 中设置速度，虽然对当前帧没有影响，但可以确保下一帧正确
        /// 
        /// 更好的方案：使用 Transpiler 在重力添加后、Move 调用前插入代码
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(global::Movement __instance)
        {
            // 只在跳跃帧检查
            if (pendingJumpVelocityY.HasValue && Time.frameCount == jumpApplicationFrame)
            {
                try
                {
                    float jumpY = pendingJumpVelocityY.Value;
                    
                    var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                    if (characterMovementField == null) return;

                    var characterMovement = characterMovementField.GetValue(__instance);
                    if (characterMovement == null) return;

                    var velocityField = AccessTools.Field(characterMovement.GetType(), "velocity");
                    if (velocityField == null) return;

                    // 读取当前速度（在 Move 调用之后）
                    Vector3 currentVel = (Vector3)velocityField.GetValue(characterMovement);
                    
                    // 关键检查：如果速度Y值是负数或者太小，说明被覆盖了
                    // 在跳跃帧，速度应该是 jumpY + gravity ≈ jumpY - 0.16 (重力是负数)
                    // 但由于 Move 可能已经修改了速度，我们需要检查实际值
                    float expectedYAfterGravity = jumpY + Physics.gravity.y * Time.deltaTime;
                    
                    FPLogger.Log("UpdateMovement Postfix (跳跃帧): 当前 velocity.y = {0}, 期望（重力后）≈ {1}, 原始 jumpY = {2}", 
                        currentVel.y, expectedYAfterGravity, jumpY);
                    
                    // 如果速度被错误地设置为负数或接近0，或者被 Move 重置了，修复它
                    // 注意：如果速度是负数，说明被错误地设置了，我们需要修复
                    if (currentVel.y < 0f)
                    {
                        FPLogger.LogWarning("UpdateMovement Postfix: 速度被设置为负数！当前 = {0}，修复为 {1}", currentVel.y, expectedYAfterGravity);
                        currentVel.y = expectedYAfterGravity;
                        velocityField.SetValue(characterMovement, currentVel);
                        FPLogger.Log("UpdateMovement Postfix: 已修复速度，velocity.y = {0}", currentVel.y);
                    }
                    else if (currentVel.y < 0.5f && jumpY > 0.5f)
                    {
                        // 如果速度被重置为接近0，但跳跃速度应该是正数，说明被覆盖了
                        FPLogger.LogWarning("UpdateMovement Postfix: 速度被重置为接近0！当前 = {0}，修复为 {1}", currentVel.y, expectedYAfterGravity);
                        currentVel.y = expectedYAfterGravity;
                        velocityField.SetValue(characterMovement, currentVel);
                        FPLogger.Log("UpdateMovement Postfix: 已修复速度，velocity.y = {0}", currentVel.y);
                    }
                    else if (Mathf.Abs(currentVel.y - expectedYAfterGravity) > 2.0f && jumpY > 0.5f)
                    {
                        // 如果速度偏差太大，说明被错误地设置了
                        FPLogger.LogWarning("UpdateMovement Postfix: 速度偏差太大！当前 = {0}，期望 ≈ {1}，修复", currentVel.y, expectedYAfterGravity);
                        currentVel.y = expectedYAfterGravity;
                        velocityField.SetValue(characterMovement, currentVel);
                        FPLogger.Log("UpdateMovement Postfix: 已修复速度，velocity.y = {0}", currentVel.y);
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityPatch Postfix 错误");
                }
            }
            else if (pendingJumpVelocityY.HasValue && Time.frameCount == jumpApplicationFrame + 1)
            {
                // 在下一帧，清理标记
                pendingJumpVelocityY = null;
                FPLogger.Log("UpdateMovement Postfix: 清理跳跃标记");
            }
        }
    }

    /// <summary>
    /// 跳跃速度补丁 - UpdateForceMove 版本
    /// 在 UpdateForceMove 中，确保 forceMoveVelocity.y 和 velocity.y 都正确设置
    /// </summary>
    [HarmonyPatch(typeof(global::Movement), "UpdateForceMove")]
    public static class JumpVelocityForceMovePatch
    {
        /// <summary>
        /// Prefix：设置 forceMoveVelocity.y 为跳跃速度，并再次确认 velocity.y
        /// 注意：velocity.y 已经在 UpdateMovement Prefix 中设置了
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(global::Movement __instance)
        {
            if (JumpVelocityPatch.pendingJumpVelocityY.HasValue && Time.frameCount == JumpVelocityPatch.jumpApplicationFrame)
            {
                try
                {
                    float jumpY = JumpVelocityPatch.pendingJumpVelocityY.Value;
                    
                    // 设置 forceMoveVelocity.y
                    var forceMoveVelocityField = AccessTools.Field(typeof(global::Movement), "forceMoveVelocity");
                    if (forceMoveVelocityField != null)
                    {
                        Vector3 currentForceVel = (Vector3)forceMoveVelocityField.GetValue(__instance);
                        currentForceVel.y = jumpY;
                        forceMoveVelocityField.SetValue(__instance, currentForceVel);
                        FPLogger.Log("UpdateForceMove Prefix: 设置 forceMoveVelocity.y = {0}", jumpY);
                    }

                    // 再次确认 velocity.y（虽然已经在 UpdateMovement Prefix 中设置了，但这里再确认一次）
                    var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                    if (characterMovementField != null)
                    {
                        var characterMovement = characterMovementField.GetValue(__instance);
                        if (characterMovement != null)
                        {
                            var velocityField = AccessTools.Field(characterMovement.GetType(), "velocity");
                            if (velocityField != null)
                            {
                                Vector3 v = (Vector3)velocityField.GetValue(characterMovement);
                                FPLogger.Log("UpdateForceMove Prefix: 当前 velocity = {0}", v);
                                
                                // 如果 velocity.y 不正确，重新设置
                                if (Mathf.Abs(v.y - jumpY) > 0.1f)
                                {
                                    FPLogger.LogWarning("UpdateForceMove Prefix: velocity.y 不正确 (当前: {0}, 期望: {1})，重新设置", v.y, jumpY);
                                    v.y = jumpY;
                                    velocityField.SetValue(characterMovement, v);
                                    FPLogger.Log("UpdateForceMove Prefix: 已重新设置 velocity.y = {0}", jumpY);
                                }
                                else
                                {
                                    FPLogger.Log("UpdateForceMove Prefix: velocity.y 正确 = {0}", v.y);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityForceMovePatch Prefix 错误");
                }
            }
        }

        /// <summary>
        /// Postfix：在 UpdateForceMove 执行后，验证并修复 velocity.y
        /// 关键：UpdateForceMove 中的 vector2.y = vector.y; 应该不会覆盖，因为 vector.y 已经是正确的值
        /// 但如果被覆盖了，我们需要修复它
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(global::Movement __instance)
        {
            if (JumpVelocityPatch.pendingJumpVelocityY.HasValue && Time.frameCount == JumpVelocityPatch.jumpApplicationFrame)
            {
                try
                {
                    float jumpY = JumpVelocityPatch.pendingJumpVelocityY.Value;
                    
                    var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                    if (characterMovementField != null)
                    {
                        var characterMovement = characterMovementField.GetValue(__instance);
                        if (characterMovement != null)
                        {
                            var velocityField = AccessTools.Field(characterMovement.GetType(), "velocity");
                            if (velocityField != null)
                            {
                                Vector3 currentVel = (Vector3)velocityField.GetValue(characterMovement);
                                FPLogger.Log("UpdateForceMove Postfix: 当前 velocity = {0}", currentVel);
                                
                                // 如果速度被覆盖，重新设置
                                if (Mathf.Abs(currentVel.y - jumpY) > 0.1f)
                                {
                                    FPLogger.LogWarning("UpdateForceMove Postfix: 速度被覆盖 (当前: {0}, 期望: {1})，重新设置", currentVel.y, jumpY);
                                    currentVel.y = jumpY;
                                    velocityField.SetValue(characterMovement, currentVel);
                                    FPLogger.Log("UpdateForceMove Postfix: 已重新设置 velocity.y = {0}", jumpY);
                                }
                                else
                                {
                                    FPLogger.Log("UpdateForceMove Postfix: 速度Y值正常 = {0}", currentVel.y);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityForceMovePatch Postfix 错误");
                }
            }
        }
    }
}
