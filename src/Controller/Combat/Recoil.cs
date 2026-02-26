using UnityEngine;
using FirstPersonCamera.Utilities;
using FirstPersonCamera.OptionsUI;
using System.Collections.Generic;

namespace FirstPersonCamera
{
    // 武器类别枚举
    public enum WeaponCategory
    {
        Unknown,
        PistolSMG,          // 手枪/冲锋枪
        Rifle,              // 步枪
        Shotgun,            // 霰弹枪
        Sniper,             // 狙击枪
        MarksmanRifle       // 精确步枪
    }

    public partial class FirstPersonCameraController
    {
        // ************** 武器 TypeID 集合（请根据实际游戏填写） **************

        /// <summary>
        /// 手枪/冲锋枪的 TypeID 集合
        /// </summary>
        private static readonly HashSet<int> PistolSMGIds = new HashSet<int>
        {
            252,254,258,260,262,391,450,655,733,734,735,736,737,783,784,914,915,916,917,943,
            946,1058,1060,1061,1128,1209,1302,1396,1433,10108,10112,10116,10117,10118,10119,
            10120,10121,10136,10141,95606,95615,95804
        };

        /// <summary>
        /// 步枪的 TypeID 集合
        /// </summary>
        private static readonly HashSet<int> RifleIds = new HashSet<int>
        {
            238,240,242,244,256,653,654,659,680,681,682,683,788,862,1055,1056,1238,1260,1286,
            1287,1300,1301,1362,1374,1521,10101,10102,10104,10105,10106,10107,10114,10122,
            10123,10124,10125,10126,10127,10129,10137,10138,10139,10140,95601,95602,95613,95612,
            95803,95806,95807,95814
        };

        /// <summary>
        /// 狙击枪的 TypeID 集合
        /// </summary>
        private static readonly HashSet<int> SniperIds = new HashSet<int>
        {
            246,327,357,407,437,780,781,782,785,1057,1289,1298,1299,1480,10109,
            10111,10113,10115,10135,10143,95605,95604,95616,95808
        };

        /// <summary>
        /// 精确步枪的 TypeID 集合（请填写实际ID）
        /// </summary>
        private static readonly HashSet<int> MarksmanRifleIds = new HashSet<int>
        {
            652,656,786,787,10103,10128,1497,10142,95805
        };

        // 如果你也想用 TypeID 控制霰弹枪（而不是依靠 ShotCount），可以取消下面注释并填写
        // private static readonly HashSet<int> ShotgunIds = new HashSet<int>
        // {
        //     // 霰弹枪 TypeID
        // };

        #region 后坐力系统字段
        /// <summary>累积的垂直后坐力（用于恢复）</summary>
        private float accumulatedRecoilV = 0f;
        /// <summary>累积的水平后坐力（用于恢复）</summary>
        private float accumulatedRecoilH = 0f;
        /// <summary>当前正在使用的武器（用于检测切换）</summary>
        private ItemAgent_Gun currentRecoilGun = null;
        /// <summary>水平后坐力方向模式计数器</summary>
        private int horizontalRecoilPattern = 0;
        /// <summary>上一次的水平方向（用于模式延续）</summary>
        private float lastHorizontalDirection = 1f;
        /// <summary>是否正在连续射击</summary>
        private bool isShooting = false;
        /// <summary>最近几次的垂直后坐力（用于鼠标松开时的重置）</summary>
        private List<float> recentRecoilV = new List<float>();
        /// <summary>最近几次的水平后坐力（用于鼠标松开时的重置）</summary>
        private List<float> recentRecoilH = new List<float>();
        /// <summary>最多保留的最近后坐力次数</summary>
        private const int MAX_RECENT_RECOIL = 3;
        /// <summary>水平后坐力模式重置阈值（每10次重置方向模式）</summary>
        private const int HORIZONTAL_RECOIL_PATTERN_RESET = 10;
        /// <summary>水平方向延续概率</summary>
        private const float HORIZONTAL_DIRECTION_CONTINUE_CHANCE = 0.7f;
        /// <summary>水平方向随机翻转概率</summary>
        private const float HORIZONTAL_DIRECTION_RANDOM_CHANCE = 0.5f;
        /// <summary>高斯随机数的标准差</summary>
        private const float GAUSSIAN_STD_DEV = 0.5f;
        /// <summary>高斯随机数的基础乘数</summary>
        private const float GAUSSIAN_BASE_MULTIPLIER = 1.35f;
        /// <summary>高斯随机数的范围乘数</summary>
        private const float GAUSSIAN_RANGE_MULTIPLIER = 0.6f;

