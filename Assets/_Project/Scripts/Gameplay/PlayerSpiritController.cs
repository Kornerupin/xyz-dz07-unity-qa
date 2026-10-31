using EchoesOfAscent.Core;
using EchoesOfAscent.Input;
using EchoesOfAscent.Presentation;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GroundDetector))]
    public sealed class PlayerSpiritController : MonoBehaviour
    {
        [SerializeField] JumpConfig jumpConfig;
        [SerializeField] SwipeInputProvider swipeInput;
        [SerializeField] TrajectoryPreviewRenderer trajectoryPreview;
        [SerializeField] Camera gameplayCamera;

        readonly JumpCalculator jumpCalculator = new JumpCalculator();

        Rigidbody rb;
        Collider bodyCollider;
        GroundDetector groundDetector;
        float lastGroundedTime;
        bool isAirborne;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            groundDetector = GetComponent<GroundDetector>();

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

            if (swipeInput == null)
                swipeInput = GetComponent<SwipeInputProvider>();

            if (trajectoryPreview == null)
                trajectoryPreview = GetComponentInChildren<TrajectoryPreviewRenderer>();
        }

        void Start()
        {
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;
        }

        void OnEnable()
        {
            if (swipeInput == null)
                return;

            swipeInput.OnSwipeUpdated += HandleSwipeUpdated;
            swipeInput.OnSwipeReleased += HandleSwipeReleased;
            swipeInput.OnSwipeCancelled += HandleSwipeCancelled;
        }

        void OnDisable()
        {
            if (swipeInput == null)
                return;

            swipeInput.OnSwipeUpdated -= HandleSwipeUpdated;
            swipeInput.OnSwipeReleased -= HandleSwipeReleased;
            swipeInput.OnSwipeCancelled -= HandleSwipeCancelled;
        }

        void FixedUpdate()
        {
            if (groundDetector.IsGrounded())
            {
                lastGroundedTime = Time.time;
                isAirborne = false;
            }
            else if (rb.linearVelocity.sqrMagnitude > 0.05f)
            {
                isAirborne = true;
            }
        }

        void HandleSwipeUpdated(SwipeData swipe)
        {
            if (jumpConfig == null || trajectoryPreview == null || gameplayCamera == null)
                return;

            if (!CanJump())
            {
                trajectoryPreview.Hide();
                return;
            }

            Vector3 launchVelocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);
            if (launchVelocity.sqrMagnitude < 0.01f)
            {
                trajectoryPreview.Hide();
                return;
            }

            Vector3[] arc = jumpCalculator.SampleTrajectory(GetTrajectoryOrigin(), launchVelocity, jumpConfig);
            trajectoryPreview.Show(arc);
        }

        void HandleSwipeReleased(SwipeData swipe)
        {
            trajectoryPreview?.Hide();

            if (jumpConfig == null || !CanJump() || !swipe.IsValid(jumpConfig.minSwipePixels))
                return;

            Vector3 launchVelocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);
            if (launchVelocity.sqrMagnitude < 0.01f)
                return;

            var platformCarrier = GetComponent<PlayerMovingPlatformCarrier>();
            Vector3 platformVelocity = platformCarrier != null && platformCarrier.IsRidingMovingPlatform
                ? platformCarrier.RidingVelocity
                : Vector3.zero;

            rb.linearVelocity = new Vector3(platformVelocity.x, 0f, 0f);
            rb.AddForce(launchVelocity, ForceMode.VelocityChange);
            isAirborne = true;
        }

        void HandleSwipeCancelled()
        {
            trajectoryPreview?.Hide();
        }

        bool CanJump()
        {
            if (jumpConfig == null)
                return false;

            if (groundDetector.IsGrounded())
                return true;

            if (isAirborne)
                return false;

            return Time.time - lastGroundedTime <= jumpConfig.coyoteTime;
        }

        Vector3 GetTrajectoryOrigin()
        {
            var bounds = bodyCollider.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
    }
}
