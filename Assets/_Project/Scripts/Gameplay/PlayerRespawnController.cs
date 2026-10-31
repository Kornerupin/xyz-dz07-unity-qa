using EchoesOfAscent.Core;
using EchoesOfAscent.Meta;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GroundDetector))]
    [DefaultExecutionOrder(50)]
    public sealed class PlayerRespawnController : MonoBehaviour
    {
        const float PlatformProbeDistance = 0.4f;

        [SerializeField] int defaultCheckpointId;
        [SerializeField] bool loadSavedCheckpointOnStart;
        [SerializeField] LayerMask groundLayers = ~0;

        Rigidbody rb;
        Collider bodyCollider;
        GroundDetector groundDetector;
        Checkpoint[] checkpoints;
        Checkpoint activeCheckpoint;
        bool pendingInitialSnap = true;
        int standingCheckpointId = -1;

        public int ActiveCheckpointId => activeCheckpoint != null ? activeCheckpoint.CheckpointId : defaultCheckpointId;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            groundDetector = GetComponent<GroundDetector>();
            checkpoints = FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        void Start()
        {
            int spawnCheckpointId = loadSavedCheckpointOnStart
                ? SaveService.LoadCheckpoint()
                : defaultCheckpointId;

            Checkpoint spawnCheckpoint = FindCheckpoint(spawnCheckpointId) ?? FindCheckpoint(defaultCheckpointId);
            if (spawnCheckpoint != null)
                SetActiveCheckpoint(spawnCheckpoint, persist: false);
        }

        void FixedUpdate()
        {
            PlatformSpawnPoint respawnPlatform = GetRespawnPlatform();
            if (respawnPlatform == null)
                return;

            if (pendingInitialSnap)
            {
                RespawnAt(respawnPlatform);
                pendingInitialSnap = false;
                standingCheckpointId = activeCheckpoint != null ? activeCheckpoint.CheckpointId : -1;
                return;
            }

            DetectStandingCheckpoint();

            float halfWidth = bodyCollider.bounds.extents.x;
            if (respawnPlatform.ShouldRespawn(transform.position, rb.linearVelocity, halfWidth))
            {
                RecordFallEcho();
                RespawnAt(respawnPlatform);
            }
        }

        void RecordFallEcho()
        {
            if (EchoesManager.Instance == null)
                return;

            var tracker = GetComponent<FallEchoTracker>();
            Vector3 echoPosition = tracker != null ? tracker.LastGroundedPosition : transform.position;
            EchoesManager.Instance.RecordFall(echoPosition);
        }

        void DetectStandingCheckpoint()
        {
            if (!groundDetector.IsGrounded())
            {
                standingCheckpointId = -1;
                return;
            }

            if (!TryGetCheckpointBelow(out Checkpoint checkpoint))
                return;

            if (checkpoint.CheckpointId == standingCheckpointId)
                return;

            standingCheckpointId = checkpoint.CheckpointId;
            GameEvents.RaiseCheckpointTouched(checkpoint.CheckpointId);
            checkpoint.Activate(this);
        }

        /// <summary>
        /// Пересканирует чекпоинты сцены и сбрасывает спавн на стартовый.
        /// </summary>
        public void RefreshCheckpoints()
        {
            checkpoints = FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Checkpoint spawnCheckpoint = FindCheckpoint(defaultCheckpointId);
            if (spawnCheckpoint != null)
            {
                SetActiveCheckpoint(spawnCheckpoint, persist: false);
                pendingInitialSnap = true;
            }
        }

        /// <summary>
        /// Регистрирует новый чекпоинт, если его ID больше текущего активного.
        /// </summary>
        /// <param name="checkpoint">Чекпоинт для регистрации.</param>
        public void RegisterCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null)
                return;

            if (activeCheckpoint != null && checkpoint.CheckpointId <= activeCheckpoint.CheckpointId)
                return;

            SetActiveCheckpoint(checkpoint, persist: true);
        }

        /// <summary>
        /// Возвращает игрока на последний активный чекпоинт.
        /// </summary>
        public void RespawnAtLastCheckpoint()
        {
            PlatformSpawnPoint platform = GetRespawnPlatform();
            if (platform != null)
                RespawnAt(platform);
        }

        PlatformSpawnPoint GetRespawnPlatform()
        {
            return activeCheckpoint != null ? activeCheckpoint.Platform : null;
        }

        void SetActiveCheckpoint(Checkpoint checkpoint, bool persist)
        {
            activeCheckpoint = checkpoint;

            if (persist)
            {
                SaveService.SaveCheckpoint(checkpoint.CheckpointId);
                GameEvents.RaiseCheckpointReached(checkpoint.CheckpointId);
            }
        }

        Checkpoint FindCheckpoint(int checkpointId)
        {
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i].CheckpointId == checkpointId)
                    return checkpoints[i];
            }

            return null;
        }

        bool TryGetCheckpointBelow(out Checkpoint checkpoint)
        {
            checkpoint = null;

            if (!groundDetector.TryGetGroundHit(out RaycastHit hit))
                return false;

            checkpoint = hit.collider.GetComponent<Checkpoint>();
            if (checkpoint == null)
                checkpoint = hit.collider.GetComponentInParent<Checkpoint>();

            return checkpoint != null;
        }

        void RespawnAt(PlatformSpawnPoint platform)
        {
            Physics.SyncTransforms();
            float halfHeight = bodyCollider.bounds.extents.y;
            Vector3 spawnPosition = platform.GetSpawnPosition(halfHeight);

            Collider platformCollider = platform.PlatformCollider;
            var ray = new Ray(spawnPosition + Vector3.up * 3f, Vector3.down);
            if (platformCollider != null
                && platformCollider.Raycast(ray, out RaycastHit hit, 6f)
                && hit.collider != bodyCollider)
            {
                spawnPosition.y = hit.point.y + halfHeight + 0.02f;
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPosition;
            transform.position = spawnPosition;
            rb.WakeUp();
            Physics.SyncTransforms();

            standingCheckpointId = platform.GetComponent<Checkpoint>()?.CheckpointId ?? standingCheckpointId;
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GroundDetector))]
    [DefaultExecutionOrder(60)]
    public sealed class PlayerMovingPlatformCarrier : MonoBehaviour
    {
        const float PlatformProbeDistance = 0.5f;
        const float RidingVerticalSpeedThreshold = 0.2f;

        [SerializeField] LayerMask groundLayers = ~0;

        Rigidbody rb;
        Collider bodyCollider;
        GroundDetector groundDetector;
        IMovingPlatform ridingPlatform;
        Vector3 localOffsetOnPlatform;

        public bool IsRidingMovingPlatform => ridingPlatform != null;
        public Vector3 RidingVelocity => ridingPlatform != null ? ridingPlatform.Velocity : Vector3.zero;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            groundDetector = GetComponent<GroundDetector>();
        }

        void FixedUpdate()
        {
            if (!groundDetector.IsGrounded())
            {
                Detach();
                return;
            }

            if (!TryGetMovingPlatformBelow(out IMovingPlatform movingPlatform))
            {
                Detach();
                return;
            }

            if (ridingPlatform != movingPlatform)
                Attach(movingPlatform);

            StickToPlatform(ridingPlatform);
        }

        void OnCollisionStay(Collision collision)
        {
            if (!groundDetector.IsGrounded())
                return;

            IMovingPlatform movingPlatform = collision.collider.GetComponent<IMovingPlatform>();
            if (movingPlatform == null)
                movingPlatform = collision.collider.GetComponentInParent<IMovingPlatform>();

            if (movingPlatform == null)
                return;

            if (!HasUpwardSupport(collision))
                return;

            if (ridingPlatform != movingPlatform)
                Attach(movingPlatform);
        }

        void Attach(IMovingPlatform movingPlatform)
        {
            ridingPlatform = movingPlatform;
            localOffsetOnPlatform = movingPlatform.Transform.InverseTransformPoint(rb.position);
        }

        void Detach()
        {
            ridingPlatform = null;
        }

        void StickToPlatform(IMovingPlatform movingPlatform)
        {
            Vector3 gluedPosition = movingPlatform.Transform.TransformPoint(localOffsetOnPlatform);
            gluedPosition.z = 0f;

            rb.MovePosition(gluedPosition);

            Vector3 platformVelocity = movingPlatform.Velocity;
            float verticalVelocity = rb.linearVelocity.y;
            if (verticalVelocity <= RidingVerticalSpeedThreshold)
                verticalVelocity = 0f;

            rb.linearVelocity = new Vector3(platformVelocity.x, verticalVelocity, 0f);
        }

        bool TryGetMovingPlatformBelow(out IMovingPlatform movingPlatform)
        {
            movingPlatform = null;

            if (!groundDetector.TryGetGroundHit(out RaycastHit hit))
                return false;

            movingPlatform = hit.collider.GetComponent<IMovingPlatform>();
            if (movingPlatform == null)
                movingPlatform = hit.collider.GetComponentInParent<IMovingPlatform>();

            return movingPlatform != null;
        }

        static bool HasUpwardSupport(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                    return true;
            }

            return false;
        }
    }
}
