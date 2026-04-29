using UnityEngine;
using UnityEngine.InputSystem;

namespace BigDreamLab.Player
{
    [DefaultExecutionOrder(-20)]
    public sealed class DesktopFirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraTransform;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 2.5f;
        [SerializeField] float sprintSpeed = 4f;
        [SerializeField] float acceleration = 18f;
        [SerializeField] float deceleration = 24f;
        [SerializeField] float gravity = -18f;
        [SerializeField] float groundedStickForce = -2f;

        [Header("Look")]
        [SerializeField] float mouseSensitivity = 0.08f;
        [SerializeField] float gamepadLookSensitivity = 120f;
        [SerializeField] float minPitch = -75f;
        [SerializeField] float maxPitch = 75f;
        [SerializeField] bool lockCursorOnStart = true;
        [SerializeField] bool disableCameraPoseDriver = true;

        CharacterController m_CharacterController;
        InputAction m_MoveAction;
        InputAction m_LookAction;
        InputAction m_InteractAction;
        InputAction m_SprintAction;
        InputAction m_UnlockCursorAction;
        float m_Yaw;
        float m_Pitch;
        float m_VerticalVelocity;
        float m_InteractHoldTime;
        Vector3 m_HorizontalVelocity;
        bool m_CursorLocked;
        bool m_InputBlocked;
        bool m_SuppressInteractThisFrame;

        public Transform CameraTransform => cameraTransform;
        public Vector2 MoveInput { get; private set; }
        public bool InputBlocked => m_InputBlocked;
        public bool InteractHeld => !m_InputBlocked && m_InteractAction != null && m_InteractAction.IsPressed();
        public bool InteractPressedThisFrame => !m_InputBlocked && m_InteractAction != null && m_InteractAction.WasPressedThisFrame() && !m_SuppressInteractThisFrame;
        public bool InteractReleasedThisFrame => !m_InputBlocked && m_InteractAction != null && m_InteractAction.WasReleasedThisFrame();
        public float InteractHoldTime => m_InteractHoldTime;

        void Awake()
        {
            ResolveReferences();
            ConfigureCharacterController();
            CacheLookAngles();

            if (disableCameraPoseDriver)
                DisablePoseDriverOnCamera();
        }

        void OnEnable()
        {
            EnsureInputActions();
            SetInputActionsEnabled(true);

            if (lockCursorOnStart)
                SetCursorLocked(true);
        }

        void OnDisable()
        {
            SetInputActionsEnabled(false);
            SetCursorLocked(false);
        }

        void OnDestroy()
        {
            m_MoveAction?.Dispose();
            m_LookAction?.Dispose();
            m_InteractAction?.Dispose();
            m_SprintAction?.Dispose();
            m_UnlockCursorAction?.Dispose();
        }

        void Update()
        {
            m_SuppressInteractThisFrame = false;
            ResolveReferences();

            if (m_InputBlocked)
            {
                StopPlayerInput();
                return;
            }

            HandleCursor();
            HandleLook();
            HandleInteractionTimer();
            HandleMovement(Time.deltaTime);
        }

        public void SetInputBlocked(bool blocked)
        {
            m_InputBlocked = blocked;

            if (blocked)
                StopPlayerInput();
        }