        // 各类武器后坐力乘数（可随时调整数值以适配手感）
        /// <summary>霰弹枪垂直后坐力乘数</summary>
        private float shotgunVerticalRecoilMultiplier = 2.0f;
        /// <summary>霰弹枪水平后坐力乘数</summary>
        private float shotgunHorizontalRecoilMultiplier = 1.3f;
        /// <summary>手枪/冲锋枪垂直后坐力乘数</summary>
        private float smgVerticalRecoilMultiplier = 1.0f;
        /// <summary>手枪/冲锋枪水平后坐力乘数</summary>
        private float smgHorizontalRecoilMultiplier = 1.0f;
        /// <summary>狙击枪垂直后坐力乘数</summary>
        private float sniperVerticalRecoilMultiplier = 0.2f;
        /// <summary>狙击枪水平后坐力乘数</summary>
        private float sniperHorizontalRecoilMultiplier = 0.5f;
        /// <summary>步枪垂直后坐力乘数</summary>
        private float rifleVerticalRecoilMultiplier = 1.0f;
        /// <summary>步枪水平后坐力乘数</summary>
        private float rifleHorizontalRecoilMultiplier = 1.0f;
        /// <summary>精确步枪垂直后坐力乘数</summary>
        private float marksmanVerticalRecoilMultiplier = 0.5f;
        /// <summary>精确步枪水平后坐力乘数</summary>
        private float marksmanHorizontalRecoilMultiplier = 0.5f;
        /// <summary>未知武器垂直后坐力乘数（默认值）</summary>
        private float unknownVerticalRecoilMultiplier = 1.0f;
        /// <summary>未知武器水平后坐力乘数（默认值）</summary>
        private float unknownHorizontalRecoilMultiplier = 1.0f;

        // ========== 每发回弹比例（射击后立即恢复一小部分，实现“轻微回弹”效果） ==========
        // 数值表示恢复刚施加后坐力的比例（0 = 无回弹，0.1 = 恢复10%）
        /// <summary>步枪每发回弹比例</summary>
        private float riflePerShotRecovery = 0.08f;      // 轻微回弹
        /// <summary>手枪/冲锋枪每发回弹比例</summary>
        private float smgPerShotRecovery = 0.05f;
        /// <summary>霰弹枪每发回弹比例（可能为0）</summary>
        private float shotgunPerShotRecovery = 0f;       // 霰弹枪不回弹
        /// <summary>精确步枪每发回弹比例</summary>
        private float marksmanPerShotRecovery = 0.1f;
        /// <summary>未知武器每发回弹比例</summary>
        private float unknownPerShotRecovery = 0.08f;
        /// <summary>狙击枪每发回弹比例（狙击枪停止后恢复，射击中可设为0）</summary>
        private float sniperPerShotRecovery = 0f;        // 狙击枪射击中不回弹

        /// <summary>是否暂停后坐力恢复（用于武器切换等）</summary>
        private bool recoveryPaused = false;
        #endregion

