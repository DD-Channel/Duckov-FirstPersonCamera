using System;
using UnityEngine;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        private bool deathEventSubscribed = false;

        // 在 Start 或 OnEnable 中调用此方法
        private void SubscribeDeathEvent()
        {
            if (deathEventSubscribed) return;
            try
            {
                LevelManager.OnMainCharacterDead += OnMainCharacterDeadHandler;
                deathEventSubscribed = true;
                FPLogger.Log("已订阅角色死亡事件");
            }
            catch (Exception ex)
            {
                FPLogger.LogException(ex, "订阅角色死亡事件失败，尝试使用 Health.OnDead 备用方案");
                // 备用方案：使用 Health.OnDead 事件
                Health.OnDead += OnHealthDeadHandler;
                deathEventSubscribed = true;
            }
        }

        private void UnsubscribeDeathEvent()
        {
            if (!deathEventSubscribed) return;
            try
            {
                LevelManager.OnMainCharacterDead -= OnMainCharacterDeadHandler;
                Health.OnDead -= OnHealthDeadHandler;
                deathEventSubscribed = false;
                FPLogger.Log("已取消订阅角色死亡事件");
            }
            catch { }
        }

        private void OnMainCharacterDeadHandler(DamageInfo dmgInfo)
        {
            FPLogger.Log("角色死亡（LevelManager事件），重置瞄准状态");
            ResetAimState();
        }

        private void OnHealthDeadHandler(Health health, DamageInfo dmgInfo)
        {
            // 仅当死亡角色是主角色时处理
            if (health != null && health.IsMainCharacterHealth)
            {
                FPLogger.Log("角色死亡（Health事件），重置瞄准状态");
                ResetAimState();
            }
        }

        /// <summary>
        /// 重置所有与瞄准相关的状态，确保复活后视角恢复正常
        /// </summary>
        private void ResetAimState()
        {
            // 重置倍镜相关状态
            currentScopeTypeID = -1;
            targetScopeFovMultiplier = 1f;
            currentScopeFovMultiplier = 1f;
            if (mainCamera != null)
            {
                mainCamera.fieldOfView = baseFov;
            }

            // 恢复准星（隐藏的倍镜元素）
            RestoreAdsCrosshair();

            // 重置后坐力累积（避免复活后仍有后坐力残留）
            accumulatedRecoilV = 0f;
            accumulatedRecoilH = 0f;

            // 重置枪械抖动
            gunShakeRotation = Vector3.zero;
            gunShakeForward = 0f;
            gunShakeVelocity = Vector3.zero;
            gunShakeForwardVelocity = 0f;

            FPLogger.Log("瞄准状态已重置");
        }
    }
}