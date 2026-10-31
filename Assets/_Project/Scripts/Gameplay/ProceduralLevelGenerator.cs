using System.Collections;
using System.Collections.Generic;
using EchoesOfAscent.Core;
using EchoesOfAscent.Meta;
using EchoesOfAscent.Presentation;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [DefaultExecutionOrder(-200)]
    public sealed class ProceduralLevelGenerator : MonoBehaviour
    {
        public const int PlatformCount = 11;

        [SerializeField] JumpConfig jumpConfig;
        [SerializeField] PlatformPrefabCatalog platformPrefabCatalog;
        [SerializeField] int randomSeed;
        [SerializeField] bool useSharedServerSeed = true;
        [SerializeField] bool deactivateScenePlatforms = true;
        [SerializeField] float platformGap = 0.3f;
        [SerializeField] Vector2 startPlatformScale = new Vector2(4f, 0.5f);
        [SerializeField] Vector2 normalPlatformWidthRange = new Vector2(2.2f, 3.2f);
        [SerializeField] float normalPlatformHeight = 0.4f;
        [SerializeField] float minVerticalRise = 0.9f;
        [SerializeField] float maxVerticalRise = 3f;
        [SerializeField] int maxPlacementAttempts = 80;
        [SerializeField] CameraFollowController cameraFollow;
        [SerializeField] Camera gameplayCamera;
        [SerializeField] float cameraCenterYOffset = 3f;
        [SerializeField] float nextPlatformMaxScreenWidthFraction = 0.62f;
        [SerializeField] float nextPlatformMaxScreenHeightFraction = 0.48f;
        [SerializeField] float comfortableImpulseMinFraction = 0.45f;
        [SerializeField] float comfortableImpulseMaxFraction = 0.82f;
        [SerializeField] float maxHorizontalPlacementFraction = 0.72f;
        [SerializeField] Material platformMaterial;
        [SerializeField] PlatformMaterialPalette platformMaterialPalette;
        [SerializeField] Color finishPlatformColor = new Color(0.2f, 0.85f, 0.35f, 1f);

        static readonly string[] LegacyObjectNames =
        {
            "Platform_Start",
            "Platform_Upper",
            "Platform_Mid",
            "Platform_High",
            "Platform_Moving",
            "Ground"
        };

        readonly List<PlatformSpec> platforms = new List<PlatformSpec>(PlatformCount);
        Transform levelRoot;
        PlacementBudget placementBudget;
        int activeSeed;

        struct PlacementBudget
        {
            public float MinHorizontal;
            public float MaxHorizontal;
            public float MinRise;
            public float MaxRise;
        }

        public int FinishCheckpointId => PlatformCount - 1;

        /// <summary>
        /// Seed, used for this run's procedural layout and echo scoping.
        /// </summary>
        public int ActiveSeed => activeSeed;

        /// <summary>
        /// True after the shared seed is resolved and the level has been generated.
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Active procedural generator in the loaded scene, if any.
        /// </summary>
        public static ProceduralLevelGenerator Current { get; private set; }

        void Awake()
        {
            Current = this;
            IsReady = false;
            StartCoroutine(BootstrapLevel());
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        IEnumerator BootstrapLevel()
        {
            if (jumpConfig == null)
            {
                JumpConfig[] configs = Resources.FindObjectsOfTypeAll<JumpConfig>();
                if (configs.Length > 0)
                    jumpConfig = configs[0];
            }

            if (platformMaterial == null)
                platformMaterial = PlatformMaterialResolver.Resolve();

            if (platformMaterialPalette == null || !platformMaterialPalette.HasShaders)
            {
                Debug.LogWarning(
                    "ProceduralLevelGenerator: PlatformMaterialPalette is missing or empty. " +
                    "Run Ascent Tools > Sync Platform Material Palette after generating shaders.");
            }

            yield return ResolveSeedCoroutine();

            Random.InitState(activeSeed);
            SaveService.SaveLevelSeed(activeSeed);

            if (deactivateScenePlatforms)
                DeactivateLegacySceneObjects();

            SaveService.ResetProgress();
            GenerateLevel();
            UpdateCameraBounds();
            RefreshPlayerCheckpoints();
            IsReady = true;

            Debug.Log($"[ProceduralLevelGenerator] Ready with shared seed {activeSeed}");
        }

        IEnumerator ResolveSeedCoroutine()
        {
            if (randomSeed != 0)
            {
                activeSeed = randomSeed;
                yield break;
            }

            if (useSharedServerSeed)
            {
                string baseUrl = EchoesManager.Instance != null
                    ? EchoesManager.Instance.ServerBaseUrl
                    : EchoApi.DefaultBaseUrl;

                int sharedSeed = 0;
                yield return EchoApi.GetSharedSeed(baseUrl, seed => sharedSeed = seed);

                if (sharedSeed != 0)
                {
                    activeSeed = sharedSeed;
                    yield break;
                }

                Debug.LogWarning(
                    "[ProceduralLevelGenerator] Shared seed unavailable; falling back to local seed. " +
                    "Devices will not share the same layout/echoes until the backend is reachable.");
            }

            if (SaveService.TryLoadLevelSeed(out int savedSeed))
            {
                activeSeed = savedSeed;
                yield break;
            }

            activeSeed = UnityEngine.Random.Range(1, int.MaxValue);
        }

        void OnEnable()
        {
            GameEvents.CheckpointTouched += HandleCheckpointTouched;
        }

        void OnDisable()
        {
            GameEvents.CheckpointTouched -= HandleCheckpointTouched;
        }

        void HandleCheckpointTouched(int checkpointId)
        {
            if (checkpointId == FinishCheckpointId)
                GameEvents.RaiseLevelCompleted();
        }

        void DeactivateLegacySceneObjects()
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < LegacyObjectNames.Length; i++)
            {
                string legacyName = LegacyObjectNames[i];
                for (int t = 0; t < transforms.Length; t++)
                {
                    Transform candidate = transforms[t];
                    if (candidate.name != legacyName || candidate.parent != null)
                        continue;

                    candidate.gameObject.SetActive(false);
                }
            }

            DeactivateOrphanSceneStonePlatforms();
        }

        static void DeactivateOrphanSceneStonePlatforms()
        {
            PlatformSpawnPoint[] spawnPoints = Object.FindObjectsByType<PlatformSpawnPoint>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                PlatformSpawnPoint spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                    continue;

                if (spawnPoint.transform.root.name == "GeneratedLevel")
                    continue;

                spawnPoint.gameObject.SetActive(false);
            }
        }

        void GenerateLevel()
        {
            platforms.Clear();

            if (platformPrefabCatalog == null || !platformPrefabCatalog.HasPrefabs)
            {
                Debug.LogError(
                    "ProceduralLevelGenerator: assign PlatformPrefabCatalog with generated stone prefabs. " +
                    "Use Ascent Tools > Refresh Platform Catalog.");
                return;
            }

            if (levelRoot != null)
                Destroy(levelRoot.gameObject);

            levelRoot = new GameObject("GeneratedLevel").transform;

            if (gameplayCamera == null)
                gameplayCamera = Camera.main;

            JumpReachMetrics comfortableReach = JumpReachAnalyzer.ComputeComfortableReach(
                jumpConfig,
                comfortableImpulseMinFraction,
                comfortableImpulseMaxFraction);
            placementBudget = BuildPlacementBudget(comfortableReach);

            CreatePlatform(0, 0f, startPlatformScale.y, startPlatformScale.x, isFinish: false, default);

            for (int platformIndex = 1; platformIndex < PlatformCount; platformIndex++)
            {
                PlatformSpec previous = platforms[platformIndex - 1];
                if (!TryPlaceRandomPlatform(platformIndex, previous))
                    PlaceDeterministicPlatform(platformIndex, previous);
            }
        }

        PlacementBudget BuildPlacementBudget(JumpReachMetrics comfortableReach)
        {
            float minHorizontal = Mathf.Max(1.1f, comfortableReach.MinHorizontal);
            float maxHorizontal = comfortableReach.MaxHorizontal * maxHorizontalPlacementFraction;
            float minRise = Mathf.Max(0.45f, minVerticalRise * 0.55f);
            float maxRise = Mathf.Min(maxVerticalRise, comfortableReach.MaxRise * 0.85f);

            if (gameplayCamera != null && gameplayCamera.orthographic)
            {
                float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
                float halfHeight = gameplayCamera.orthographicSize;
                maxHorizontal = Mathf.Min(maxHorizontal, halfWidth * nextPlatformMaxScreenWidthFraction);
                maxRise = Mathf.Min(maxRise, halfHeight * nextPlatformMaxScreenHeightFraction);
            }

            maxHorizontal = Mathf.Max(minHorizontal + 0.35f, maxHorizontal);
            maxRise = Mathf.Max(minRise + 0.25f, maxRise);

            return new PlacementBudget
            {
                MinHorizontal = minHorizontal,
                MaxHorizontal = maxHorizontal,
                MinRise = minRise,
                MaxRise = maxRise
            };
        }

        bool TryPlaceRandomPlatform(int platformIndex, PlatformSpec previous)
        {
            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float width = platformIndex == FinishCheckpointId
                    ? Mathf.Max(normalPlatformWidthRange.y, 3f)
                    : Random.Range(normalPlatformWidthRange.x, normalPlatformWidthRange.y);

                float direction = Random.value > 0.5f ? 1f : -1f;
                float horizontalOffset = direction * Random.Range(placementBudget.MinHorizontal, placementBudget.MaxHorizontal);
                float verticalOffset = Random.Range(placementBudget.MinRise, placementBudget.MaxRise);
                float centerX = previous.CenterX + horizontalOffset;
                float topY = previous.TopY + verticalOffset;

                if (!TryBuildCandidate(centerX, topY, width, out PlatformSpec candidate, out PlatformPrefabCatalog.Entry catalogEntry))
                    continue;

                if (!IsValidPlacement(previous, candidate))
                    continue;

                CreatePlatform(platformIndex, centerX, topY, width, platformIndex == FinishCheckpointId, catalogEntry);
                return true;
            }

            return false;
        }

        void PlaceDeterministicPlatform(int platformIndex, PlatformSpec previous)
        {
            for (int variant = 0; variant < 10; variant++)
            {
                float direction = variant % 2 == 0 ? 1f : -1f;
                float width = platformIndex == FinishCheckpointId ? 3.2f : 2.6f;
                float horizontalT = 0.35f + variant * 0.04f;
                float verticalT = 0.3f + variant * 0.06f;
                float horizontalOffset = direction * Mathf.Lerp(
                    placementBudget.MinHorizontal,
                    placementBudget.MaxHorizontal,
                    horizontalT);
                float verticalOffset = Mathf.Lerp(
                    placementBudget.MinRise,
                    placementBudget.MaxRise,
                    verticalT);
                float centerX = previous.CenterX + horizontalOffset;
                float topY = previous.TopY + verticalOffset;

                if (!TryBuildCandidate(centerX, topY, width, out PlatformSpec candidate, out PlatformPrefabCatalog.Entry catalogEntry))
                    continue;

                if (!IsValidPlacement(previous, candidate))
                    continue;

                CreatePlatform(platformIndex, centerX, topY, width, platformIndex == FinishCheckpointId, catalogEntry);
                return;
            }

            float fallbackWidth = 2.6f;
            float fallbackX = previous.CenterX + placementBudget.MinHorizontal;
            float fallbackTopY = previous.TopY + placementBudget.MinRise;
            CreatePlatform(platformIndex, fallbackX, fallbackTopY, fallbackWidth, platformIndex == FinishCheckpointId, default);
        }

        bool TryBuildCandidate(
            float centerX,
            float topY,
            float width,
            out PlatformSpec candidate,
            out PlatformPrefabCatalog.Entry catalogEntry)
        {
            catalogEntry = platformPrefabCatalog.PickRandomEntry();
            candidate = PlatformSpecUtility.EstimateFromEntry(catalogEntry, width, centerX, topY);
            return true;
        }

        bool IsValidPlacement(PlatformSpec previous, PlatformSpec candidate)
        {
            if (OverlapsAny(candidate))
                return false;

            if (!IsVisibleFromPrevious(previous, candidate))
                return false;

            return JumpReachAnalyzer.CanJumpBetween(
                previous,
                candidate,
                jumpConfig,
                minImpulseFraction: comfortableImpulseMinFraction,
                maxImpulseFraction: comfortableImpulseMaxFraction);
        }

        bool IsVisibleFromPrevious(PlatformSpec previous, PlatformSpec candidate)
        {
            if (gameplayCamera == null || !gameplayCamera.orthographic)
                return true;

            float cameraCenterX = previous.CenterX;
            float cameraCenterY = previous.TopY + cameraCenterYOffset;
            float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
            float halfHeight = gameplayCamera.orthographicSize;

            float maxDeltaX = halfWidth * nextPlatformMaxScreenWidthFraction;
            float minVisibleY = cameraCenterY - halfHeight * 0.2f;
            float maxVisibleY = cameraCenterY + halfHeight * nextPlatformMaxScreenHeightFraction;

            bool xVisible = Mathf.Abs(candidate.CenterX - cameraCenterX) <= maxDeltaX;
            bool yVisible = candidate.TopY >= minVisibleY && candidate.TopY <= maxVisibleY;
            return xVisible && yVisible;
        }

        bool OverlapsAny(PlatformSpec candidate)
        {
            for (int i = 0; i < platforms.Count; i++)
            {
                PlatformSpec existing = platforms[i];
                if (RectsOverlap(candidate, existing, platformGap))
                    return true;
            }

            return false;
        }

        static bool RectsOverlap(PlatformSpec a, PlatformSpec b, float gap)
        {
            float aMinX = a.MinX - gap;
            float aMaxX = a.MaxX + gap;
            float aMinY = a.Position.y - a.Scale.y * 0.5f - gap;
            float aMaxY = a.TopY + gap;

            float bMinX = b.MinX - gap;
            float bMaxX = b.MaxX + gap;
            float bMinY = b.Position.y - b.Scale.y * 0.5f - gap;
            float bMaxY = b.TopY + gap;

            bool separatedX = aMaxX < bMinX || bMaxX < aMinX;
            bool separatedY = aMaxY < bMinY || bMaxY < aMinY;
            return !separatedX && !separatedY;
        }

        void CreatePlatform(
            int checkpointId,
            float centerX,
            float topY,
            float width,
            bool isFinish,
            PlatformPrefabCatalog.Entry catalogEntry)
        {
            if (catalogEntry.prefab == null)
                catalogEntry = platformPrefabCatalog.PickRandomEntry();

            GameObject platformObject = platformPrefabCatalog.InstantiateScaled(
                levelRoot,
                catalogEntry,
                width,
                centerX,
                topY,
                checkpointId,
                platformMaterialPalette,
                platformMaterial,
                isFinish,
                finishPlatformColor,
                out PlatformSpec spec);

            platformObject.name = isFinish ? "Platform_Finish" : $"Platform_{checkpointId + 1}";
            platformObject.tag = "Ground";
            int groundLayer = LayerMask.NameToLayer("Ground");
            platformObject.layer = groundLayer >= 0 ? groundLayer : 7;

            if (!platformObject.TryGetComponent(out PlatformSpawnPoint spawnPoint))
                spawnPoint = platformObject.AddComponent<PlatformSpawnPoint>();

            Collider gameplayCollider = platformObject.GetComponent<BoxCollider>();
            if (gameplayCollider == null)
                gameplayCollider = platformObject.GetComponent<Collider>();
            if (gameplayCollider != null)
                spawnPoint.BindPlatformCollider(gameplayCollider);

            Checkpoint checkpoint = platformObject.GetComponent<Checkpoint>();
            if (checkpoint == null)
                checkpoint = platformObject.AddComponent<Checkpoint>();

            checkpoint.Configure(checkpointId);
            platforms.Add(spec);
        }

        void UpdateCameraBounds()
        {
            if (cameraFollow == null)
                cameraFollow = FindFirstObjectByType<CameraFollowController>();

            if (cameraFollow == null || platforms.Count == 0)
                return;

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < platforms.Count; i++)
            {
                PlatformSpec platform = platforms[i];
                minX = Mathf.Min(minX, platform.MinX);
                maxX = Mathf.Max(maxX, platform.MaxX);
                maxY = Mathf.Max(maxY, platform.TopY);
            }

            cameraFollow.SetBounds(minX - 4f, maxX + 4f, 0f, maxY + 8f);
        }

        static void RefreshPlayerCheckpoints()
        {
            PlayerRespawnController[] respawnControllers = Object.FindObjectsByType<PlayerRespawnController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < respawnControllers.Length; i++)
                respawnControllers[i].RefreshCheckpoints();
        }
    }
}