        #region 后坐力应用逻辑
        /// <summary>
        /// 鼠标松开时重置后坐力，保留最近三次的累积值
        /// </summary>
        private void HandleMouseReleaseRecoilReset()
        {
            if (!isShooting) return;
            if (currentRecoilGun != null && currentRecoilGun.ShotCount > 1)
                return; // 霰弹枪不重置
            isShooting = false;

            float last3RecoilV = 0f, last3RecoilH = 0f;
            for (int i = 0; i < recentRecoilV.Count; i++)
            {
                last3RecoilV += recentRecoilV[i];
                last3RecoilH += recentRecoilH[i];
            }
            accumulatedRecoilV = last3RecoilV;
            accumulatedRecoilH = last3RecoilH;
            recentRecoilV.Clear();
            recentRecoilH.Clear();
        }

        /// <summary>
        /// 角色射击事件处理，计算并应用后坐力
        /// </summary>
        private void OnCharacterShoot(DuckovItemAgent agent)
        {
            if (!isFirstPersonMode) return;
            if (agent == null) return;

            ItemAgent_Gun gun = agent as ItemAgent_Gun;
            if (gun == null) return;

            try
            {
                bool recoilEnabled = OptionsHelper.LoadInt(FirstPersonOptionsUI.EnableRecoilKey, 1) == 1;
                if (!recoilEnabled) return;

                float recoilMult = 1f;
                try { recoilMult = LevelManager.Rule.RecoilMultiplier; } catch { }

                float strengthMult = OptionsHelper.LoadFloat(FirstPersonOptionsUI.RecoilStrengthKey, 0.1f);

                // 原版后坐力基础值映射
                float origVMin = gun.RecoilVMin;
                float origVMax = gun.RecoilVMax;
                float origHMin = gun.RecoilHMin;
                float origHMax = gun.RecoilHMax;

                float baseV = MapRecoilValue(UnityEngine.Random.Range(origVMin, origVMax));
                float baseH = MapRecoilValue(UnityEngine.Random.Range(origHMin, origHMax));

                // 散布动态因子
                float currentScatter = gun.CurrentScatter;
                float defaultScatter = gun.DefaultScatter;
                float maxScatter = gun.MaxScatter;
                float scatterGrow = gun.ScatterGrow;
                float scatterRatio = Mathf.Clamp01((currentScatter - defaultScatter) / Mathf.Max(0.01f, maxScatter - defaultScatter));
                float scatterFactor = 1f + scatterRatio * 1f;

                // 垂直后坐力
                float v = baseV * scatterFactor * gun.RecoilScaleV * (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * recoilMult * strengthMult;

                // 水平方向
                float hDirection;
                if (horizontalRecoilPattern == 0)
                {
                    hDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                }
                else
                {
                    float patternChance = UnityEngine.Random.value;
                    hDirection = patternChance < HORIZONTAL_DIRECTION_CONTINUE_CHANCE ? lastHorizontalDirection : -lastHorizontalDirection;
                    if (UnityEngine.Random.value < HORIZONTAL_DIRECTION_RANDOM_CHANCE)
                        hDirection = -hDirection;
                }

                float gaussianFactor = Mathf.Clamp01(Mathf.Abs(GaussianRandom(0f, GAUSSIAN_STD_DEV)));
                float hBase = baseH * (GAUSSIAN_BASE_MULTIPLIER + GAUSSIAN_RANGE_MULTIPLIER * gaussianFactor);
                float h = hBase * scatterFactor * hDirection * gun.RecoilScaleH * (1f / Mathf.Max(0.01f, gun.CharacterRecoilControl)) * recoilMult * strengthMult;

                // 瞄准状态调整
                float adsValue = gun.AdsValue;
                if (adsValue > 0f)
                {
                    v *= 1.5f;
                    h *= 1.5f;
                }

                // ========== 根据武器类别应用后坐力乘数 ==========
                WeaponCategory category = GetWeaponCategory(gun);
                FPLogger.Log($"[Recoil] 武器类别: {category}");

                switch (category)
                {
                    case WeaponCategory.Shotgun:
                        v *= shotgunVerticalRecoilMultiplier;
                        h *= shotgunHorizontalRecoilMultiplier;
                        FPLogger.Log("[Recoil] 霰弹枪乘数应用");
                        break;
                    case WeaponCategory.PistolSMG:
                        v *= smgVerticalRecoilMultiplier;
                        h *= smgHorizontalRecoilMultiplier;
                        FPLogger.Log("[Recoil] 冲锋枪/手枪乘数应用");
                        break;
                    case WeaponCategory.Sniper:
                        v *= sniperVerticalRecoilMultiplier;
                        h *= sniperHorizontalRecoilMultiplier;
                        FPLogger.Log("[Recoil] 狙击枪乘数应用");
                        break;
                    case WeaponCategory.Rifle:
                        v *= rifleVerticalRecoilMultiplier;
                        h *= rifleHorizontalRecoilMultiplier;
                        FPLogger.Log("[Recoil] 步枪乘数应用");
                        break;
                    case WeaponCategory.MarksmanRifle:
                        v *= marksmanVerticalRecoilMultiplier;
                        h *= marksmanHorizontalRecoilMultiplier;
                        FPLogger.Log("[Recoil] 精确步枪乘数应用");
                        break;
                    case WeaponCategory.Unknown:
                    default:
                        v *= unknownVerticalRecoilMultiplier;
                        h *= unknownHorizontalRecoilMultiplier;
                        FPLogger.Log("[Recoil] 未知武器乘数应用");
                        break;
                }

                // 更新模式
                horizontalRecoilPattern++;
                lastHorizontalDirection = hDirection;
                if (horizontalRecoilPattern >= HORIZONTAL_RECOIL_PATTERN_RESET)
                    horizontalRecoilPattern = 0;

                // 武器切换
                if (currentRecoilGun != null && currentRecoilGun != gun)
                {
                    horizontalRecoilPattern = 0;
                    recentRecoilV.Clear();
                    recentRecoilH.Clear();
                    recoveryPaused = true;
                    ForceRecreateAdsMarker();

                    // 重置所有平滑恢复的速度变量（防止旧武器惯性影响新武器）
                    rifleSmoothVelV = rifleSmoothVelH = 0f;
                    smgSmoothVelV = smgSmoothVelH = 0f;
                    shotgunSmoothVelV = shotgunSmoothVelH = 0f;
                    marksmanSmoothVelV = marksmanSmoothVelH = 0f;
                    unknownSmoothVelV = unknownSmoothVelH = 0f;
                    sniperSmoothVelV = sniperSmoothVelH = 0f;
                }
                currentRecoilGun = gun;
                isShooting = true;

                recentRecoilV.Add(v);
                recentRecoilH.Add(h);
                if (recentRecoilV.Count > MAX_RECENT_RECOIL)
                {
                    recentRecoilV.RemoveAt(0);
                    recentRecoilH.RemoveAt(0);
                }

                // 应用后坐力到累积值和视角
                accumulatedRecoilV += v;
                accumulatedRecoilH += h;
                pitch = Mathf.Clamp(pitch - v, -89f, 89f);
                yaw += h;

                // 视觉抖动
                ApplyGunShake(v, h, adsValue, gun);

                // ========== 射击后立即进行每发轻微回弹（除狙击枪外） ==========
                ApplyPerShotRecovery(category, v, h);

                recoveryPaused = false;
            }
            catch { }
        }

        /// <summary>
        /// 根据武器类别应用每发回弹（射击后立即恢复一小部分后坐力）
        /// </summary>
        private void ApplyPerShotRecovery(WeaponCategory category, float v, float h)
        {
            float recoverRatio = 0f;

            switch (category)
            {
                case WeaponCategory.Rifle:
                    recoverRatio = riflePerShotRecovery;
                    break;
                case WeaponCategory.PistolSMG:
                    recoverRatio = smgPerShotRecovery;
                    break;
                case WeaponCategory.Shotgun:
                    recoverRatio = shotgunPerShotRecovery;
                    break;
                case WeaponCategory.Sniper:
                    recoverRatio = sniperPerShotRecovery;
                    break;
                case WeaponCategory.MarksmanRifle:
                    recoverRatio = marksmanPerShotRecovery;
                    break;
                case WeaponCategory.Unknown:
                default:
                    recoverRatio = unknownPerShotRecovery;
                    break;
            }

            if (recoverRatio <= 0f) return;

            // 计算要恢复的量（不能超过当前累积值，且不能反向）
            float recoverV = Mathf.Min(accumulatedRecoilV, v * recoverRatio);
            float recoverH = Mathf.Min(Mathf.Abs(accumulatedRecoilH), Mathf.Abs(h) * recoverRatio) * Mathf.Sign(accumulatedRecoilH);

            if (Mathf.Abs(recoverV) > 0.001f)
            {
                accumulatedRecoilV -= recoverV;
                pitch += recoverV; // 恢复准星向上移动（因为后坐力是向下压准星，恢复就是向上抬回）
            }

            if (Mathf.Abs(recoverH) > 0.001f)
            {
                accumulatedRecoilH -= recoverH;
                yaw -= recoverH; // 水平方向同理
            }
        }

        /// <summary>
        /// 将原始后坐力值映射到自定义范围（用于平衡手感）
        /// </summary>
        private float MapRecoilValue(float originalValue)
        {
            if (originalValue <= 0f) return 0f;
            if (originalValue <= 10f) return originalValue * 0.2f;
            if (originalValue <= 20f) return 2f + (originalValue - 10f) * 0.2f;
            if (originalValue <= 30f)
            {
                float normalized = (originalValue - 20f) / 10f;
                return 4f + Mathf.Sqrt(normalized) * 2f;
            }
            if (originalValue <= 50f)
            {
                float normalized = (originalValue - 30f) / 20f;
                return 6f + Mathf.Pow(normalized, 0.6f) * 2f;
            }
            float excess = originalValue - 50f;
            float compressed = Mathf.Log(excess + 1f) / Mathf.Log(50f);
            return 8f + Mathf.Clamp01(compressed) * 1f;
        }

        /// <summary>
        /// 生成高斯分布随机数（用于水平后坐力变化）
        /// </summary>
        private float GaussianRandom(float mean, float stdDev)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;
            float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            return mean + stdDev * z0;
        }

