using UnityEngine;

namespace SpaceGame.Player
{
    /// <summary>
    /// Attach this component to any GameObject that the player should be able to pick up with 'F'.
    /// Ensures a Rigidbody is present with optimal physics settings for smooth carrying and dropping.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarryableObject : MonoBehaviour
    {
        [Header("Carry Physics Attributes")]
        [Tooltip("Custom hold distance offset when carried. 0 uses the player's default hold distance.")]
        [SerializeField] private float customHoldDistance = 0f;

        [Tooltip("Throw force multiplier applied when thrown by the player.")]
        [SerializeField] private float throwMultiplier = 1f;

        [Tooltip("If true, the object will orient upright relative to the world when carried.")]
        [SerializeField] private bool keepUprightWhenCarried = false;

        private Rigidbody rb;
        private bool isCarried = false;

        public Rigidbody Rigidbody => rb != null ? rb : (rb = GetComponent<Rigidbody>());
        public float CustomHoldDistance => customHoldDistance;
        public float ThrowMultiplier => throwMultiplier;
        public bool KeepUprightWhenCarried => keepUprightWhenCarried;
        public bool IsCarried => isCarried;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Ensure realistic, responsive physics interaction
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        public void OnPickup()
        {
            isCarried = true;
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearDamping = 8f;
                rb.angularDamping = 8f;
            }
        }

        public void OnDrop(Vector3 releaseVelocity)
        {
            isCarried = false;
            if (rb != null)
            {
                rb.useGravity = true;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;
                rb.linearVelocity = releaseVelocity;
            }
        }
    }
}
