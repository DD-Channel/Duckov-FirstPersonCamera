using System.Collections;
using UnityEngine;

namespace FirstPersonCamera
{
    public partial class FirstPersonCameraController
    {
        [Header("工作台交互调试")]
        [Tooltip("三个交互点的视角位置（可在 Inspector 中赋值）")]
        [SerializeField] private Transform workbenchViewPoint1;
        [SerializeField] private Transform workbenchViewPoint2;
        [SerializeField] private Transform workbenchViewPoint3;

        // 当前交互状态
        private bool isWorkbenchInteracting = false;
        private Coroutine workbenchTransitionCoroutine;

        // 用于保存玩家原始偏移（如果你需要恢复精确位置）
        private Vector3 preWorkbenchLocalPos;
        private Quaternion preWorkbenchLocalRot;

        // ========== 公共方法 ==========
        /// <summary>
        /// 开始与指定视角点的工作台交互
        /// </summary>
        /// <param name="viewPoint">工作台的视角 Transform</param>
        public void StartWorkbenchInteraction(Transform viewPoint)
        {
            if (!isFirstPersonMode)
                return;

            if (isWorkbenchInteracting)
                EndWorkbenchInteraction();

            if (viewPoint == null)
                return;

            // 启动协程
            if (workbenchTransitionCoroutine != null)
                StopCoroutine(workbenchTransitionCoroutine);
            workbenchTransitionCoroutine = StartCoroutine(TransitionToWorkbench(viewPoint));
        }

        /// <summary>
        /// 结束工作台交互，返回第一人称
        /// </summary>
        public void EndWorkbenchInteraction()
        {
            if (!isWorkbenchInteracting)
                return;

            if (workbenchTransitionCoroutine != null)
                StopCoroutine(workbenchTransitionCoroutine);
            workbenchTransitionCoroutine = StartCoroutine(TransitionFromWorkbench());
        }

        // 协程：进入工作台视角
        private IEnumerator TransitionToWorkbench(Transform targetView)
        {
            isWorkbenchInteracting = true;

            // 记录开始时的相机状态
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            Vector3 targetPos = targetView.position;
            Quaternion targetRot = targetView.rotation;

            float duration = 1.0f; // 过渡时间 1 秒，可根据需要调整
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 使用平滑曲线
                t = t * t * (3f - 2f * t); // smoothstep

                mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            // 确保最终位置准确
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;

            workbenchTransitionCoroutine = null;
        }

        // 协程：返回第一人称
        private IEnumerator TransitionFromWorkbench()
        {
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;

            // 计算目标位置（复用现有的第一人称位置计算方法）
            Vector3 targetPos = GetFirstPersonTargetPosition();
            Quaternion targetRot = GetFirstPersonTargetRotation();

            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // smoothstep

                mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;

            isWorkbenchInteracting = false;
            workbenchTransitionCoroutine = null;
        }

        // 辅助：获取第一人称相机的目标位置（复用你已有的逻辑）
        private Vector3 GetFirstPersonTargetPosition()
        {
            if (mainCharacter == null) return mainCamera.transform.position;

            Vector3 basePos;
            if (headSocket != null)
            {
                basePos = headSocket.position + Vector3.up * cameraHeightOffset;
            }
            else
            {
                basePos = mainCharacter.transform.position + Vector3.up * (1.7f + cameraHeightOffset);
            }

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 forward = rot * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

            basePos += forward * cameraForwardOffset + right * cameraRightOffset;
            basePos += right * peekOffset * peekMaxOffset;

            return basePos;
        }

        // 辅助：获取第一人称相机的目标旋转
        private Quaternion GetFirstPersonTargetRotation()
        {
            return Quaternion.Euler(pitch, yaw, -peekOffset * peekMaxRotation);
        }
    }
}