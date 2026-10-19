using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpaceGame.Player
{
    /// <summary>
    /// Backbone First-Person Controller:
    /// - WASD Movement + Sprint (Left Shift) + Jump (Space)
    /// - Mouse Look with pitch clamping & locked cursor
    /// - Escape to unlock cursor, Click to lock cursor
    /// - Automatic camera detection & eye-level mounting
    /// - Integrated HUD crosshair
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Standard walking speed in m/s")]
        [SerializeField] private float walkSpeed = 6.0f;

        [Tooltip("Sprinting speed in m/s (holding Left Shift)")]
        [SerializeField] private float sprintSpeed = 10.0f;

        [Tooltip("Jump height in meters on the moon (floaty high leap)")]
        [SerializeField] private float jumpHeight = 2.8f;

        [Tooltip("Gravity acceleration (m/s^2). Moon gravity is -1.62 m/s^2 for authentic lunar physics.")]
        [SerializeField] private float gravity = -1.62f;

        [Header("Look / Camera Settings")]
        [Tooltip("The camera transform to rotate vertically (pitch). If unassigned, Camera.main will be automatically found and mounted.")]
        [SerializeField] private Transform cameraTransform;

        [Tooltip("Mouse sensitivity multiplier for look rotation")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float mouseSensitivity = 0.15f;

        [Tooltip("Minimum look angle (looking down)")]
        [SerializeField] private float minPitch = -85.0f;

        [Tooltip("Maximum look angle (looking up)")]
        [SerializeField] private float maxPitch = 85.0f;

        [Tooltip("Invert vertical mouse look")]
        [SerializeField] private bool invertY = false;

        [Header("Cursor Settings")]
        [Tooltip("Lock and hide cursor when playing")]
        [SerializeField] private bool autoLockCursor = true;

        [Header("Crosshair Settings")]
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private Color crosshairColor = new Color(0.2f, 0.9f, 1f, 0.85f); // Sci-fi cyan
        [SerializeField] private float crosshairSize = 4.0f;

        // Internal State
        private CharacterController characterController;
        private float verticalVelocity = 0.0f;
        private float currentPitch = 0.0f;
        private bool isCursorLocked = false;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Transform CameraTransform => cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : transform;
        public float CrosshairSize { get => crosshairSize; set => crosshairSize = value; }
        public bool ShowCrosshair { get => showCrosshair; set => showCrosshair = value; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            // Setup CharacterController default sizing if needed
            if (characterController != null)
            {
                if (characterController.height < 1.0f)
                {
                    characterController.height = 2.0f;
                    characterController.center = new Vector3(0f, 1.0f, 0f);
                    characterController.radius = 0.45f;
                }
            }

            // Auto-detect and mount Main Camera if not already set
            SetupCamera();
        }

        private void Start()
        {
            if (autoLockCursor)
            {
                SetCursorLock(true);
            }
        }

        private void Update()
        {
            HandleCursorState();
            HandleLook();
            HandleMovement();
        }

        private void SetupCamera()
        {
            if (cameraTransform == null)
            {
                // Check if there is already a child camera
                Camera childCam = GetComponentInChildren<Camera>();
                if (childCam != null)
                {
                    cameraTransform = childCam.transform;
                }
                else
                {
                    // Find Main Camera in scene and mount it
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.transform.SetParent(transform);
                        mainCam.transform.localPosition = new Vector3(0f, 1.65f, 0f); // Standard eye level
                        mainCam.transform.localRotation = Quaternion.identity;
                        cameraTransform = mainCam.transform;
                    }
                    else
                    {
                        // Create a camera if none exists in scene
                        GameObject camObj = new GameObject("PlayerCamera");
                        camObj.transform.SetParent(transform);
                        camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                        camObj.transform.localRotation = Quaternion.identity;
                        Camera newCam = camObj.AddComponent<Camera>();
                        camObj.AddComponent<AudioListener>();
                        newCam.tag = "MainCamera";
                        cameraTransform = camObj.transform;
                    }
                }
            }

            if (cameraTransform != null)
            {
                currentPitch = cameraTransform.localEulerAngles.x;
                if (currentPitch > 180f) currentPitch -= 360f;
            }
        }

        private void HandleCursorState()
        {
            // Toggle cursor lock with Escape or clicking into the window
#if ENABLE_INPUT_SYSTEM
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool clickPressed = Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
#else
            bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
            bool clickPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif

            if (escapePressed)
            {
                SetCursorLock(false);
            }
            else if (clickPressed && !isCursorLocked)
            {
                SetCursorLock(true);
            }

            // Continuously re-enforce lock in Unity Editor so the cursor stays locked even if focus changed
            if (isCursorLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void HandleLook()
        {
            // Only rotate when player hasn't intentionally unlocked the cursor with Escape
            if (!isCursorLocked) return;

            Vector2 lookDelta = GetLookInput();

            float lookX = lookDelta.x * mouseSensitivity;
            float lookY = lookDelta.y * mouseSensitivity;

            if (invertY)
            {
                lookY = -lookY;
            }

            // Horizontal Look (Yaw) - Rotates the entire player body
            transform.Rotate(Vector3.up * lookX);

            // Vertical Look (Pitch) - Rotates only the camera
            // Moving mouse UP (lookY > 0) decreases X angle in Unity, which tilts the camera UP
            // Moving mouse DOWN (lookY < 0) increases X angle in Unity, which tilts the camera DOWN
            if (cameraTransform != null)
            {
                currentPitch = Mathf.Clamp(currentPitch - lookY, minPitch, maxPitch);
                cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
            }
        }

        private Vector3 currentVelocity;

        private void HandleMovement()
        {
            Vector2 moveInput = GetMoveInput();
            bool isSprint = GetSprintInput();
            bool isJump = GetJumpInput();

            // Grounding check & vertical velocity
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0.0f)
                {
                    // Gentle downward stick force suitable for lunar gravity
                    verticalVelocity = -0.6f;
                }

                if (isJump)
                {
                    // v = sqrt(2 * g * h)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                }
            }
            else
            {
                // Lunar gravity acceleration
                verticalVelocity += gravity * Time.deltaTime;
            }

            // Direction relative to player orientation
            Vector3 targetMoveDir = (transform.forward * moveInput.y + transform.right * moveInput.x);
            if (targetMoveDir.sqrMagnitude > 1.0f)
            {
                targetMoveDir.Normalize();
            }

            float currentSpeed = isSprint ? sprintSpeed : walkSpeed;
            Vector3 targetHorizontalVelocity = targetMoveDir * currentSpeed;

            // In low-gravity (airborne), conserve momentum and drift with subtle air-steering
            float accelerationRate = characterController.isGrounded ? 18f : 3.5f;
            currentVelocity = Vector3.Lerp(currentVelocity, targetHorizontalVelocity, accelerationRate * Time.deltaTime);

            Vector3 totalMove = currentVelocity + Vector3.up * verticalVelocity;
            characterController.Move(totalMove * Time.deltaTime);
        }

        private Vector2 GetMoveInput()
        {
            Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.y += 1.0f;
                if (Keyboard.current.sKey.isPressed) move.y -= 1.0f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1.0f;
                if (Keyboard.current.dKey.isPressed) move.x += 1.0f;
            }
#else
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
#endif
            return move;
        }

        private Vector2 GetLookInput()
        {
            Vector2 look = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                look = Mouse.current.delta.ReadValue();
            }
#else
            look.x = Input.GetAxis("Mouse X") * 10.0f;
            look.y = Input.GetAxis("Mouse Y") * 10.0f;
#endif
            return look;
        }

        private bool GetSprintInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        private bool GetJumpInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private void OnGUI()
        {
            if (showCrosshair && isCursorLocked)
            {
                float centerX = Screen.width * 0.5f;
                float centerY = Screen.height * 0.5f;
                float half = crosshairSize * 0.5f;

                Color oldColor = GUI.color;
                GUI.color = crosshairColor;
                GUI.DrawTexture(new Rect(centerX - half, centerY - half, crosshairSize, crosshairSize), Texture2D.whiteTexture);
                GUI.color = oldColor;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && autoLockCursor)
            {
                SetCursorLock(true);
            }
        }
    }
}
