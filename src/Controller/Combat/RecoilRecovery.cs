using UnityEngine;
using System.Collections.Generic;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 后坐力恢复系统

        // ========== 需要恢复后坐力的枪械类型列表（请根据实际游戏 TypeID 填写） ==========
        private HashSet<int> rifleTypeIDs = new HashSet<int>
        {
            // 示例：AK47 改进型 (10122), AK-103 改进型 (10129), M14 EBR (10103)
            10122, 10129, 10103,
            // 添加其他步枪 TypeID
        };

        private HashSet<int> sniperTypeIDs = new HashSet<int>
        {
            // 示例：AWP [NEX] (10109), M700 改进型 (10111), VKS (95808)
            10109, 10111, 95808,
            // 添加其他狙击枪 TypeID
        };

        private void UpdateRecoilRecovery()
        {
            if (accumulatedRecoilV == 0f && accumulatedRecoilH == 0f) return;
            if (isShooting) return; // 射击时不恢复
            if (recoveryPaused) return; // 切枪后暂停恢复（直到再次射击）

            // 获取当前武器
            if (currentRecoilGun == null) return;
            int typeID = currentRecoilGun.Item.TypeID;

            // ========== 判断是否需要恢复：仅步枪和狙击枪需要恢复 ==========
            bool shouldRecover = rifleTypeIDs.Contains(typeID) || sniperTypeIDs.Contains(typeID);
            if (!shouldRecover)
            {
                // 其他枪械（霰弹、冲锋枪等）不恢复
                return;
            }

            // 从当前武器获取散布恢复速率
            float scatterRecover = 1f;
            try
            {
                scatterRecover = currentRecoilGun.ScatterRecover;
            }
            catch { }

            // 将散布恢复速率转换为后坐力恢复速率（系数10可调）
            float baseRecoverRate = scatterRecover * 8f;

            // 动态恢复因子：累积后坐力越大，恢复越快
            float totalMagnitude = Mathf.Abs(accumulatedRecoilV) + Mathf.Abs(accumulatedRecoilH);
            float dynamicFactor = 1f + totalMagnitude * 1f; // 系数可调
            float recoverRate = baseRecoverRate * dynamicFactor;

            float recoverFactor = Mathf.Exp(-recoverRate * Time.unscaledDeltaTime);

            // 恢复垂直
            if (accumulatedRecoilV > 0.001f)
            {
                float oldV = accumulatedRecoilV;
                accumulatedRecoilV *= recoverFactor;
                float recoveredV = oldV - accumulatedRecoilV;
                pitch += recoveredV;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                if (accumulatedRecoilV < 0.001f) accumulatedRecoilV = 0f;
            }

            // 恢复水平
            if (Mathf.Abs(accumulatedRecoilH) > 0.001f)
            {
                float oldH = accumulatedRecoilH;
                accumulatedRecoilH *= recoverFactor;
                float recoveredH = oldH - accumulatedRecoilH;
                yaw -= recoveredH;
                if (Mathf.Abs(accumulatedRecoilH) < 0.001f) accumulatedRecoilH = 0f;
            }
        }
        #endregion
    }
}