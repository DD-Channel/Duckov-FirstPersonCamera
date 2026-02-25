using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 准星隐藏字段
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

            // 递归隐藏所有名字包含 "frame" 的子物体
            HideFrameChildrenRecursive(followUI);
        }

        private void RestoreAdsCrosshair()
        {
            if (Time.unscaledTime - lastRestoreLogTime > RestoreLogInterval)
            {
                FPLogger.Log("[AdsCrosshair] RestoreAdsCrosshair called");
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
        #endregion
    }
}