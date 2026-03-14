using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstPersonCamera.Utilities;

namespace FirstPersonCamera.UI
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        private TextMeshProUGUI textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                // Debug.LogError($"[LocalizedText] No TextMeshProUGUI found on {gameObject.name}, disabling.");
                enabled = false;
                return;
            }
            // Debug.Log($"[LocalizedText] Awake on {gameObject.name}, key={localizationKey}");
            UpdateText();
            FPLocalization.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            FPLocalization.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            // Debug.Log($"[LocalizedText] OnLanguageChanged received on {gameObject.name}, key={localizationKey}");
            if (textComponent != null)
                UpdateText();
        }

        private void OnEnable()
        {
            // 对象激活时，确保文本为当前语言，并重建布局
            if (textComponent != null && !string.IsNullOrEmpty(localizationKey))
            {
                // 重新应用文本（可能在上次语言切换时已经设置，但以防万一）
                textComponent.text = FPLocalization.Get(localizationKey);
                StartCoroutine(RebuildLayoutNextFrame());
            }
        }

        public void UpdateText()
        {
            if (textComponent == null || string.IsNullOrEmpty(localizationKey))
                return;

            string newText = FPLocalization.Get(localizationKey);
            // Debug.Log($"[LocalizedText] Updating {gameObject.name} from '{textComponent.text}' to '{newText}'");
            textComponent.text = newText;

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(RebuildLayoutNextFrame());
            }
        }

        public void SetKey(string key)
        {
            localizationKey = key;
            if (textComponent != null)
                UpdateText();
        }

        private IEnumerator RebuildLayoutNextFrame()
        {
            yield return null;
            if (transform == null) yield break;

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
            }
        }
    }
}