using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-10)]
    public sealed class CircularArcMover : MonoBehaviour, IMovingPlatform
    {
        [SerializeField] Transform pivot;
        [SerializeField] float radius;
        [SerializeField] float startAngleDegrees;
        [SerializeField] float arcSpanDegrees = 70f;
        [SerializeField] float angularSpeedDegrees = 22f;
        [SerializeField] float angleSmoothTime = 0.12f;
        [SerializeField] bool autoConfigureFromStartPose = true;
        [SerializeField] bool pingPong = true;

        Rigidbody platformRb;
        float currentAngleDegrees;
        float targetAngleDegrees;
        float angleVelocity;
        float sweepDirection = 1f;
        float minAngleDegrees;
        float maxAngleDegrees;
        Vector3 previousPosition;

        public Vector3 FrameDelta { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Transform Transform => transform;

        void Awake()
        {
            platformRb = GetComponent<Rigidbody>();
            platformRb.isKinematic = true;
            platformRb.useGravity = false;
            platformRb.interpolation = RigidbodyInterpolation.Interpolate;
            platformRb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

            if (autoConfigureFromStartPose && pivot != null)
                ConfigureFromCurrentPose();

            CacheArcBounds();
            currentAngleDegrees = startAngleDegrees;
            targetAngleDegrees = startAngleDegrees;
            previousPosition = transform.position;
            ApplyPosition(currentAngleDegrees);
            FrameDelta = Vector3.zero;
            Velocity = Vector3.zero;
        }

        void FixedUpdate()
        {
            if (pivot == null || radius <= 0f)
            {
                FrameDelta = Vector3.zero;
                Velocity = Vector3.zero;
                return;
            }

            previousPosition = platformRb.position;

            AdvanceTargetAngle();
            currentAngleDegrees = Mathf.SmoothDampAngle(
                currentAngleDegrees,
                targetAngleDegrees,
                ref angleVelocity,
                angleSmoothTime);

            ApplyPosition(currentAngleDegrees);
            FrameDelta = platformRb.position - previousPosition;
            Velocity = FrameDelta / Time.fixedDeltaTime;
        }

        void ConfigureFromCurrentPose()
        {
            Vector2 offset = ToPlane(pivot.position, transform.position);
            radius = offset.magnitude;
            startAngleDegrees = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }

        void CacheArcBounds()
        {
            float span = Mathf.Abs(arcSpanDegrees);
            float sign = Mathf.Sign(arcSpanDegrees == 0f ? 1f : arcSpanDegrees);

            minAngleDegrees = startAngleDegrees;
            maxAngleDegrees = startAngleDegrees + span * sign;

            if (minAngleDegrees > maxAngleDegrees)
                (minAngleDegrees, maxAngleDegrees) = (maxAngleDegrees, minAngleDegrees);
        }

        void AdvanceTargetAngle()
        {
            targetAngleDegrees += sweepDirection * angularSpeedDegrees * Time.fixedDeltaTime;

            if (!pingPong)
                return;

            if (targetAngleDegrees >= maxAngleDegrees)
            {
                targetAngleDegrees = maxAngleDegrees;
                sweepDirection = -1f;
            }
            else if (targetAngleDegrees <= minAngleDegrees)
            {
                targetAngleDegrees = minAngleDegrees;
                sweepDirection = 1f;
            }
        }

        void ApplyPosition(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float x = pivot.position.x + radius * Mathf.Cos(radians);
            float y = pivot.position.y + radius * Mathf.Sin(radians);

            platformRb.MovePosition(new Vector3(x, y, pivot.position.z));
        }

        static Vector2 ToPlane(Vector3 origin, Vector3 point)
        {
            return new Vector2(point.x - origin.x, point.y - origin.y);
        }

        void OnDrawGizmosSelected()
        {
            if (pivot == null || radius <= 0f)
                return;

            Vector3 center = pivot.position;
            float span = Mathf.Abs(arcSpanDegrees);
            int segments = Mathf.Max(8, Mathf.RoundToInt(span / 5f));
            float sign = Mathf.Sign(arcSpanDegrees == 0f ? 1f : arcSpanDegrees);
            float arcStart = startAngleDegrees;
            float arcEnd = startAngleDegrees + span * sign;

            Vector3 previous = center + PolarToOffset(arcStart);
            Gizmos.color = new Color(0.9f, 0.75f, 0.35f, 0.9f);

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(arcStart, arcEnd, t);
                Vector3 next = center + PolarToOffset(angle);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.DrawWireSphere(center, 0.12f);
        }

        Vector3 PolarToOffset(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                radius * Mathf.Cos(radians),
                radius * Mathf.Sin(radians),
                0f);
        }
    }
}
