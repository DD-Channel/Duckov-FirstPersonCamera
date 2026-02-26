using UnityEngine;
using System.Collections.Generic;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 后坐力恢复系统（SmoothDamp 版本 - 仅狙击枪回弹）

        // ========== 各类武器恢复参数（仅狙击枪有效） ==========

        // 狙击枪：完全回零，可配置平滑时间
        private float sniperSmoothTime = 0.2f;          // 平滑时间（秒），值越大回弹越慢
        private float sniperMinRemainingV = 0.2f;          // 保留的最小垂直后坐力（0为完全回零）
        private float sniperMinRemainingH = 0f;
        private float sniperSmoothVelV, sniperSmoothVelH; // 速度缓存（由 SmoothDamp 使用）

        // 其他武器的参数保留但不会被使用（可留作未来扩展）
        private float rifleSmoothVelV, rifleSmoothVelH;
        private float smgSmoothVelV, smgSmoothVelH;
        private float shotgunSmoothVelV, shotgunSmoothVelH;
        private float marksmanSmoothVelV, marksmanSmoothVelH;
        private float unknownSmoothVelV, unknownSmoothVelH;

        // ========== 主恢复更新方法 ==========
        private void UpdateRecoilRecovery()
        {
            // 射击中不恢复
            if (isShooting) return;
            if (currentRecoilGun == null) return;

            WeaponCategory category = GetWeaponCategory(currentRecoilGun);
            FPLogger.Log($"[UpdateRecoilRecovery] weapon category: {category}");

            // 只有狙击枪进行回弹，其他武器均不恢复（准星保持最后上抬位置）
            if (category == WeaponCategory.Sniper)
            {
                FPLogger.Log("[UpdateRecoilRecovery] entering sniper recovery");
                SniperRecovery();
            }
            else
            {
                //FPLogger.Log($"[UpdateRecoilRecovery] {category} recovery disabled - retaining current aim point");
                // 不做任何处理，后坐力值保持不变，准星不回落
            }
        }

        // ========== 狙击枪恢复实现（使用 SmoothDamp） ==========
        private void SniperRecovery()
        {
            // 垂直平滑逼近 sniperMinRemainingV
            float targetV = Mathf.Max(sniperMinRemainingV, 0f);
            float newV = Mathf.SmoothDamp(accumulatedRecoilV, targetV, ref sniperSmoothVelV, sniperSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            float deltaV = accumulatedRecoilV - newV;
            accumulatedRecoilV = newV;
            pitch += deltaV;

            // 水平平滑逼近 sniperMinRemainingH（保留符号）
            float targetH = sniperMinRemainingH * Mathf.Sign(accumulatedRecoilH);
            float newH = Mathf.SmoothDamp(accumulatedRecoilH, targetH, ref sniperSmoothVelH, sniperSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            float deltaH = accumulatedRecoilH - newH;
            accumulatedRecoilH = newH;
            yaw -= deltaH;

            // 可选日志
            // FPLogger.Log($"[SniperRecovery] newV={newV:F3}, newH={newH:F3}, deltaV={deltaV:F3}, deltaH={deltaH:F3}");
        }

        #endregion
    }
}