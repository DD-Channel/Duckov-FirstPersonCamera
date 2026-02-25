using UnityEngine;
using System.Collections.Generic;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 后坐力恢复系统

        // ========== 狙击枪恢复参数 ==========
        private float sniperRecoveryStartTime = -1f;
        private float sniperRecoveryDuration = 0.15f;      // 恢复持续时间（秒）
        private float sniperInitialV;
        private float sniperInitialH;

        // ========== 主恢复更新方法 ==========
        private void UpdateRecoilRecovery()
        {
            // 如果后坐力已经为零，重置狙击枪计时器
            if (accumulatedRecoilV == 0f && accumulatedRecoilH == 0f)
            {
                sniperRecoveryStartTime = -1f;
                return;
            }

            // 射击中不恢复
            if (isShooting) return;
            if (currentRecoilGun == null) return;

            WeaponCategory category = GetWeaponCategory(currentRecoilGun);
            FPLogger.Log($"[UpdateRecoilRecovery] weapon category: {category}");

            // 只有狙击枪才进行恢复（完全回弹）
            if (category == WeaponCategory.Sniper)
            {
                FPLogger.Log("[UpdateRecoilRecovery] entering sniper recovery");
                SniperRecovery();
            }
            else
            {
                // 其他武器：停止射击后不恢复，准星保持在最后上抬位置
                FPLogger.Log($"[UpdateRecoilRecovery] {category} recovery disabled - retaining current aim point");
                // 不做任何处理，accumulatedRecoilV/H 保持不变，准星位置不变
            }
        }

        /// <summary>
        /// 狙击枪：线性回弹（完全回零）
        /// </summary>
        private void SniperRecovery()
        {
            if (sniperRecoveryStartTime < 0f)
            {
                sniperRecoveryStartTime = Time.unscaledTime;
                sniperInitialV = accumulatedRecoilV;
                sniperInitialH = accumulatedRecoilH;
                FPLogger.Log($"[SniperRecovery] started at {sniperRecoveryStartTime}, initial V={sniperInitialV:F3}, H={sniperInitialH:F3}");
            }

            float elapsed = Time.unscaledTime - sniperRecoveryStartTime;
            float t = Mathf.Clamp01(elapsed / sniperRecoveryDuration);
            float targetV = Mathf.Lerp(sniperInitialV, 0f, t);
            float targetH = Mathf.Lerp(sniperInitialH, 0f, t);

            float deltaV = accumulatedRecoilV - targetV;
            float deltaH = accumulatedRecoilH - targetH;

            accumulatedRecoilV = targetV;
            accumulatedRecoilH = targetH;
            pitch += deltaV;
            yaw -= deltaH;

            FPLogger.Log($"[SniperRecovery] t={t:F3}, targetV={targetV:F3}, targetH={targetH:F3}, deltaV={deltaV:F3}, deltaH={deltaH:F3}");

            if (t >= 1f)
            {
                accumulatedRecoilV = 0f;
                accumulatedRecoilH = 0f;
                sniperRecoveryStartTime = -1f;
                FPLogger.Log("[SniperRecovery] completed");
            }
        }

        #endregion
    }
}