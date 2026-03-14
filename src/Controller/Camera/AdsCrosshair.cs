using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 准星隐藏字段
        /// <summary>
        /// 每个倍镜类型对应的中心点物体名称
        /// 需要您根据实际游戏中的子物体名称填写
        /// </summary>
        private Dictionary<int, string> scopeCenterPartNames = new Dictionary<int, string>()
        {
            // 1.2x 倍镜 (红点、全息等)
            { 570, "CenterDot" },   // 数字瞄具 Lv4
            { 571, "CenterDot" },   // 全息瞄具 Lv4
            { 572, "AimMarkerRotateParent" }, // 快速瞄具 Lv4 (请根据实际调试结果修改)
            { 573, "CenterDot" },   // 红点瞄具 Lv4

            // 2x 倍镜
            { 574, "Center" },      

            // 4x 倍镜
            { 568, "Center" },      

            // 8x 倍镜
            { 569, "Center" },

            // 特殊倍镜（智慧核心等）
            { 12028, "AimMarkerRotateParent" }, // 快速瞄具[智慧核心]
            { 12031, "AimMarkerRotateParent" }, // 狙击镜[智慧核心]
        };

        private Dictionary<Transform, bool> originalChildStates = new Dictionary<Transform, bool>();
        private ADSAimMarker currentAdsMarker = null;
        private bool debugPrintEnabled = false; // 调试完成后设为 false
        private HashSet<int> printedScopeTypes = new HashSet<int>();
        private float lastRestoreLogTime = -10f;
        private const float RestoreLogInterval = 5f;
        #endregion

        #region 准星隐藏方法
        private void UpdateAdsCrosshair(int scopeTypeID)
        {
            if (!isFirstPersonMode) return;

            ADSAimMarker adsMarker = GetCurrentAdsMarker();
            if (adsMarker == null) return;

            if (currentAdsMarker != adsMarker)
            {
                RestoreAdsCrosshair();
                currentAdsMarker = adsMarker;
            }

            var followUI = adsMarker.followUI;
            if (followUI == null) return;

            if (originalChildStates.Count == 0)
            {
                SaveOriginalStatesRecursive(followUI);
            }

            if (debugPrintEnabled && !printedScopeTypes.Contains(scopeTypeID))
            {
                DebugPrintAdsCrosshairChildren(adsMarker, scopeTypeID);
                printedScopeTypes.Add(scopeTypeID);
            }

            // 获取中心点物体名称（如果配置了）
            string centerName;
            if (scopeCenterPartNames.TryGetValue(scopeTypeID, out centerName))
            {
                // 配置了中心点：隐藏所有非中心点物体
                HideAllExceptCenterRecursive(followUI, centerName);
            }
            else
            {
                // 未配置：使用通用规则，隐藏所有名字包含 "frame" 的子物体
                HideFrameChildrenRecursive(followUI);
            }
        }

        private void RestoreAdsCrosshair()
        {
            if (Time.unscaledTime - lastRestoreLogTime > RestoreLogInterval)
            {
                // FPLogger.Log("[AdsCrosshair] RestoreAdsCrosshair called");
                lastRestoreLogTime = Time.unscaledTime;
            }

            foreach (var kvp in originalChildStates)
            {
                if (kvp.Key != null)
                    kvp.Key.gameObject.SetActive(kvp.Value);
            }
            originalChildStates.Clear();
            currentAdsMarker = null;
        }

        private void SaveOriginalStatesRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                originalChildStates[child] = child.gameObject.activeSelf;
                SaveOriginalStatesRecursive(child);
            }
        }

        private void HideAllExceptCenterRecursive(Transform parent, string centerName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == centerName)
                {
                    HideAllExceptCenterRecursive(child, centerName);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void HideFrameChildrenRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLowerInvariant().Contains("frame"))
                {
                    child.gameObject.SetActive(false);
                }
                else
                {
                    HideFrameChildrenRecursive(child);
                }
            }
        }

        private ADSAimMarker GetCurrentAdsMarker()
        {
            if (cachedAimMarker == null)
                cachedAimMarker = Object.FindObjectOfType<AimMarker>();
            if (cachedAimMarker == null) return null;

            var currentAdsField = typeof(AimMarker).GetField("currentAdsAimMarker",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (currentAdsField == null) return null;

            return currentAdsField.GetValue(cachedAimMarker) as ADSAimMarker;
        }

        private void DebugPrintAdsCrosshairChildren(ADSAimMarker adsMarker, int scopeTypeID)
        {
            if (adsMarker == null) return;
            var followUI = adsMarker.followUI;
            if (followUI == null) return;

            FPLogger.Log($"=== 准星子物体列表 (ScopeTypeID={scopeTypeID}, {adsMarker.name}) ===");
            PrintChildrenRecursive(followUI, 0);
        }

        private void PrintChildrenRecursive(Transform parent, int depth)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string indent = new string(' ', depth * 2);
                FPLogger.Log($"{indent}[{i}] {child.name} (Active: {child.gameObject.activeSelf})");
                PrintChildrenRecursive(child, depth + 1);
            }
        }

        public void DebugPrintCurrentAdsCrosshair()
        {
            ADSAimMarker adsMarker = GetCurrentAdsMarker();
            if (adsMarker == null)
            {
                FPLogger.Log("[AdsCrosshair] 当前没有活动的ADS准星");
                return;
            }
            DebugPrintAdsCrosshairChildren(adsMarker, -1);
        }

        /// <summary>
        /// 强制重建 ADS 准星，用于武器切换后立即应用新准星的隐藏规则
        /// </summary>
        public void ForceRecreateAdsMarker()
        {
            if (!isFirstPersonMode) return;

            // 获取 AimMarker 实例
            if (cachedAimMarker == null)
                cachedAimMarker = Object.FindObjectOfType<AimMarker>();
            if (cachedAimMarker == null) return;

            // 获取当前武器
            var gun = mainCharacter?.GetGun();
            if (gun == null) return;

            // 获取当前武器应使用的 ADS 准星预制体
            var aimMarkerPfb = gun.GetAimMarkerPfb();

            if (aimMarkerPfb == null)
            {
                // 没有倍镜，恢复原始准星
                RestoreAdsCrosshair();
                return;
            }

            // 通过反射调用 AimMarker 的私有方法 SwitchAdsAimMarker
            var method = typeof(AimMarker).GetMethod("SwitchAdsAimMarker",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(cachedAimMarker, new object[] { aimMarkerPfb });
            }

            // 清除内部缓存，确保下一帧重新应用隐藏规则
            currentAdsMarker = null;
            originalChildStates.Clear();
        }
        #endregion
    }
}