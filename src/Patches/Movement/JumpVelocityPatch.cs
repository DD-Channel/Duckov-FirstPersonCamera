using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using AccessTools = HarmonyLib.AccessTools;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.Patches.Movement
{
    /// <summary>
    /// 跳跃速度补丁 - 使用 PauseGroundConstraint 暂停地面约束
    /// 核心思路：
    /// 1. 在跳跃时暂停地面约束（PauseGroundConstraint），避免 CharacterMovement 重置速度
    /// 2. 在 UpdateMovement 的 Postfix 中，在 Move 调用之后，直接设置 velocity.y
    /// 3. 关键：CharacterMovement 的 constrainToGround 可能在检测到角色在地面上时强制将 velocity.y 设为 0
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
        /// 暂停地面约束（关键！）
        /// </summary>
        private static void PauseGroundConstraint(global::Movement movement)
        {
            try
            {
                var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                if (characterMovementField == null)
                {
                    FPLogger.LogWarning("无法获取 characterMovement 字段");
                    return;
                }

                var characterMovement = characterMovementField.GetValue(movement);
                if (characterMovement == null)
                {
                    FPLogger.LogWarning("characterMovement 为 null");
                    return;
                }

                // 调用 PauseGroundConstraint 方法
                var pauseMethod = AccessTools.Method(characterMovement.GetType(), "PauseGroundConstraint", new System.Type[] { typeof(float) });
                if (pauseMethod != null)
                {
                    pauseMethod.Invoke(characterMovement, new object[] { 0.5f }); // 暂停 0.5 秒
                    FPLogger.Log("已暂停地面约束 0.5 秒");
                }
                else
                {
                    FPLogger.LogWarning("无法找到 PauseGroundConstraint 方法");
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogWarning("暂停地面约束失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 直接设置 CharacterMovement 的 velocity（unsafe 指针）
        /// </summary>
        private static void SetCharacterMovementVelocity(global::Movement movement, Vector3 velocity)
        {
            try
            {
                var characterMovementField = AccessTools.Field(typeof(global::Movement), "characterMovement");
                if (characterMovementField == null) return;

                var characterMovement = characterMovementField.GetValue(movement);
                if (characterMovement == null) return;

                // 获取 velocity 字段（unsafe 指针）
                var velocityField = AccessTools.Field(characterMovement.GetType(), "velocity");
                if (velocityField != null)
                {
                    // 由于 velocity 是 unsafe 指针，我们需要使用不同的方法
                    // 尝试通过反射设置
                    velocityField.SetValue(characterMovement, velocity);
                    FPLogger.Log("SetCharacterMovementVelocity: 设置 velocity = {0}", velocity);
                }
            }
            catch (System.Exception ex)
            {
                FPLogger.LogWarning("SetCharacterMovementVelocity 失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Prefix：在 UpdateMovement 执行前，如果有跳跃速度，暂停地面约束
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(global::Movement __instance)
        {
            if (pendingJumpVelocityY.HasValue && Time.frameCount == jumpApplicationFrame)
            {
                try
                {
                    float jumpY = pendingJumpVelocityY.Value;
                    FPLogger.Log("UpdateMovement Prefix: 检测到跳跃速度 {0}，暂停地面约束", jumpY);
                    
                    // 暂停地面约束（关键！）
                    PauseGroundConstraint(__instance);
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityPatch Prefix 错误");
                }
            }
        }

        /// <summary>
        /// Postfix：在 UpdateMovement 执行后，在重力添加和 Move 调用之后，直接设置 velocity.y
        /// 关键：在 Move 调用之后，velocity 已经被设置了，我们需要再次设置它
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(global::Movement __instance)
        {
            if (pendingJumpVelocityY.HasValue && Time.frameCount == jumpApplicationFrame)
            {
                try
                {
                    float jumpY = pendingJumpVelocityY.Value;
                    
                    // 获取当前速度
                    var velocityProperty = AccessTools.Property(typeof(global::Movement), "Velocity");
                    if (velocityProperty != null)
                    {
                        Vector3 currentVel = (Vector3)velocityProperty.GetValue(__instance);
                        
                        // 如果速度Y值不正确，直接设置
                        if (currentVel.y < jumpY * 0.5f)
                        {
                            FPLogger.LogWarning("UpdateMovement Postfix: 检测到速度Y值被覆盖 (当前: {0}, 期望: {1})，直接设置", currentVel.y, jumpY);
                            
                            // 设置新的速度
                            Vector3 newVel = currentVel;
                            newVel.y = jumpY;
                            
                            // 尝试直接设置 velocity
                            SetCharacterMovementVelocity(__instance, newVel);
                            
                            // 验证
                            Vector3 verifyVel = (Vector3)velocityProperty.GetValue(__instance);
                            if (verifyVel.y > 0.5f)
                            {
                                FPLogger.Log("UpdateMovement Postfix: 速度Y值已设置 = {0}", verifyVel.y);
                            }
                            else
                            {
                                FPLogger.LogWarning("UpdateMovement Postfix: 速度Y值设置失败，仍然 = {0}", verifyVel.y);
                            }
                        }
                        else
                        {
                            FPLogger.Log("UpdateMovement Postfix: 速度Y值正确 = {0}", currentVel.y);
                        }
                    }
                    
                    // 清理跳跃标记（在下一帧清理）
                    if (Time.frameCount > jumpApplicationFrame)
                    {
                        pendingJumpVelocityY = null;
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityPatch Postfix 错误");
                }
            }
        }
    }

    /// <summary>
    /// 跳跃速度补丁 - UpdateForceMove 版本
    /// 在 UpdateForceMove 执行后，检查速度
    /// </summary>
    [HarmonyPatch(typeof(global::Movement), "UpdateForceMove")]
    public static class JumpVelocityForceMovePatch
    {
        /// <summary>
        /// Prefix：确保 forceMoveVelocity.y 设置为跳跃速度
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(global::Movement __instance)
        {
            if (JumpVelocityPatch.pendingJumpVelocityY.HasValue && Time.frameCount == JumpVelocityPatch.jumpApplicationFrame)
            {
                try
                {
                    float jumpY = JumpVelocityPatch.pendingJumpVelocityY.Value;
                    
                    var forceMoveVelocityField = AccessTools.Field(typeof(global::Movement), "forceMoveVelocity");
                    if (forceMoveVelocityField != null)
                    {
                        Vector3 currentForceVel = (Vector3)forceMoveVelocityField.GetValue(__instance);
                        currentForceVel.y = jumpY;
                        forceMoveVelocityField.SetValue(__instance, currentForceVel);
                        FPLogger.Log("UpdateForceMove Prefix: 设置 forceMoveVelocity.y = {0}", jumpY);
                    }
                }
                catch (System.Exception ex)
                {
                    FPLogger.LogException(ex, "JumpVelocityForceMovePatch Prefix 错误");
                }
            }
        }

        /// <summary>
        /// Postfix：检查速度
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(global::Movement __instance)
        {
            if (JumpVelocityPatch.pendingJumpVelocityY.HasValue && Time.frameCount == JumpVelocityPatch.jumpApplicationFrame)
            {
                try
                {
                    float jumpY = JumpVelocityPatch.pendingJumpVelocityY.Value;
                    
                    var velocityProperty = AccessTools.Property(typeof(global::Movement), "Velocity");
                    if (velocityProperty != null)
                    {
                        Vector3 currentVel = (Vector3)velocityProperty.GetValue(__instance);
                        
                        if (currentVel.y > 0.5f)
                        {
                            FPLogger.Log("UpdateForceMove Postfix: 速度Y值正确 = {0}", currentVel.y);
                        }
                        else
                        {
                            FPLogger.LogWarning("UpdateForceMove Postfix: 速度Y值被覆盖 = {0}，将在 UpdateMovement Postfix 中修复", currentVel.y);
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