using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        private const string HitFxEnableKey = "FirstPersonCamera_HitFx_Enable";
        private const string HitFxPostStrengthKey = "FirstPersonCamera_HitFx_PostStrength";
        private const string HitFxUiStrengthKey = "FirstPersonCamera_HitFx_UiStrength";
        private const string HitFxShowDirKey = "FirstPersonCamera_HitFx_ShowDirection";
        private const float HitFxVolumePriority = 0f;
        private const int HitFxCanvasSortOrder = 5000;
        private const float DefaultDamageReference = 30f;
        private const float HitFxPeakTime = 0.12f;
        private const float HitFxDecayTime = 0.55f;
        private const float VignetteBaseIntensity = 0.45f;
        private const float ChromaticAberrationBaseIntensity = 0.35f;
        private const float LensDistortionBaseIntensity = -0.05f;
        private const float ColorSaturationBaseIntensity = -10f;
        private const float UiFlashBaseAlpha = 0.35f;
        private static readonly Color VignetteColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        private static readonly Color UiFlashColor = new Color(0.7f, 0.05f, 0.05f, 0f);
        private const float VignetteSmoothness = 0.8f;
        private const float DefaultPostStrength = 0.75f;
        private const float DefaultUiStrength = 0.60f;
        private const float Epsilon = 0.0001f;

        // 方向指示器可配置参数（已按反馈修改）
        private const float DirectionArcSize = 300f;                // 圆弧的大小（像素）
        private const float DirectionArcOffset = 350f;               // 圆弧距离屏幕中心的距离（像素）
        private const float DirectionArcIntensity = 0.8f;            // 圆弧最大透明度
        private static readonly Color DirectionArcColor = Color.red; // 改为纯红色
        private const float ArcAngle = 60f;                           // 圆弧的张角（度）
        #endregion

        #region 私有字段
        private Volume hitFxVolume;
        private VolumeProfile hitFxProfile;
        private Vignette fxVignette;
        private ChromaticAberration fxCA;
        private LensDistortion fxLD;
        private ColorAdjustments fxColor;
        private Canvas hitFxCanvas;
        private Image hitFlashImage;
        private Image directionArc;        // 圆弧方向指示器
        private Coroutine hitFxRoutine;
        #endregion

        #region 初始化方法
        private void EnsureHitFxSetup()
        {
            if (hitFxVolume == null) InitializePostProcessingVolume();
            if (hitFxCanvas == null) InitializeHitFxCanvas();
        }

        private void InitializePostProcessingVolume()
        {
            var volumeGO = new GameObject("FPC_HitFxVolume");
            volumeGO.transform.SetParent(this.transform, false);
            hitFxVolume = volumeGO.AddComponent<Volume>();
            hitFxVolume.isGlobal = true;
            hitFxVolume.priority = HitFxVolumePriority;
            hitFxProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            hitFxVolume.sharedProfile = hitFxProfile;

            fxVignette = hitFxProfile.Add<Vignette>(true);
            fxVignette.active = true;
            fxVignette.intensity.Override(0f);
            fxVignette.smoothness.Override(VignetteSmoothness);
            fxVignette.color.Override(VignetteColor);

            fxCA = hitFxProfile.Add<ChromaticAberration>(true);
            fxCA.active = true;
            fxCA.intensity.Override(0f);

            fxLD = hitFxProfile.Add<LensDistortion>(true);
            fxLD.active = true;
            fxLD.intensity.Override(0f);
            fxLD.scale.Override(1f);

            fxColor = hitFxProfile.Add<ColorAdjustments>(true);
            fxColor.active = true;
            fxColor.saturation.Override(0f);
        }

        private void InitializeHitFxCanvas()
        {
            if (uiDefaultSprite == null)
            {
                try { uiDefaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd"); }
                catch { uiDefaultSprite = null; }
            }

            var canvasGO = new GameObject("FPC_HitFxCanvas");
            hitFxCanvas = canvasGO.AddComponent<Canvas>();
            hitFxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hitFxCanvas.sortingOrder = HitFxCanvasSortOrder;
            canvasGO.AddComponent<CanvasScaler>();
            var graphicRaycaster = canvasGO.AddComponent<GraphicRaycaster>();
            graphicRaycaster.enabled = false;

            // 全屏闪烁
            var flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(canvasGO.transform, false);
            hitFlashImage = flashGO.AddComponent<Image>();
            if (uiDefaultSprite != null) hitFlashImage.sprite = uiDefaultSprite;
            hitFlashImage.type = Image.Type.Sliced;
            hitFlashImage.color = UiFlashColor;
            var flashRect = hitFlashImage.rectTransform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            // 创建圆弧方向指示器
            CreateDirectionArc(canvasGO.transform);
        }

        private void CreateDirectionArc(Transform parent)
        {
            var arcGO = new GameObject("DirectionArc");
            arcGO.transform.SetParent(parent, false);

            directionArc = arcGO.AddComponent<Image>();
            directionArc.sprite = CreateArcSprite();
            directionArc.raycastTarget = false;
            directionArc.color = DirectionArcColor; // 设置颜色

            var arcRect = directionArc.rectTransform;
            arcRect.anchorMin = new Vector2(0.5f, 0.5f);
            arcRect.anchorMax = new Vector2(0.5f, 0.5f);
            arcRect.pivot = new Vector2(0.5f, 0.5f);
            arcRect.sizeDelta = new Vector2(DirectionArcSize, DirectionArcSize);
            arcRect.anchoredPosition = Vector2.zero;

            directionArc.gameObject.SetActive(false);
        }

        private Sprite CreateArcSprite()
        {
            // 增大纹理分辨率以匹配放大后的尺寸，避免模糊
            int size = 128; // 原64，现在128以获得更清晰的效果
            Texture2D texture = new Texture2D(size, size);
            Color transparent = new Color(0, 0, 0, 0);
            Color white = Color.white;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.4f;               // 外半径
            float innerRadius = radius * 0.8f;        // 内半径（弧形宽度 = radius * 0.2f）
            float halfAngle = ArcAngle / 2f * Mathf.Deg2Rad;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector2 pixel = new Vector2(x, y);
                    Vector2 dir = pixel - center;
                    float distance = dir.magnitude;

                    // 绘制一个指向右的圆弧（中心线角度0°）
                    if (distance > innerRadius && distance < radius)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x);
                        if (Mathf.Abs(angle) <= halfAngle)
                        {
                            texture.SetPixel(x, y, white);
                            continue;
                        }
                    }
                    texture.SetPixel(x, y, transparent);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        #endregion

        #region 选项加载
        private float LoadHitFxPostStrength()
        {
            try { return Mathf.Clamp01(FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(HitFxPostStrengthKey, DefaultPostStrength)); }
            catch { return DefaultPostStrength; }
        }

        private float LoadHitFxUiStrength()
        {
            try { return Mathf.Clamp01(FirstPersonCamera.Utilities.OptionsHelper.LoadFloat(HitFxUiStrengthKey, DefaultUiStrength)); }
            catch { return DefaultUiStrength; }
        }

        private bool LoadHitFxEnable()
        {
            try { return FirstPersonCamera.Utilities.OptionsHelper.LoadInt(HitFxEnableKey, 1) == 1; }
            catch { return true; }
        }

        private bool LoadHitFxShowDir()
        {
            try { return FirstPersonCamera.Utilities.OptionsHelper.LoadInt(HitFxShowDirKey, 1) == 1; }
            catch { return true; }
        }
        #endregion

        #region 公共接口
        public void OnHitEffect(float damage, Vector3? damagePointWorld)
        {
            if (!isFirstPersonMode) return;
            if (!LoadHitFxEnable()) return;

            EnsureHitFxSetup();

            float clampedDamage = Mathf.Max(0f, damage);
            float postStrength = LoadHitFxPostStrength();
            float uiStrength = LoadHitFxUiStrength();
            bool showDir = LoadHitFxShowDir();

            float intensityFactor = Mathf.Clamp01(Mathf.Sqrt(clampedDamage / DefaultDamageReference));

            // 计算水平角度（用于方向指示）
            float angleDeg = 0f;
            if (showDir && damagePointWorld.HasValue && mainCamera != null)
            {
                Vector3 toDamage = damagePointWorld.Value - mainCamera.transform.position;
                toDamage.y = 0;
                if (toDamage.sqrMagnitude > 0.001f)
                {
                    toDamage.Normalize();
                    Vector3 forward = mainCamera.transform.forward;
                    forward.y = 0;
                    forward.Normalize();
                    angleDeg = Vector3.SignedAngle(forward, toDamage, Vector3.up);
                }
            }

            if (hitFxRoutine != null)
            {
                StopCoroutine(hitFxRoutine);
            }
            hitFxRoutine = StartCoroutine(CoHitFx(intensityFactor, postStrength, uiStrength, showDir, angleDeg));

            StartCoroutine(ReapplyObstructionHidingAfterHit());
        }

        private IEnumerator ReapplyObstructionHidingAfterHit()
        {
            yield return null;
            yield return new WaitForSeconds(0.1f);
            try { HideFirstPersonObstructions(); } catch { }
        }
        #endregion

        #region 特效协程
        private IEnumerator CoHitFx(float intensityFactor, float postStrength, float uiStrength, bool showDir, float angleDeg)
        {
            float elapsedTime = 0f;
            float totalDuration = HitFxPeakTime + HitFxDecayTime;

            if (hitFlashImage != null)
            {
                var color = hitFlashImage.color;
                color.a = 0f;
                hitFlashImage.color = color;
            }

            // 初始化圆弧指示器（透明）
            if (directionArc != null)
            {
                directionArc.gameObject.SetActive(showDir);
                var color = directionArc.color;
                color.a = 0f;
                directionArc.color = color;
            }

            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float animationProgress = 0f;
                if (elapsedTime <= HitFxPeakTime)
                    animationProgress = elapsedTime / Mathf.Max(Epsilon, HitFxPeakTime);
                else
                {
                    float decayProgress = (elapsedTime - HitFxPeakTime) / Mathf.Max(Epsilon, HitFxDecayTime);
                    animationProgress = Mathf.Clamp01(1f - decayProgress);
                }

                ApplyPostProcessingEffects(postStrength, intensityFactor, animationProgress);
                ApplyUiFlashEffect(uiStrength, intensityFactor, animationProgress);

                if (showDir && directionArc != null)
                {
                    UpdateDirectionArc(angleDeg, uiStrength, intensityFactor, animationProgress);
                }

                yield return null;
            }

            ClearAllEffects();
        }

        /// <summary>
        /// 更新圆弧指示器的位置和旋转（基于角度）
        /// </summary>
        private void UpdateDirectionArc(float angleDeg, float uiStrength, float intensityFactor, float animationProgress)
        {
            if (directionArc == null) return;

            // 将角度转换为弧度，计算方向向量（从屏幕中心指向伤害来源）
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

            // 计算圆弧位置：沿方向向量移动固定距离
            Vector2 arcPos = direction * DirectionArcOffset;

            // 设置位置
            directionArc.rectTransform.anchoredPosition = arcPos;

            // 设置旋转：使圆弧的中心线指向外部（即指向伤害来源）
            directionArc.rectTransform.rotation = Quaternion.FromToRotation(Vector2.right, direction);

            // 计算透明度
            float alpha = DirectionArcIntensity * uiStrength * intensityFactor * animationProgress;
            var color = directionArc.color;
            color.a = alpha;
            directionArc.color = color;
        }
        #endregion

        #region 效果应用与清除
        private void ApplyPostProcessingEffects(float postStrength, float intensityFactor, float animationProgress)
        {
            float combinedIntensity = postStrength * intensityFactor * animationProgress;
            if (fxVignette != null) fxVignette.intensity.value = VignetteBaseIntensity * combinedIntensity;
            if (fxCA != null) fxCA.intensity.value = ChromaticAberrationBaseIntensity * combinedIntensity;
            if (fxLD != null) fxLD.intensity.value = LensDistortionBaseIntensity * combinedIntensity;
            if (fxColor != null) fxColor.saturation.value = ColorSaturationBaseIntensity * combinedIntensity;
        }

        private void ApplyUiFlashEffect(float uiStrength, float intensityFactor, float animationProgress)
        {
            if (hitFlashImage != null)
            {
                var color = hitFlashImage.color;
                color.a = UiFlashBaseAlpha * uiStrength * intensityFactor * animationProgress;
                hitFlashImage.color = color;
            }
        }

        private void ClearAllEffects()
        {
            if (fxVignette != null) fxVignette.intensity.value = 0f;
            if (fxCA != null) fxCA.intensity.value = 0f;
            if (fxLD != null) fxLD.intensity.value = 0f;
            if (fxColor != null) fxColor.saturation.value = 0f;

            if (hitFlashImage != null)
            {
                var color = hitFlashImage.color;
                color.a = 0f;
                hitFlashImage.color = color;
            }

            if (directionArc != null)
            {
                directionArc.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}