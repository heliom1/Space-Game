using UnityEngine;
using UnityEngine.UI;

namespace SpaceGame.UI
{
    /// <summary>
    /// Attach this to CloudVignetteOverlay in the Hierarchy.
    /// Allows live tuning of the animated cloud mist, swirl, drift speed, and density
    /// with immediate visual feedback in the Scene and Game views.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class AnimatedCloudVignette : MonoBehaviour
    {
        [Header("Drift & Swirl Animation")]
        [Tooltip("Speed at which the mist drifts and billows inward from the edges")]
        [Range(0.01f, 0.5f)]
        public float driftSpeed = 0.06f;

        [Tooltip("Swirl / distortion strength of the cloud tendrils")]
        [Range(0.0f, 2.0f)]
        public float turbulence = 0.7f;

        [Tooltip("Gentle atmospheric breathing (expansion and contraction of clouds)")]
        [Range(0.0f, 0.5f)]
        public float breathingPulsation = 0.12f;

        [Header("Cloud Appearance")]
        [Tooltip("Thickness of the billowing dark clouds at the perimeter")]
        [Range(0.2f, 3.0f)]
        public float cloudDensity = 1.35f;

        [Tooltip("Scale / granularity of the cloud wisps")]
        [Range(1.0f, 8.0f)]
        public float cloudScale = 3.2f;

        [Tooltip("Distance from center where clouds begin to appear")]
        [Range(0.1f, 1.0f)]
        public float edgeStart = 0.38f;

        [Tooltip("Outer edge softness")]
        [Range(0.5f, 2.0f)]
        public float edgeEnd = 1.15f;

        private Material materialInstance;
        private Graphic graphic;

        private void OnEnable()
        {
            SetupMaterial();
        }

        private void Start()
        {
            SetupMaterial();
        }

        private void SetupMaterial()
        {
            graphic = GetComponent<Graphic>();
            if (graphic != null && graphic.material != null)
            {
                if (materialInstance == null || materialInstance.shader != graphic.material.shader)
                {
                    materialInstance = new Material(graphic.material);
                    graphic.material = materialInstance;
                }
            }
        }

        private void Update()
        {
            if (materialInstance == null)
            {
                SetupMaterial();
                if (materialInstance == null) return;
            }

            materialInstance.SetFloat("_DriftSpeed", driftSpeed);
            materialInstance.SetFloat("_CloudDensity", cloudDensity);
            materialInstance.SetFloat("_Scale", cloudScale);
            materialInstance.SetFloat("_Turbulence", turbulence);
            materialInstance.SetFloat("_Pulsation", breathingPulsation);
            materialInstance.SetFloat("_EdgeStart", edgeStart);
            materialInstance.SetFloat("_EdgeEnd", edgeEnd);
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (materialInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(materialInstance);
                }
                else
                {
                    DestroyImmediate(materialInstance);
                }
                materialInstance = null;
            }
        }
    }
}
