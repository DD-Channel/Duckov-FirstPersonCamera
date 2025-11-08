using System;
using System.Reflection;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FirstPersonCamera.Patches.UI
{
    /// <summary>
    /// Extends the inventory item right-click menu (ItemOperationMenu) with a new "检视" option.
    /// - Weapons/Melee: call FirstPerson inspect flows (requires item currently equipped)
    /// - UI-like items: show the item icon at screen center for 3s
    /// - 3D modeled items: reserved for future (no-op for now)
    /// </summary>
    [HarmonyPatch]
    internal static class InventoryInspectPatch
    {
        // Cache reflection for private fields in ItemOperationMenu
        private static readonly Type MenuType = typeof(ItemOperationMenu);
        private static readonly FieldInfo F_BtnUse = AccessTools.Field(MenuType, "btn_Use");
        private static readonly FieldInfo F_BtnWishlist = AccessTools.Field(MenuType, "btn_Wishlist");
        private static readonly FieldInfo F_TargetDisplay = AccessTools.Field(MenuType, "TargetDisplay");

        // Component that actually manages the extra button instance
        private class InspectMenuExtension : MonoBehaviour
        {
            public ItemOperationMenu owner;
            public Button inspectButton;
            public TextMeshProUGUI inspectLabel;
            private bool styleApplied;

            public void EnsureButton()
            {
                if (owner == null) owner = GetComponent<ItemOperationMenu>();
                if (owner == null) return;

                if (inspectButton != null && inspectLabel != null) return;

                // Try clone an existing button as template (prefer Wishlist, else Use)
                var templateBtn = (Button)(F_BtnWishlist?.GetValue(owner) as Button) ?? (Button)(F_BtnUse?.GetValue(owner) as Button);
                if (templateBtn == null) return;

                var templateGO = templateBtn.gameObject;
                var parent = templateGO.transform.parent;
                var newGO = GameObject.Instantiate(templateGO, parent);
                newGO.name = "Btn_Inspect";
                newGO.transform.localScale = Vector3.one;
                // Clear listeners of the cloned button
                var btn = newGO.GetComponent<Button>();
                if (btn == null) return;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(new UnityAction(OnInspectClicked));
                inspectButton = btn;

                inspectLabel = newGO.GetComponentInChildren<TextMeshProUGUI>(true);
                ApplyButtonStyle();
            }

            public void RefreshForCurrentTarget()
            {
                if (owner == null) owner = GetComponent<ItemOperationMenu>();
                if (owner == null) return;
                EnsureButton();
                if (inspectButton == null) return;

                var item = TryGetTargetItem(owner);
                var available = item != null;
                inspectButton.gameObject.SetActive(available);

                if (!available) return;

                // Option always visible for now; future: adjust visibility based on type
                // Force label and color after menu setup to avoid localization overwriting
                ApplyButtonStyle();
                
                // 延迟设置文本，确保在本地化系统之后设置（避免被覆盖）
                if (this != null && gameObject != null && gameObject.activeInHierarchy)
                {
                    StartCoroutine(DelayedApplyButtonText());
                }
            }
            
            private System.Collections.IEnumerator DelayedApplyButtonText()
            {
                // 等待一帧，确保菜单完全初始化
                yield return null;
                
                // 再次设置文本，防止被本地化系统覆盖
                if (inspectLabel != null)
                {
                    inspectLabel.text = "检视";
                    inspectLabel.color = Color.white;
                    inspectLabel.gameObject.SetActive(true);
                    inspectLabel.enabled = true;
                }
                
                // 再等待一帧，再次确保文本设置
                yield return null;
                if (inspectLabel != null)
                {
                    inspectLabel.text = "检视";
                }
            }

            private static Item TryGetTargetItem(ItemOperationMenu menu)
            {
                try
                {
                    var targetDisplay = F_TargetDisplay?.GetValue(menu) as ItemDisplay;
                    return targetDisplay != null ? targetDisplay.Target : null;
                }
                catch
                {
                    return null;
                }
            }

            private void OnInspectClicked()
            {
                try
                {
                    if (owner == null) owner = GetComponent<ItemOperationMenu>();
                    var item = TryGetTargetItem(owner);
                    if (item == null)
                    {
                        owner?.Close();
                        return;
                    }

                    // Unified dispatch via controller for any item type
                    var fpController = FirstPersonCameraController.Instance;
                    if (fpController != null)
                    {
                        fpController.StartInspectForAnyItem(item);
                    }
                    owner?.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[FirstPersonCamera] Inspect button click failed: " + ex);
                    try { owner?.Close(); } catch { }
                }
            }

            private void ApplyButtonStyle()
            {
                if (inspectButton == null) return;
                
                // Text - 确保找到或创建文本组件
                if (inspectLabel == null)
                {
                    // 首先尝试查找现有的文本组件
                    inspectLabel = inspectButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    
                    // 如果找不到，尝试查找所有子对象
                    if (inspectLabel == null)
                    {
                        var allTexts = inspectButton.GetComponentsInChildren<TextMeshProUGUI>(true);
                        if (allTexts != null && allTexts.Length > 0)
                        {
                            inspectLabel = allTexts[0];
                        }
                    }
                    
                    // 如果还是找不到，创建一个新的文本标签
                    if (inspectLabel == null)
                    {
                        // Create a new text label if template has no text (e.g., icon-only buttons)
                        var labelGO = new GameObject("Label");
                        labelGO.transform.SetParent(inspectButton.transform, false);
                        var rt = labelGO.AddComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMin = 12f;
                        tmp.fontSizeMax = 28f;
                        tmp.raycastTarget = false;
                        inspectLabel = tmp;
                    }
                }
                
                // 强制设置文本和样式
                if (inspectLabel != null)
                {
                    inspectLabel.text = "检视";
                    inspectLabel.color = Color.white;
                    inspectLabel.gameObject.SetActive(true);
                    inspectLabel.enabled = true;
                    // 确保文本组件在正确的层级
                    inspectLabel.transform.SetAsLastSibling();
                }

                // Button blue color theme
                var img = inspectButton.targetGraphic as Image;
                // Lighter blue theme
                var baseBlue = new Color(0.44f, 0.72f, 1.00f, 1f);      // ~#70B8FF
                var highlight = new Color(0.60f, 0.82f, 1.00f, 1f);     // lighter on hover
                var pressed = new Color(0.32f, 0.58f, 0.94f, 1f);        // slightly darker when pressed

                if (img != null)
                {
                    img.color = baseBlue;
                }
                var cb = inspectButton.colors;
                cb.normalColor = baseBlue;
                cb.highlightedColor = highlight;
                cb.pressedColor = pressed;
                cb.selectedColor = baseBlue;
                // keep disabledColor as is
                inspectButton.colors = cb;

                styleApplied = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemOperationMenu), "Awake")]
        private static void ItemOperationMenu_Awake_Postfix(ItemOperationMenu __instance)
        {
            try
            {
                if (__instance == null) return;
                var extender = __instance.gameObject.GetComponent<InspectMenuExtension>();
                if (extender == null)
                {
                    extender = __instance.gameObject.AddComponent<InspectMenuExtension>();
                    extender.owner = __instance;
                }
                extender.EnsureButton();
                extender.RefreshForCurrentTarget();
            }
            catch (Exception ex)
            {
                Debug.LogError("[FirstPersonCamera] Failed to init InspectMenuExtension: " + ex);
            }
        }

        // Run after the menu selects a new target and sets up built-in buttons
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemOperationMenu), "MShow")]
        private static void ItemOperationMenu_MShow_Postfix(ItemOperationMenu __instance)
        {
            try
            {
                if (__instance == null) return;
                var extender = __instance.gameObject.GetComponent<InspectMenuExtension>();
                if (extender != null)
                {
                    extender.RefreshForCurrentTarget();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[FirstPersonCamera] Failed to refresh Inspect button: " + ex);
            }
        }
    }
}