        /// <summary>
        /// 根据武器 TypeID 判断武器类别（霰弹枪优先使用 ShotCount）
        /// </summary>
        private WeaponCategory GetWeaponCategory(ItemAgent_Gun gun)
        {
            // 霰弹枪通过 ShotCount 识别（保留原逻辑）
            if (gun.ShotCount > 1)
                return WeaponCategory.Shotgun;

            // 确保 gun 和 Item 不为空
            if (gun?.Item == null)
                return WeaponCategory.Unknown;

            int typeId = gun.Item.TypeID;

            // 按集合判断武器类型
            if (PistolSMGIds.Contains(typeId))
                return WeaponCategory.PistolSMG;
            if (RifleIds.Contains(typeId))
                return WeaponCategory.Rifle;
            if (SniperIds.Contains(typeId))
                return WeaponCategory.Sniper;
            if (MarksmanRifleIds.Contains(typeId))
                return WeaponCategory.MarksmanRifle;

            // 如果用了霰弹枪集合，可以在这里判断
            // if (ShotgunIds.Contains(typeId))
            //     return WeaponCategory.Shotgun;

            // 未知武器：记录日志并返回 Unknown，由后续逻辑处理
            FPLogger.Log($"[GetWeaponCategory] 未知武器 TypeID={typeId}, Name={gun.Item.DisplayName}，归类为 Unknown");
            return WeaponCategory.Unknown;
        }
        #endregion
    }
}