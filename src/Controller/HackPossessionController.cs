using UnityEngine;
using UnityEngine.InputSystem;
using FirstPersonCamera.Utilities;
using Duckov;

namespace FirstPersonCamera
{
    public class HackPossessionController : MonoBehaviour
    {
        #region 单例
        private static HackPossessionController _instance;
        public static HackPossessionController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("HackPossessionController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<HackPossessionController>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            // 清理事件订阅
            if (CharacterMainControl.Main != null)
            {
                CharacterMainControl.Main.OnActionStartEvent -= OnActionStart;
                CharacterMainControl.Main.OnActionProgressFinishEvent -= OnActionFinish;
            }
        }
        #endregion

        #region 配置参数
        [Header("Camera Settings")]
        public float cameraDistance = 1.5f;
        public float cameraHeight = 2f;
        public float mouseSensitivity = 1.5f;
        public float moveSpeed = 5f;

        [Header("Angle Limits")]
        public float minPitch = -30f;
        public float maxPitch = 60f;
        #endregion

        #region 运行时状态
        private FirstPersonCameraController fpsController;
        private CharacterMainControl possessedEnemy;
        private bool isPossessing;
        private float yaw;
        private float pitch;

        private Transform originalCameraParent;
        private Vector3 originalCameraLocalPos;
        private Quaternion originalCameraLocalRot;

        private bool useNewInputSystem;

        // 轮询上次控制角色
        private CharacterMainControl lastControlledCharacter;
        #endregion

        #region 初始化
        private void Start()
        {
            // Debug.Log("[HackPossession] Start() 被调用");
            fpsController = FirstPersonCameraController.Instance;
            useNewInputSystem = Keyboard.current != null && Mouse.current != null;

            // 订阅动作事件（仍保留作为快速响应）
            if (CharacterMainControl.Main != null)
            {
                CharacterMainControl.Main.OnActionStartEvent += OnActionStart;
                CharacterMainControl.Main.OnActionProgressFinishEvent += OnActionFinish;
            }
        }

        private Camera GetCamera()
        {
            if (fpsController != null && fpsController.MainCamera != null)
                return fpsController.MainCamera;
            return Camera.main;
        }

        private bool IsBoomCar(CharacterMainControl character)
        {
            if (character == null) return false;
            return character.GetComponentInChildren<AISpecialAttachment_BoomCar>() != null;
        }

        private bool IsHorse(CharacterMainControl character)
        {
            if (character == null) return false;
            return character.GetComponentInChildren<AISpecialAttachment_Horse>() != null;
        }

        public CharacterMainControl GetPossessedEnemy()
        {
            return possessedEnemy;
        }
        #endregion

        #region 事件处理（备选）
        private void OnActionStart(CharacterActionBase action)
        {
            if (action is CA_ControlOtherCharacter controlAction)
            {
                var target = controlAction.targetCharacter;
                if (target != null && !IsBoomCar(target) && !IsHorse(target))
                {
                    // Debug.Log($"[HackPossession] 检测到控制动作启动，目标: {target.name}");
                    StartPossessing(target);
                }
            }
        }

        private void OnActionFinish(CharacterActionBase action)
        {
            if (action is CA_ControlOtherCharacter controlAction)
            {
                if (controlAction.targetCharacter == possessedEnemy)
                {
                    StopPossessing();
                }
            }
        }
        #endregion

        #region 轮询控制角色变化
        private void LateUpdate()
        {
            // 处理已控制的敌人
            if (isPossessing)
            {
                UpdatePossessed();
            }

            // 轮询控制角色变化
            if (LevelManager.Instance != null)
            {
                CharacterMainControl currentControlled = LevelManager.Instance.ControllingCharacter;
                if (currentControlled != lastControlledCharacter)
                {
                    lastControlledCharacter = currentControlled;
                    OnControllingCharacterPolled(currentControlled);
                }
            }
        }

        private void OnControllingCharacterPolled(CharacterMainControl newControlled)
        {
            // Debug.Log($"[HackPossession] 控制角色轮询变化: {newControlled?.name}, isMain={newControlled == CharacterMainControl.Main}");

            if (newControlled == null) return;

            bool isMain = newControlled == CharacterMainControl.Main;

            if (isPossessing)
            {
                // 如果当前在控制中，且新角色不是当前敌人，说明控制结束
                if (newControlled != possessedEnemy)
                {
                    StopPossessing();
                }
            }
            else
            {
                // 如果新角色不是主角，且不是车辆/马匹，则接管
                if (!isMain && !IsBoomCar(newControlled) && !IsHorse(newControlled))
                {
                    // Debug.Log($"[HackPossession] 轮询检测到可控制敌人: {newControlled.name}");
                    StartPossessing(newControlled);
                }
            }
        }
        #endregion

