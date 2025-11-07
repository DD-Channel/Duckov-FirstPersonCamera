using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 遮挡物管理模块
    /// 负责在第一人称模式下隐藏玩家身体部位和装备，避免遮挡相机视野
    /// 支持根据选项灵活控制需要隐藏的部位，并在退出第一人称模式时恢复显示
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 选项键：隐藏头盔
        /// </summary>
        private const string OptionKeyHideHelmet = "FirstPersonCamera_HideHelmet";
        
        /// <summary>
        /// 选项键：隐藏面罩
        /// </summary>
        private const string OptionKeyHideFaceMask = "FirstPersonCamera_HideFaceMask";
        
        /// <summary>
        /// 选项键：隐藏护甲
        /// </summary>
        private const string OptionKeyHideArmor = "FirstPersonCamera_HideArmor";
        
        /// <summary>
        /// 选项键：隐藏面部
        /// </summary>
        private const string OptionKeyHideFace = "FirstPersonCamera_HideFace";
        
        /// <summary>
        /// 选项键：隐藏头发
        /// </summary>
        private const string OptionKeyHideHair = "FirstPersonCamera_HideHair";
        
        /// <summary>
        /// 选项键：隐藏背包
        /// </summary>
        private const string OptionKeyHideBackpack = "FirstPersonCamera_HideBackpack";
        
        /// <summary>
        /// 选项键：隐藏近战武器
        /// </summary>
        private const string OptionKeyHideMelee = "FirstPersonCamera_HideMelee";
        
        /// <summary>
        /// 选项键：隐藏左手
        /// </summary>
        private const string OptionKeyHideLeftHand = "FirstPersonCamera_HideLeftHand";
        
        /// <summary>
        /// 选项键：隐藏右手
        /// </summary>
        private const string OptionKeyHideRightHand = "FirstPersonCamera_HideRightHand";
        
        /// <summary>
        /// 选项键：隐藏耳机
        /// </summary>
        private const string OptionKeyHideHeadset = "FirstPersonCamera_HideHeadset";
        
        /// <summary>
        /// 选项键：隐藏面部眼睛
        /// </summary>
        private const string OptionKeyHideFaceEyes = "FirstPersonCamera_HideFaceEyes";
        
        /// <summary>
        /// 选项键：隐藏面部眉毛
        /// </summary>
        private const string OptionKeyHideFaceEyebrows = "FirstPersonCamera_HideFaceEyebrows";
        
        /// <summary>
        /// 选项键：隐藏面部嘴巴
        /// </summary>
        private const string OptionKeyHideFaceMouth = "FirstPersonCamera_HideFaceMouth";
        
        /// <summary>
        /// 选项键：隐藏面部尾巴
        /// </summary>
        private const string OptionKeyHideFaceTail = "FirstPersonCamera_HideFaceTail";
        
        /// <summary>
        /// 选项键：隐藏面部脚部
        /// </summary>
        private const string OptionKeyHideFaceFeet = "FirstPersonCamera_HideFaceFeet";
        
        /// <summary>
        /// 选项键：隐藏面部翅膀
        /// </summary>
        private const string OptionKeyHideFaceWings = "FirstPersonCamera_HideFaceWings";
        
        /// <summary>
        /// 选项键：强制显示手持武器（即使隐藏手部）
        /// </summary>
        private const string OptionKeyForceShowHeldWeapon = "FirstPersonCamera_ForceShowHeldWeapon";
        
        /// <summary>
        /// 反射属性名：PartInstance（用于访问面部部件的实例）
        /// </summary>
        private const string ReflectionPropertyNamePartInstance = "PartInstance";
        
        /// <summary>
        /// 反射字段名：renderers（用于访问部件实例的渲染器列表或数组）
        /// 注意：CustomFacePart.renderers是List<Renderer>类型，不是数组
        /// </summary>
        private const string ReflectionFieldNameRenderers = "renderers";
        #endregion

        #region 私有字段
        /// <summary>
        /// 已隐藏的渲染器列表（用于恢复显示）
        /// </summary>
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        #endregion

        #region 隐藏遮挡物方法
        /// <summary>
        /// 隐藏第一人称模式下的遮挡物
        /// 根据选项设置隐藏玩家的身体部位和装备，避免遮挡相机视野
        /// 先恢复之前隐藏的渲染器，然后重新扫描并隐藏（以处理新装备的情况）
        /// </summary>
        private void HideFirstPersonObstructions()
        {
            try
            {
                // 先恢复所有之前隐藏的渲染器，以便重新扫描并正确隐藏（包括新装备）
                RestoreHiddenRenderers();
                hiddenRenderers.Clear();
                
                if (characterModel == null) return;

                // 加载所有选项设置
                var hideSettings = LoadHideSettings();

                // 隐藏角色模型的主要部位
                HideCharacterModelParts(hideSettings);

                // 隐藏自定义面部部件（使用反射访问精细部位）
                var face = characterModel.CustomFace;
                if (face != null)
                {
                    HideCustomFaceParts(face, hideSettings);
                }

                // 强制显示手持武器（即使隐藏了手部）
                EnsureWeaponMeshesVisible();
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 加载所有隐藏选项设置
        /// </summary>
        /// <returns>包含所有隐藏选项的元组</returns>
        private (bool helmet, bool faceMask, bool armor, bool face, bool hair,
                 bool backpack, bool melee, bool leftHand, bool rightHand,
                 bool headset, bool faceEyes, bool faceEyebrows, bool faceMouth,
                 bool faceTail, bool faceFeet, bool faceWings) LoadHideSettings()
        {
            try
            {
                return (
                    helmet: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideHelmet, 1) == 1,
                    faceMask: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceMask, 1) == 1,
                    armor: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideArmor, 1) == 1,
                    face: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFace, 1) == 1,
                    hair: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideHair, 1) == 1,
                    backpack: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideBackpack, 0) == 1,
                    melee: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideMelee, 0) == 1,
                    leftHand: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideLeftHand, 0) == 1,
                    rightHand: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideRightHand, 0) == 1,
                    headset: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideHeadset, 0) == 1,
                    faceEyes: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceEyes, 0) == 1,
                    faceEyebrows: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceEyebrows, 0) == 1,
                    faceMouth: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceMouth, 0) == 1,
                    faceTail: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceTail, 0) == 1,
                    faceFeet: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceFeet, 0) == 1,
                    faceWings: FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyHideFaceWings, 0) == 1
                );
            }
            catch
            {
                // 加载失败，返回默认值（大部分默认隐藏）
                return (true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false);
            }
        }

        /// <summary>
        /// 隐藏角色模型的主要部位
        /// </summary>
        /// <param name="hideSettings">隐藏选项设置</param>
        private void HideCharacterModelParts((bool helmet, bool faceMask, bool armor, bool face, bool hair,
                                              bool backpack, bool melee, bool leftHand, bool rightHand,
                                              bool headset, bool faceEyes, bool faceEyebrows, bool faceMouth,
                                              bool faceTail, bool faceFeet, bool faceWings) hideSettings)
        {
            // 隐藏头盔
            if (hideSettings.helmet && characterModel.HelmatSocket != null)
            {
                HideRenderersInTransform(characterModel.HelmatSocket);
            }

            // 隐藏面罩
            if (hideSettings.faceMask && characterModel.FaceMaskSocket != null)
            {
                HideRenderersInTransform(characterModel.FaceMaskSocket);
            }

            // 隐藏护甲
            if (hideSettings.armor && characterModel.ArmorSocket != null)
            {
                HideRenderersInTransform(characterModel.ArmorSocket);
            }

            // 隐藏背包
            if (hideSettings.backpack && characterModel.BackpackSocket != null)
            {
                HideRenderersInTransform(characterModel.BackpackSocket);
            }

            // 隐藏近战武器
            if (hideSettings.melee && characterModel.MeleeWeaponSocket != null)
            {
                HideRenderersInTransform(characterModel.MeleeWeaponSocket);
            }

            // 隐藏左手
            if (hideSettings.leftHand && characterModel.LefthandSocket != null)
            {
                HideRenderersInTransform(characterModel.LefthandSocket);
            }

            // 隐藏右手
            if (hideSettings.rightHand && characterModel.RightHandSocket != null)
            {
                HideRenderersInTransform(characterModel.RightHandSocket);
            }

            // 隐藏耳机（通常附加在头盔插槽上）
            if ((hideSettings.headset || hideSettings.helmet) && characterModel.HelmatSocket != null)
            {
                HideRenderersInTransform(characterModel.HelmatSocket);
            }

            // 隐藏身体部位：使用反射访问CharacterModel.renderers字段
            // 这是角色模型的主要身体部位渲染器列表
            HideCharacterModelBodyParts(hideSettings);
        }

        /// <summary>
        /// 隐藏角色模型的身体部位渲染器
        /// 通过反射访问CharacterModel.renderers字段来隐藏身体部位
        /// </summary>
        /// <param name="hideSettings">隐藏选项设置</param>
        private void HideCharacterModelBodyParts((bool helmet, bool faceMask, bool armor, bool face, bool hair,
                                                   bool backpack, bool melee, bool leftHand, bool rightHand,
                                                   bool headset, bool faceEyes, bool faceEyebrows, bool faceMouth,
                                                   bool faceTail, bool faceFeet, bool faceWings) hideSettings)
        {
            if (characterModel == null) return;

            try
            {
                // 使用反射访问CharacterModel的私有renderers字段
                var renderersField = characterModel.GetType().GetField("renderers", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (renderersField == null) return;

                var renderersValue = renderersField.GetValue(characterModel);
                if (renderersValue == null) return;

                // CharacterModel.renderers是List<Renderer>类型
                if (renderersValue is System.Collections.IList renderersList)
                {
                    // 收集需要排除的武器Socket下的渲染器（用于保留武器显示）
                    var weaponRenderers = new HashSet<Renderer>();
                    
                    // 收集右手Socket下的MeshRenderer（武器）
                    if (characterModel.RightHandSocket != null)
                    {
                        var rightHandMeshRenderers = characterModel.RightHandSocket.GetComponentsInChildren<MeshRenderer>(true);
                        foreach (var mr in rightHandMeshRenderers)
                        {
                            weaponRenderers.Add(mr);
                        }
                    }
                    
                    // 收集左手Socket下的MeshRenderer（武器）
                    if (characterModel.LefthandSocket != null)
                    {
                        var leftHandMeshRenderers = characterModel.LefthandSocket.GetComponentsInChildren<MeshRenderer>(true);
                        foreach (var mr in leftHandMeshRenderers)
                        {
                            weaponRenderers.Add(mr);
                        }
                    }

                    // 根据设置隐藏身体部位渲染器
                    foreach (var item in renderersList)
                    {
                        if (item is Renderer renderer && renderer != null && renderer.enabled)
                        {
                            // 跳过武器渲染器（保留武器显示）
                            if (weaponRenderers.Contains(renderer))
                            {
                                continue;
                            }

                            // 跳过LineRenderer（激光），不要隐藏激光
                            if (renderer is LineRenderer)
                            {
                                continue;
                            }

                            // 检查是否需要隐藏（根据设置）
                            bool shouldHide = false;

                            // 如果设置了隐藏手部，检查是否是手部渲染器
                            if ((hideSettings.leftHand || hideSettings.rightHand))
                            {
                                // 检查渲染器是否在手部Socket下
                                if (characterModel.LefthandSocket != null && 
                                    renderer.transform.IsChildOf(characterModel.LefthandSocket))
                                {
                                    // 但如果是MeshRenderer（武器），不隐藏
                                    // 武器渲染器已在前面排除，直接隐藏
                                    shouldHide = true;
                                }
                                else if (characterModel.RightHandSocket != null && 
                                         renderer.transform.IsChildOf(characterModel.RightHandSocket))
                                {
                                    // 但如果是MeshRenderer（武器），不隐藏
                                    // 武器渲染器已在前面排除，直接隐藏
                                    shouldHide = true;
                                }
                            }

                            // 如果设置了隐藏脸部，检查是否是脸部渲染器
                            if (hideSettings.face && !shouldHide)
                            {
                                // 脸部渲染器通常在CustomFace中处理，但这里作为后备
                                // 检查是否在头部相关位置
                                if (characterModel.HelmatSocket != null && 
                                    renderer.transform.IsChildOf(characterModel.HelmatSocket))
                                {
                                    // 但如果是装备（MeshRenderer），不隐藏（装备已在上面处理）
                                    // 装备已在上面处理过（并且武器已排除），这里直接隐藏
                                    shouldHide = true;
                                }
                            }

                            // 如果任何身体部位需要隐藏
                            // 激进策略仅在隐藏手部时生效：如果设置了隐藏手部，就隐藏所有非装备的身体渲染器
                            if (!shouldHide && (hideSettings.leftHand || hideSettings.rightHand))
                            {
                                // 检查渲染器是否在任何Socket下（如果在Socket下，说明是装备，已经处理过）
                                bool isInSocket = false;
                                if (characterModel.HelmatSocket != null && renderer.transform.IsChildOf(characterModel.HelmatSocket)) isInSocket = true;
                                if (characterModel.FaceMaskSocket != null && renderer.transform.IsChildOf(characterModel.FaceMaskSocket)) isInSocket = true;
                                if (characterModel.ArmorSocket != null && renderer.transform.IsChildOf(characterModel.ArmorSocket)) isInSocket = true;
                                if (characterModel.BackpackSocket != null && renderer.transform.IsChildOf(characterModel.BackpackSocket)) isInSocket = true;
                                if (characterModel.MeleeWeaponSocket != null && renderer.transform.IsChildOf(characterModel.MeleeWeaponSocket)) isInSocket = true;
                                
                                // 如果不在Socket下，则隐藏（这是身体本体，武器已在上面排除）
                                if (!isInSocket && !(renderer is MeshRenderer))
                                {
                                    shouldHide = true;
                                }
                            }

                            // 额外处理：有些身体使用MeshRenderer而非SkinnedMeshRenderer，
                            // 如果设置了隐藏手，且该渲染器不在任何装备Socket下，也应隐藏。
                            if (!shouldHide && (hideSettings.leftHand || hideSettings.rightHand))
                            {
                                bool isInSocket2 = false;
                                if (characterModel.HelmatSocket != null && renderer.transform.IsChildOf(characterModel.HelmatSocket)) isInSocket2 = true;
                                if (characterModel.FaceMaskSocket != null && renderer.transform.IsChildOf(characterModel.FaceMaskSocket)) isInSocket2 = true;
                                if (characterModel.ArmorSocket != null && renderer.transform.IsChildOf(characterModel.ArmorSocket)) isInSocket2 = true;
                                if (characterModel.BackpackSocket != null && renderer.transform.IsChildOf(characterModel.BackpackSocket)) isInSocket2 = true;
                                if (characterModel.MeleeWeaponSocket != null && renderer.transform.IsChildOf(characterModel.MeleeWeaponSocket)) isInSocket2 = true;
                                if (!isInSocket2)
                                {
                                    shouldHide = true;
                                }
                            }

                            if (shouldHide)
                            {
                                renderer.enabled = false;
                                hiddenRenderers.Add(renderer);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 反射访问失败，使用后备方案：隐藏整个角色模型（除了武器Socket）
                UnityEngine.Debug.LogWarning($"[FirstPersonCamera] HideCharacterModelBodyParts失败，使用后备方案: {ex.Message}");
                try
                {
                    // 后备方案：隐藏角色模型根Transform下的所有渲染器，但排除武器Socket
                    HideRenderersInCharacterModelRoot(hideSettings);
                }
                catch
                {
                    // 后备方案也失败，静默处理
                }
            }
        }

        /// <summary>
        /// 后备方案：隐藏角色模型根Transform下的所有渲染器（除了武器）
        /// </summary>
        /// <param name="hideSettings">隐藏选项设置</param>
        private void HideRenderersInCharacterModelRoot((bool helmet, bool faceMask, bool armor, bool face, bool hair,
                                                         bool backpack, bool melee, bool leftHand, bool rightHand,
                                                         bool headset, bool faceEyes, bool faceEyebrows, bool faceMouth,
                                                         bool faceTail, bool faceFeet, bool faceWings) hideSettings)
        {
            if (characterModel == null || characterModel.transform == null) return;

            // 收集武器Socket下的所有渲染器（用于排除）
            var weaponRenderers = new HashSet<Renderer>();
            
            if (characterModel.RightHandSocket != null)
            {
                var renderers = characterModel.RightHandSocket.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r is MeshRenderer) // 只排除MeshRenderer（武器）
                    {
                        weaponRenderers.Add(r);
                    }
                }
            }
            
            if (characterModel.LefthandSocket != null)
            {
                var renderers = characterModel.LefthandSocket.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r is MeshRenderer) // 只排除MeshRenderer（武器）
                    {
                        weaponRenderers.Add(r);
                    }
                }
            }

            // 获取角色模型根Transform下的所有渲染器
            var allRenderers = characterModel.transform.GetComponentsInChildren<Renderer>(true);
            
            foreach (var renderer in allRenderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                
                // 跳过武器渲染器
                if (weaponRenderers.Contains(renderer)) continue;
                
                // 跳过LineRenderer（激光），不要隐藏激光
                if (renderer is LineRenderer) continue;
                
                // 跳过已经在Socket下的渲染器（装备已在上面处理）
                bool isInSocket = false;
                if (characterModel.HelmatSocket != null && renderer.transform.IsChildOf(characterModel.HelmatSocket)) isInSocket = true;
                if (characterModel.FaceMaskSocket != null && renderer.transform.IsChildOf(characterModel.FaceMaskSocket)) isInSocket = true;
                if (characterModel.ArmorSocket != null && renderer.transform.IsChildOf(characterModel.ArmorSocket)) isInSocket = true;
                if (characterModel.BackpackSocket != null && renderer.transform.IsChildOf(characterModel.BackpackSocket)) isInSocket = true;
                if (characterModel.MeleeWeaponSocket != null && renderer.transform.IsChildOf(characterModel.MeleeWeaponSocket)) isInSocket = true;
                if (characterModel.RightHandSocket != null && renderer.transform.IsChildOf(characterModel.RightHandSocket))
                {
                    // 右手Socket下的MeshRenderer是武器，不隐藏
                    if (renderer is MeshRenderer) continue;
                    isInSocket = true;
                }
                if (characterModel.LefthandSocket != null && renderer.transform.IsChildOf(characterModel.LefthandSocket))
                {
                    // 左手Socket下的MeshRenderer是武器，不隐藏
                    if (renderer is MeshRenderer) continue;
                    isInSocket = true;
                }
                
                // 如果不在Socket下，且设置了隐藏身体部位，则隐藏
                if (!isInSocket && (hideSettings.leftHand || hideSettings.rightHand || hideSettings.face))
                {
                    // 只隐藏SkinnedMeshRenderer（身体部位），不隐藏MeshRenderer（可能是其他物体）
                    if (renderer is SkinnedMeshRenderer)
                    {
                        renderer.enabled = false;
                        hiddenRenderers.Add(renderer);
                    }
                }
            }
        }

        /// <summary>
        /// 隐藏自定义面部部件
        /// </summary>
        /// <param name="face">自定义面部对象</param>
        /// <param name="hideSettings">隐藏选项设置</param>
        private void HideCustomFaceParts(object face, (bool helmet, bool faceMask, bool armor, bool face, bool hair,
                                                       bool backpack, bool melee, bool leftHand, bool rightHand,
                                                       bool headset, bool faceEyes, bool faceEyebrows, bool faceMouth,
                                                       bool faceTail, bool faceFeet, bool faceWings) hideSettings)
        {
            // 使用反射访问面部对象的属性
            var faceType = face.GetType();

            // 隐藏主面部渲染器
            if (hideSettings.face)
            {
                var mainRenderersProperty = faceType.GetProperty("mainRenderers");
                var mainRenderers = mainRenderersProperty?.GetValue(face) as Renderer[];
                if (mainRenderers != null)
                {
                    foreach (var renderer in mainRenderers)
                    {
                        if (renderer != null && renderer.enabled)
                        {
                            renderer.enabled = false;
                            hiddenRenderers.Add(renderer);
                        }
                    }
                }
            }

            // 隐藏头发
            if (hideSettings.hair)
            {
                var hairSocketProperty = faceType.GetProperty("hairSocket");
                var hairSocket = hairSocketProperty?.GetValue(face) as Transform;
                if (hairSocket != null)
                {
                    HideRenderersInTransform(hairSocket);
                }
            }

            // 隐藏面部头盔
            if (hideSettings.helmet)
            {
                var helmatSocketProperty = faceType.GetProperty("helmatSocket");
                var helmatSocket = helmatSocketProperty?.GetValue(face) as Transform;
                if (helmatSocket != null)
                {
                    HideRenderersInTransform(helmatSocket);
                }
            }

            // 隐藏面罩
            if (hideSettings.faceMask)
            {
                var faceMaskSocketProperty = faceType.GetProperty("faceMaskSocket");
                var faceMaskSocket = faceMaskSocketProperty?.GetValue(face) as Transform;
                if (faceMaskSocket != null)
                {
                    HideRenderersInTransform(faceMaskSocket);
                }
            }

            // 隐藏精细部位（使用反射访问PartInstance）
            if (hideSettings.faceEyes)
            {
                var eyePartProperty = faceType.GetProperty("eyePart");
                var eyePart = eyePartProperty?.GetValue(face);
                HideFacePartViaReflection(eyePart);
            }

            if (hideSettings.faceEyebrows)
            {
                var eyebrowPartProperty = faceType.GetProperty("eyebrowPart");
                var eyebrowPart = eyebrowPartProperty?.GetValue(face);
                HideFacePartViaReflection(eyebrowPart);
            }

            if (hideSettings.faceMouth)
            {
                var mouthPartProperty = faceType.GetProperty("mouthPart");
                var mouthPart = mouthPartProperty?.GetValue(face);
                HideFacePartViaReflection(mouthPart);
                
                // 同时隐藏嘴巴插槽
                if (mouthPart != null)
                {
                    var socketProperty = mouthPart.GetType().GetProperty("socket");
                    var socket = socketProperty?.GetValue(mouthPart) as Transform;
                    if (socket != null)
                    {
                        HideRenderersInTransform(socket);
                    }
                }
            }

            if (hideSettings.faceTail)
            {
                var tailPartProperty = faceType.GetProperty("tailPart");
                var tailPart = tailPartProperty?.GetValue(face);
                HideFacePartViaReflection(tailPart);
            }

            if (hideSettings.faceFeet)
            {
                var footLPartProperty = faceType.GetProperty("footLPart");
                var footLPart = footLPartProperty?.GetValue(face);
                HideFacePartViaReflection(footLPart);

                var footRPartProperty = faceType.GetProperty("footRPart");
                var footRPart = footRPartProperty?.GetValue(face);
                HideFacePartViaReflection(footRPart);
            }

            if (hideSettings.faceWings)
            {
                var wingLPartProperty = faceType.GetProperty("wingLPart");
                var wingLPart = wingLPartProperty?.GetValue(face);
                HideFacePartViaReflection(wingLPart);

                var wingRPartProperty = faceType.GetProperty("wingRPart");
                var wingRPart = wingRPartProperty?.GetValue(face);
                HideFacePartViaReflection(wingRPart);
            }
        }

        /// <summary>
        /// 通过反射隐藏面部部件
        /// 使用反射访问PartInstance的renderers字段或组件
        /// </summary>
        /// <param name="partUtil">面部部件工具对象</param>
        private void HideFacePartViaReflection(object partUtil)
        {
            if (partUtil == null) return;

            try
            {
                // 获取PartInstance属性
                var partInstanceProperty = partUtil.GetType().GetProperty(ReflectionPropertyNamePartInstance);
                var instance = partInstanceProperty?.GetValue(partUtil, null);
                if (instance == null) return;

                // 尝试获取renderers字段（可能是List<Renderer>或Renderer[]）
                var renderersField = instance.GetType().GetField(ReflectionFieldNameRenderers);
                if (renderersField == null) 
                {
                    // 如果字段不存在，使用后备方案
                    var component = instance as Component;
                    if (component != null)
                    {
                        HideRenderersInTransform(component.transform);
                    }
                    return;
                }

                var renderersValue = renderersField.GetValue(instance);
                if (renderersValue == null) 
                {
                    // 如果renderers为null，使用后备方案
                    var component = instance as Component;
                    if (component != null)
                    {
                        HideRenderersInTransform(component.transform);
                    }
                    return;
                }

                // 尝试作为List<Renderer>处理
                if (renderersValue is System.Collections.IList renderersList)
                {
                    foreach (var item in renderersList)
                    {
                        if (item is Renderer renderer && renderer != null && renderer.enabled)
                        {
                            renderer.enabled = false;
                            hiddenRenderers.Add(renderer);
                        }
                    }
                }
                // 尝试作为Renderer[]数组处理（向后兼容）
                else if (renderersValue is Renderer[] renderersArray)
                {
                    foreach (var renderer in renderersArray)
                    {
                        if (renderer != null && renderer.enabled)
                        {
                            renderer.enabled = false;
                            hiddenRenderers.Add(renderer);
                        }
                    }
                }
                else
                {
                    // 后备方案：禁用部件实例下的所有渲染器
                    var component = instance as Component;
                    if (component != null)
                    {
                        HideRenderersInTransform(component.transform);
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 反射访问失败，使用后备方案
                UnityEngine.Debug.LogWarning($"[FirstPersonCamera] HideFacePartViaReflection失败，使用后备方案: {ex.Message}");
                try
                {
                    var partInstanceProperty = partUtil.GetType().GetProperty(ReflectionPropertyNamePartInstance);
                    var instance = partInstanceProperty?.GetValue(partUtil, null);
                    var component = instance as Component;
                    if (component != null)
                    {
                        HideRenderersInTransform(component.transform);
                    }
                }
                catch
                {
                    // 后备方案也失败，静默处理
                }
            }
        }

        /// <summary>
        /// 在指定Transform下隐藏所有渲染器
        /// </summary>
        /// <param name="parent">父Transform</param>
        private void HideRenderersInTransform(Transform parent)
        {
            if (parent == null) return;

            var renderers = parent.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    // 跳过LineRenderer（激光），不要隐藏激光
                    if (renderer is LineRenderer)
                    {
                        continue;
                    }
                    
                    renderer.enabled = false;
                    hiddenRenderers.Add(renderer);
                }
            }
        }

        /// <summary>
        /// 确保手持武器网格可见（即使隐藏了手部）
        /// 强制显示非蒙皮网格渲染器，用于显示武器模型
        /// </summary>
        private void EnsureWeaponMeshesVisible()
        {
            try
            {
                bool forceShowWeapon = FirstPersonCamera.Utilities.OptionsHelper.LoadInt(OptionKeyForceShowHeldWeapon, 1) == 1;
                if (!forceShowWeapon || characterModel == null) return;

                // 启用右手插槽中的非蒙皮网格渲染器
                if (characterModel.RightHandSocket != null)
                {
                    EnableNonSkinnedMeshRenderers(characterModel.RightHandSocket);
                }

                // 启用左手插槽中的非蒙皮网格渲染器
                if (characterModel.LefthandSocket != null)
                {
                    EnableNonSkinnedMeshRenderers(characterModel.LefthandSocket);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 启用指定Transform下的所有非蒙皮网格渲染器
        /// 注意：这些渲染器不会被添加到hiddenRenderers列表，因为它们需要持续显示
        /// </summary>
        /// <param name="parent">父Transform</param>
        private void EnableNonSkinnedMeshRenderers(Transform parent)
        {
            if (parent == null) return;

            var meshRenderers = parent.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var meshRenderer in meshRenderers)
            {
                if (meshRenderer != null && !meshRenderer.enabled)
                {
                    meshRenderer.enabled = true;
                    // 注意：不添加到hiddenRenderers列表，因为我们希望它持续显示
                }
            }
        }

        /// <summary>
        /// 恢复所有之前隐藏的渲染器
        /// </summary>
        private void RestoreHiddenRenderers()
        {
            foreach (var renderer in hiddenRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }
        #endregion

        #region 恢复遮挡物方法
        /// <summary>
        /// 恢复第一人称模式下隐藏的遮挡物
        /// 恢复所有被隐藏的渲染器，并强制显示装备插槽以处理会话中更换装备的情况
        /// </summary>
        private void RestoreFirstPersonObstructions()
        {
            try
            {
                // 恢复所有隐藏的渲染器
                RestoreHiddenRenderers();
            }
            finally
            {
                // 清空列表
                hiddenRenderers.Clear();
            }

            // 额外强制显示装备插槽，以处理会话中更换装备的情况
            ForceShowEquipmentRenderers();
        }

        /// <summary>
        /// 强制显示所有装备插槽的渲染器
        /// 用于处理在会话中更换装备的情况，确保所有装备都能正确显示
        /// </summary>
        private void ForceShowEquipmentRenderers()
        {
            try
            {
                if (characterModel == null) return;

                // 强制显示所有主要装备插槽
                ForceShowRenderersInTransform(characterModel.HelmatSocket);
                ForceShowRenderersInTransform(characterModel.FaceMaskSocket);
                ForceShowRenderersInTransform(characterModel.ArmorSocket);
                ForceShowRenderersInTransform(characterModel.BackpackSocket);
                ForceShowRenderersInTransform(characterModel.MeleeWeaponSocket);
                ForceShowRenderersInTransform(characterModel.LefthandSocket);
                ForceShowRenderersInTransform(characterModel.RightHandSocket);

                // 处理自定义面部
                var face = characterModel.CustomFace;
                if (face != null)
                {
                    ForceShowCustomFaceRenderers(face);
                }
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 强制显示自定义面部的所有渲染器
        /// </summary>
        /// <param name="face">自定义面部对象</param>
        private void ForceShowCustomFaceRenderers(object face)
        {
            try
            {
                var faceType = face.GetType();

                // 强制显示主渲染器
                var mainRenderersProperty = faceType.GetProperty("mainRenderers");
                var mainRenderers = mainRenderersProperty?.GetValue(face) as Renderer[];
                if (mainRenderers != null)
                {
                    foreach (var renderer in mainRenderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = true;
                        }
                    }
                }

                // 强制显示所有插槽
                ForceShowRenderersInTransform(faceType.GetProperty("hairSocket")?.GetValue(face) as Transform);
                ForceShowRenderersInTransform(faceType.GetProperty("helmatSocket")?.GetValue(face) as Transform);
                ForceShowRenderersInTransform(faceType.GetProperty("faceMaskSocket")?.GetValue(face) as Transform);

                // 强制显示所有部件实例（使用反射）
                ForceShowFacePartViaReflection(faceType.GetProperty("eyePart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("eyebrowPart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("mouthPart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("tailPart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("footLPart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("footRPart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("wingLPart")?.GetValue(face));
                ForceShowFacePartViaReflection(faceType.GetProperty("wingRPart")?.GetValue(face));
            }
            catch
            {
                // 处理失败，静默处理
            }
        }

        /// <summary>
        /// 通过反射强制显示面部部件
        /// </summary>
        /// <param name="partUtil">面部部件工具对象</param>
        private void ForceShowFacePartViaReflection(object partUtil)
        {
            if (partUtil == null) return;

            try
            {
                var partInstanceProperty = partUtil.GetType().GetProperty(ReflectionPropertyNamePartInstance);
                var instance = partInstanceProperty?.GetValue(partUtil, null);
                var component = instance as Component;
                
                if (component != null)
                {
                    ForceShowRenderersInTransform(component.transform);
                }
            }
            catch
            {
                // 反射访问失败，静默处理
            }
        }

        /// <summary>
        /// 强制显示指定Transform下的所有渲染器
        /// </summary>
        /// <param name="parent">父Transform</param>
        private void ForceShowRenderersInTransform(Transform parent)
        {
            if (parent == null) return;

            var renderers = parent.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }
        #endregion
    }
}
