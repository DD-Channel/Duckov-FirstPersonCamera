using UnityEngine;

namespace FirstPersonCamera
{
    /// <summary>
    /// 第一人称相机控制器 - 遮挡全局变量维护模块
    /// 负责在第一人称模式下维护游戏的遮挡相关着色器全局变量（OC_*），
    /// 确保着色器能够正确访问相机和角色的位置、方向信息
    /// 注意：此功能默认关闭，因为第一人称模式下通常不需要维护这些全局变量
    /// </summary>
    public partial class FirstPersonCameraController
    {
        #region 常量定义
        /// <summary>
        /// 着色器全局变量名：瞄准视图方向
        /// </summary>
        private const string ShaderGlobalAimViewDir = "OC_AimViewDir";
        
        /// <summary>
        /// 着色器全局变量名：角色视图方向
        /// </summary>
        private const string ShaderGlobalCharacterViewDir = "OC_CharacterViewDir";
        
        /// <summary>
        /// 着色器全局变量名：瞄准位置
        /// </summary>
        private const string ShaderGlobalAimPos = "OC_AimPos";
        
        /// <summary>
        /// 着色器全局变量名：角色位置
        /// </summary>
        private const string ShaderGlobalCharacterPos = "OC_CharacterPos";
        
        /// <summary>
        /// Vector4的W分量：用于方向向量（通常为0）
        /// </summary>
        private const float Vector4WForDirection = 0f;
        
        /// <summary>
        /// Vector4的W分量：用于位置向量（通常为1）
        /// </summary>
        private const float Vector4WForPosition = 1f;
        #endregion

        #region 私有字段
        /// <summary>
        /// 是否维护遮挡全局变量标志
        /// 默认关闭：第一人称模式下通常不需要维护游戏的遮挡全局变量
        /// 如果启用，会在第一人称模式下保持OC_*着色器全局变量与相机同步
        /// </summary>
        private bool maintainOcclusionGlobals = false;
        #endregion

        #region 遮挡全局变量维护方法
        /// <summary>
        /// 在需要时维护遮挡全局变量
        /// 如果启用了维护标志，则更新着色器全局变量以匹配相机位置和方向
        /// 这确保在游戏管理器被禁用时，着色器仍能正确访问相机和角色信息
        /// </summary>
        private void MaintainOcclusionGlobalsIfNeeded()
        {
            try
            {
                // 如果未启用维护标志，直接返回
                if (!maintainOcclusionGlobals) return;
                
                // 如果主相机不存在，无法更新
                if (mainCamera == null) return;

                // 获取相机位置和方向
                Vector3 cameraPosition = mainCamera.transform.position;
                Vector3 cameraForward = mainCamera.transform.forward;

                // 更新瞄准视图方向全局变量
                SetShaderGlobalVector(ShaderGlobalAimViewDir, cameraForward, Vector4WForDirection);

                // 更新角色视图方向全局变量（与瞄准方向相同）
                SetShaderGlobalVector(ShaderGlobalCharacterViewDir, cameraForward, Vector4WForDirection);

                // 更新瞄准位置全局变量
                SetShaderGlobalVector(ShaderGlobalAimPos, cameraPosition, Vector4WForPosition);

                // 获取角色位置（如果角色存在则使用角色位置，否则使用相机位置）
                Vector3 characterPosition = mainCharacter != null 
                    ? mainCharacter.transform.position 
                    : cameraPosition;

                // 更新角色位置全局变量
                SetShaderGlobalVector(ShaderGlobalCharacterPos, characterPosition, Vector4WForPosition);
            }
            catch
            {
                // 处理失败，静默处理
            }
            
        }

        /// <summary>
        /// 设置着色器全局向量
        /// </summary>
        /// <param name="globalName">全局变量名称</param>
        /// <param name="vector3">三维向量值</param>
        /// <param name="w">四维向量的W分量</param>
        private void SetShaderGlobalVector(string globalName, Vector3 vector3, float w)
        {
            try
            {
                var vector4 = new Vector4(vector3.x, vector3.y, vector3.z, w);
                Shader.SetGlobalVector(globalName, vector4);
            }
            catch
            {
                // 设置失败，静默处理
            }
        }
        #endregion
    }
}