        void ResolveReferences()
        {
            if (cameraTransform != null)
                return;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
                return;
            }

            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;
        }

        void ConfigureCharacterController()
        {
            m_CharacterController = GetComponent<CharacterController>();
        }

        void CacheLookAngles()
        {
            m_Yaw = transform.eulerAngles.y;
            if (cameraTransform == null)
                return;

            m_Pitch = NormalizePitch(cameraTransform.localEulerAngles.x);
            m_Pitch = Mathf.Clamp(m_Pitch, minPitch, maxPitch);
        }

        void EnsureInputActions()
        {
            if (m_MoveAction != null)
                return;

            m_MoveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            m_MoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            m_MoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            m_MoveAction.AddBinding("<Gamepad>/leftStick");

            m_LookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
            m_LookAction.AddBinding("<Mouse>/delta");
            m_LookAction.AddBinding("<Gamepad>/rightStick");

            m_InteractAction = new InputAction("Interact", InputActionType.Button);
            m_InteractAction.AddBinding("<Mouse>/leftButton");

            m_SprintAction = new InputAction("Sprint", InputActionType.Button);
            m_SprintAction.AddBinding("<Keyboard>/leftShift");
            m_SprintAction.AddBinding("<Keyboard>/rightShift");
            m_SprintAction.AddBinding("<Gamepad>/leftStickPress");

            m_UnlockCursorAction = new InputAction("Unlock Cursor", InputActionType.Button);
            m_UnlockCursorAction.AddBinding("<Keyboard>/escape");
        }

        void SetInputActionsEnabled(bool enabled)
        {
            if (enabled)
            {
                m_MoveAction?.Enable();
                m_LookAction?.Enable();
                m_InteractAction?.Enable();
                m_SprintAction?.Enable();
                m_UnlockCursorAction?.Enable();
                return;
            }

            m_MoveAction?.Disable();
            m_LookAction?.Disable();
            m_InteractAction?.Disable();
            m_SprintAction?.Disable();
            m_UnlockCursorAction?.Disable();
        }

        void HandleCursor()
        {
            if (m_UnlockCursorAction.WasPressedThisFrame())
            {
                SetCursorLocked(false);
                return;
            }

            if (!m_CursorLocked && m_InteractAction.WasPressedThisFrame())
            {
                SetCursorLocked(true);
                m_SuppressInteractThisFrame = true;
            }
        }

        void HandleLook()
        {
            if (!m_CursorLocked || cameraTransform == null)
                return;

            var lookInput = m_LookAction.ReadValue<Vector2>();
            if (lookInput.sqrMagnitude <= Mathf.Epsilon)
                return;

            var activeDevice = m_LookAction.activeControl?.device;
            var isMouse = activeDevice is Mouse;
            var scale = isMouse ? mouseSensitivity : gamepadLookSensitivity * Time.unscaledDeltaTime;

            m_Yaw += lookInput.x * scale;
            m_Pitch = Mathf.Clamp(m_Pitch - lookInput.y * scale, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
        }

        void HandleMovement(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            MoveInput = Vector2.ClampMagnitude(m_MoveAction.ReadValue<Vector2>(), 1f);
            var forwardSource = cameraTransform != null ? cameraTransform : transform;
            var forward = Vector3.ProjectOnPlane(forwardSource.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(forwardSource.right, Vector3.up).normalized;
            var desiredMove = forward * MoveInput.y + right * MoveInput.x;

            var speed = m_SprintAction.IsPressed() ? sprintSpeed : walkSpeed;
            var targetHorizontalVelocity = desiredMove * speed;
            var velocityChangeRate = targetHorizontalVelocity.sqrMagnitude > m_HorizontalVelocity.sqrMagnitude
                ? acceleration
                : deceleration;

            m_HorizontalVelocity = Vector3.MoveTowards(
                m_HorizontalVelocity,
                targetHorizontalVelocity,
                velocityChangeRate * deltaTime);

            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                if (m_CharacterController.isGrounded && m_VerticalVelocity < 0f)
                    m_VerticalVelocity = groundedStickForce;

                m_VerticalVelocity += gravity * deltaTime;
                var velocity = m_HorizontalVelocity + Vector3.up * m_VerticalVelocity;
                m_CharacterController.Move(velocity * deltaTime);
                return;
            }

            if (m_HorizontalVelocity.sqrMagnitude <= Mathf.Epsilon)
                return;

            transform.position += m_HorizontalVelocity * deltaTime;
        }

        void HandleInteractionTimer()
        {
            if (InteractHeld)
                m_InteractHoldTime += Time.deltaTime;
            else
                m_InteractHoldTime = 0f;
        }

        void StopPlayerInput()
        {
            MoveInput = Vector2.zero;
            m_HorizontalVelocity = Vector3.zero;
            m_InteractHoldTime = 0f;
        }

        public void SetCursorLocked(bool locked)
        {
            m_CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void DisablePoseDriverOnCamera()
        {
            if (cameraTransform == null)
                return;

            var behaviours = cameraTransform.GetComponents<Behaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour == this)
                    continue;

                if (behaviour.GetType().Name.Contains("TrackedPoseDriver"))
                    behaviour.enabled = false;
            }
        }

        static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}
