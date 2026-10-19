using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpaceGame.Player
{
    /// <summary>
    /// Physics-based object carrying system:
    /// - Left Click (LMB): Pick up aimed object
    /// - Right Click (RMB): Put object down / place gently
    /// - 'F' Key: Throw object forward with physics velocity
    /// - Dynamic Crosshair indicator: Automatically expands/thickens when hovering over a carryable object or holding one
    /// - Bottom Screen HUD prompt: Contextual prompt displayed cleanly at the bottom of the screen
    /// </summary>
    [RequireComponent(typeof(FPSController))]
    public class ObjectCarryingSystem : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Max reach distance to grab an object")]
        [SerializeField] private float grabReachDistance = 3.5f;

        [Tooltip("Fixed holding distance in front of player")]
        [SerializeField] private float holdDistance = 2.2f;

        [Tooltip("LayerMask of objects that can be grabbed (leave Everything for any Rigidbody/CarryableObject)")]
        [SerializeField] private LayerMask interactableLayers = ~0;

        [Header("Physics Spring Settings")]
        [Tooltip("Strength of the spring pulling the object towards the hold point")]
        [SerializeField] private float springStrength = 180f;

        [Tooltip("Damping to prevent oscillations and jitter")]
        [SerializeField] private float springDamping = 22f;

        [Tooltip("Maximum force applied to the carried object (prevents pushing through walls)")]
        [SerializeField] private float maxForce = 500f;

        [Tooltip("Distance threshold at which the object drops if snagged behind walls or player moves too far")]
        [SerializeField] private float breakDistance = 4.8f;

        [Header("Throwing Settings")]
        [Tooltip("Forward velocity applied when pressing 'F' while carrying an object")]
        [SerializeField] private float throwForce = 12f;

        [Header("Crosshair Dynamic Indicator")]
        [Tooltip("Normal idle crosshair dot size")]
        [SerializeField] private float idleCrosshairSize = 4f;

        [Tooltip("Expanded crosshair size/thickness when looking at or carrying an object")]
        [SerializeField] private float activeCrosshairSize = 9f;

        [Tooltip("Smooth speed of crosshair size adjustment")]
        [SerializeField] private float crosshairTransitionSpeed = 14f;

        [Header("Bottom Prompt Settings")]
        [SerializeField] private bool showBottomPrompt = true;
        [SerializeField] private Color promptTextColor = new Color(0.96f, 0.94f, 0.88f, 0.95f);
        [SerializeField] private Color promptBgColor = new Color(0.04f, 0.05f, 0.08f, 0.82f);

        // Internal State
        private FPSController fpsController;
        private Transform playerCamera;
        private Rigidbody carriedRigidbody;
        private CarryableObject carriedCustomObject;
        private bool isCarrying = false;
        private bool isLookingAtCarryable = false;
        private string targetObjectName = "";
        private float currentCrosshairSize;

        // Saved original Rigidbody properties
        private bool originalUseGravity;
        private float originalDrag;
        private float originalAngularDrag;
        private RigidbodyInterpolation originalInterpolation;
        private CollisionDetectionMode originalCollisionMode;

        private Texture2D promptBgTex;

        public bool IsCarrying => isCarrying;
        public Rigidbody CarriedRigidbody => carriedRigidbody;

        private void Awake()
        {
            fpsController = GetComponent<FPSController>();
            currentCrosshairSize = idleCrosshairSize;
        }

        private void Start()
        {
            if (fpsController != null)
            {
                playerCamera = fpsController.CameraTransform;
                fpsController.CrosshairSize = idleCrosshairSize;
            }

            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (playerCamera == null)
            {
                if (fpsController != null && fpsController.CameraTransform != null)
                {
                    playerCamera = fpsController.CameraTransform;
                }
                else if (Camera.main != null)
                {
                    playerCamera = Camera.main.transform;
                }
                if (playerCamera == null) return;
            }

            // 1. Raycast detection when NOT carrying
            if (!isCarrying)
            {
                CheckForCarryableTarget();
            }

            // 2. Adjust dynamic crosshair thickness/size
            UpdateCrosshairIndicator();

            // 3. Left Mouse Button: Pick Up
            if (!isCarrying && CheckPickUpInput())
            {
                TryGrabTarget();
            }

            // 4. Right Mouse Button: Place Down Gently
            if (isCarrying && CheckPlaceInput())
            {
                PlaceCarriedObject();
            }

            // 5. 'F' Key: Throw Object Forward
            if (isCarrying && CheckThrowInput())
            {
                ThrowCarriedObject();
            }
        }

        private void UpdateCrosshairIndicator()
        {
            if (fpsController == null) return;

            float targetSize = (isCarrying || isLookingAtCarryable) ? activeCrosshairSize : idleCrosshairSize;
            currentCrosshairSize = Mathf.MoveTowards(currentCrosshairSize, targetSize, crosshairTransitionSpeed * Time.deltaTime);
            fpsController.CrosshairSize = currentCrosshairSize;
        }

        private void FixedUpdate()
        {
            if (!isCarrying || carriedRigidbody == null || playerCamera == null) return;

            // Target hover position directly in front of camera
            Vector3 targetPosition = playerCamera.position + playerCamera.forward * holdDistance;
            Vector3 positionDelta = targetPosition - carriedRigidbody.worldCenterOfMass;
            float distance = positionDelta.magnitude;

            // Break grip if snagged too far
            if (distance > breakDistance)
            {
                DropCarriedObject();
                return;
            }

            // Physics spring force: F = k * delta - c * v
            Vector3 springForce = positionDelta * springStrength - carriedRigidbody.linearVelocity * springDamping;
            if (springForce.sqrMagnitude > maxForce * maxForce)
            {
                springForce = springForce.normalized * maxForce;
            }

            carriedRigidbody.AddForce(springForce, ForceMode.Acceleration);

            // Maintain rotation stability relative to player look
            Quaternion targetRotation = playerCamera.rotation;
            if (carriedCustomObject != null && carriedCustomObject.KeepUprightWhenCarried)
            {
                targetRotation = Quaternion.Euler(0f, playerCamera.eulerAngles.y, 0f);
            }

            Quaternion rotationDelta = targetRotation * Quaternion.Inverse(carriedRigidbody.rotation);
            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            if (Mathf.Abs(angle) > 1.0f)
            {
                Vector3 angularVelocityTarget = axis * (angle * Mathf.Deg2Rad * 12f);
                carriedRigidbody.angularVelocity = Vector3.Lerp(carriedRigidbody.angularVelocity, angularVelocityTarget, Time.fixedDeltaTime * 15f);
            }
        }

        private void CheckForCarryableTarget()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);

            // 1. Precise raycast
            if (Physics.Raycast(ray, out RaycastHit hit, grabReachDistance, interactableLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    isLookingAtCarryable = true;
                    targetObjectName = CleanName(rb.gameObject.name);
                    return;
                }
            }

            // 2. Forgiving spherecast
            if (Physics.SphereCast(ray, 0.25f, out hit, grabReachDistance, interactableLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    isLookingAtCarryable = true;
                    targetObjectName = CleanName(rb.gameObject.name);
                    return;
                }
            }

            isLookingAtCarryable = false;
            targetObjectName = "";
        }

        private void TryGrabTarget()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, grabReachDistance, interactableLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    GrabObject(rb);
                    return;
                }
            }

            if (Physics.SphereCast(ray, 0.3f, out hit, grabReachDistance, interactableLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    GrabObject(rb);
                }
            }
        }

        private string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Object";
            return raw.Replace("Physics", "").Replace("_", " ").Trim();
        }

        public void GrabObject(Rigidbody rb)
        {
            if (rb == null) return;

            carriedRigidbody = rb;
            carriedCustomObject = rb.GetComponent<CarryableObject>();

            // Save original states
            originalUseGravity = carriedRigidbody.useGravity;
            originalDrag = carriedRigidbody.linearDamping;
            originalAngularDrag = carriedRigidbody.angularDamping;
            originalInterpolation = carriedRigidbody.interpolation;
            originalCollisionMode = carriedRigidbody.collisionDetectionMode;

            // Apply carry physics settings
            carriedRigidbody.useGravity = false;
            carriedRigidbody.linearDamping = 6f;
            carriedRigidbody.angularDamping = 6f;
            carriedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            carriedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (carriedCustomObject != null)
            {
                carriedCustomObject.OnPickup();
            }

            isCarrying = true;
        }

        public void PlaceCarriedObject()
        {
            if (!isCarrying || carriedRigidbody == null) return;
            Vector3 releaseVel = carriedRigidbody.linearVelocity * 0.2f;
            RestoreRigidbodyState(releaseVel);
        }

        public void DropCarriedObject()
        {
            if (!isCarrying || carriedRigidbody == null) return;
            RestoreRigidbodyState(Vector3.zero);
        }

        public void ThrowCarriedObject()
        {
            if (!isCarrying || carriedRigidbody == null || playerCamera == null) return;

            float force = throwForce;
            if (carriedCustomObject != null)
            {
                force *= carriedCustomObject.ThrowMultiplier;
            }

            Vector3 throwVelocity = playerCamera.forward * force;
            RestoreRigidbodyState(throwVelocity);
        }

        private void RestoreRigidbodyState(Vector3 velocityToApply)
        {
            if (carriedRigidbody != null)
            {
                carriedRigidbody.useGravity = originalUseGravity;
                carriedRigidbody.linearDamping = originalDrag;
                carriedRigidbody.angularDamping = originalAngularDrag;
                carriedRigidbody.interpolation = originalInterpolation;
                carriedRigidbody.collisionDetectionMode = originalCollisionMode;
                carriedRigidbody.linearVelocity = velocityToApply;

                if (carriedCustomObject != null)
                {
                    carriedCustomObject.OnDrop(velocityToApply);
                }
            }

            carriedRigidbody = null;
            carriedCustomObject = null;
            isCarrying = false;
        }

        private bool CheckPickUpInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool CheckPlaceInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        private bool CheckThrowInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }

        private void OnGUI()
        {
            if (!showBottomPrompt) return;

            float screenW = Screen.width;
            float screenH = Screen.height;

            if (promptBgTex == null)
            {
                promptBgTex = new Texture2D(1, 1);
                promptBgTex.SetPixel(0, 0, promptBgColor);
                promptBgTex.Apply();
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = promptTextColor;

            // Placed at the bottom of the screen (60px from bottom edge)
            if (isCarrying)
            {
                string text = "[F] Throw Object    •    [RMB] Put Down";
                float boxW = 380f;
                float boxH = 36f;
                Rect boxRect = new Rect(screenW * 0.5f - boxW * 0.5f, screenH - 75f, boxW, boxH);
                GUI.DrawTexture(boxRect, promptBgTex);
                GUI.Label(boxRect, text, style);
            }
            else if (isLookingAtCarryable)
            {
                string text = $"[LMB] Pick up {targetObjectName}";
                float boxW = 260f;
                float boxH = 34f;
                Rect boxRect = new Rect(screenW * 0.5f - boxW * 0.5f, screenH - 75f, boxW, boxH);
                GUI.DrawTexture(boxRect, promptBgTex);
                GUI.Label(boxRect, text, style);
            }
        }
    }
}