        #region 控制逻辑
        private void StartPossessing(CharacterMainControl enemy)
        {
            if (isPossessing) return;
            if (enemy == null) return;

            var cam = GetCamera();
            if (cam == null)
            {
                // Debug.LogError("[HackPossession] 无法获取相机");
                return;
            }

            // Debug.Log($"[HackPossession] 开始控制敌人: {enemy.name}");

            var ai = enemy.GetComponent<AICharacterController>();
            if (ai != null) ai.enabled = false;

            originalCameraParent = cam.transform.parent;
            originalCameraLocalPos = cam.transform.localPosition;
            originalCameraLocalRot = cam.transform.localRotation;

            if (fpsController != null)
                fpsController.EnableCameraUpdate = false;

            cam.transform.SetParent(null);

            yaw = enemy.transform.eulerAngles.y;
            pitch = 15f;

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;
            cam.transform.position = enemy.transform.position + dir * cameraDistance + Vector3.up * cameraHeight;
            cam.transform.LookAt(enemy.transform.position + Vector3.up * cameraHeight);

            possessedEnemy = enemy;
            isPossessing = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void StopPossessing()
        {
            if (!isPossessing) return;

            // Debug.Log("[HackPossession] 停止控制敌人");

            var cam = GetCamera();
            if (cam != null)
            {
                if (originalCameraParent != null)
                {
                    cam.transform.SetParent(originalCameraParent);
                    cam.transform.localPosition = originalCameraLocalPos;
                    cam.transform.localRotation = originalCameraLocalRot;
                }
            }

            if (possessedEnemy != null)
            {
                var ai = possessedEnemy.GetComponent<AICharacterController>();
                if (ai != null) ai.enabled = true;
            }

            if (fpsController != null)
                fpsController.EnableCameraUpdate = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            isPossessing = false;
            possessedEnemy = null;
        }
        #endregion

        #region 更新已控制敌人
        private void UpdatePossessed()
        {
            if (possessedEnemy == null)
            {
                StopPossessing();
                return;
            }

            var cam = GetCamera();
            if (cam == null) return;

            // 鼠标输入
            float mouseX = 0f, mouseY = 0f;
            try
            {
                if (useNewInputSystem)
                {
                    var mouse = Mouse.current;
                    if (mouse != null)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        mouseX = delta.x;
                        mouseY = delta.y;
                    }
                }
                else
                {
                    mouseX = Input.GetAxis("Mouse X");
                    mouseY = Input.GetAxis("Mouse Y");
                }
            }
            catch { }

            yaw += mouseX * mouseSensitivity * 0.1f;
            pitch -= mouseY * mouseSensitivity * 0.1f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Transform targetTransform = possessedEnemy.modelRoot != null ? possessedEnemy.modelRoot : possessedEnemy.transform;
            targetTransform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;
            Vector3 targetPos = possessedEnemy.transform.position + dir * cameraDistance + Vector3.up * cameraHeight;
            cam.transform.position = targetPos;
            cam.transform.LookAt(possessedEnemy.transform.position + Vector3.up * cameraHeight);

            // 瞄准点
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Vector3 aimPoint;
            if (Physics.Raycast(ray, out hit, 1000f))
                aimPoint = hit.point;
            else
                aimPoint = ray.GetPoint(1000f);
            possessedEnemy.SetAimPoint(aimPoint);

            // 移动
            Vector3 move = Vector3.zero;
            try
            {
                if (useNewInputSystem)
                {
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        if (keyboard.wKey.isPressed) move.z += 1f;
                        if (keyboard.sKey.isPressed) move.z -= 1f;
                        if (keyboard.aKey.isPressed) move.x -= 1f;
                        if (keyboard.dKey.isPressed) move.x += 1f;
                    }
                }
                else
                {
                    move.x = Input.GetAxis("Horizontal");
                    move.z = Input.GetAxis("Vertical");
                }
            }
            catch { }

            if (move.magnitude > 0.1f)
            {
                move = move.normalized;
                Vector3 forward = possessedEnemy.transform.forward;
                Vector3 right = possessedEnemy.transform.right;
                Vector3 moveDir = (forward * move.z + right * move.x).normalized;
                possessedEnemy.SetMoveInput(moveDir);
            }
            else
            {
                possessedEnemy.SetMoveInput(Vector3.zero);
            }

            // 攻击
            bool attackPressed = false;
            try
            {
                if (useNewInputSystem)
                {
                    var mouse = Mouse.current;
                    if (mouse != null) attackPressed = mouse.leftButton.wasPressedThisFrame;
                }
                else
                {
                    attackPressed = Input.GetMouseButtonDown(0);
                }
            }
            catch { }

            if (attackPressed)
            {
                if (!possessedEnemy.Attack())
                {
                    possessedEnemy.Trigger(true, true, false);
                }
            }
        }
        #endregion

    }
}