using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    /// <summary>
    /// Tracks the last grounded world position so fall echoes spawn near the leave point,
    /// not inside the kill zone.
    /// </summary>
    [RequireComponent(typeof(GroundDetector))]
    [DefaultExecutionOrder(40)]
    public sealed class FallEchoTracker : MonoBehaviour
    {
        GroundDetector groundDetector;
        Vector3 lastGroundedPosition;
        bool hasGroundedPosition;

        public Vector3 LastGroundedPosition => hasGroundedPosition ? lastGroundedPosition : transform.position;
        public bool HasGroundedPosition => hasGroundedPosition;

        void Awake()
        {
            groundDetector = GetComponent<GroundDetector>();
            lastGroundedPosition = transform.position;
        }

        void FixedUpdate()
        {
            if (!groundDetector.IsGrounded())
                return;

            lastGroundedPosition = transform.position;
            hasGroundedPosition = true;
        }
    }
}
