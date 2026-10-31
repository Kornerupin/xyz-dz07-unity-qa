using System.Collections;
using System.Linq;
using System.Reflection;
using EchoesOfAscent.Core;
using EchoesOfAscent.Gameplay;
using EchoesOfAscent.Meta;
using EchoesOfAscent.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoesOfAscent.Tests.PlayMode
{
    /// <summary>
    /// BDD integration coverage for positive GDD scenarios (TC-001..TC-016).
    /// Source: EchoesOfAscent_GDD.pdf + тесты.txt
    /// </summary>
    public sealed class GddPositiveBddIntegrationTests
    {
        JumpConfig jumpConfig;
        Camera gameplayCamera;
        JumpCalculator jumpCalculator;

        [SetUp]
        public void SetUp()
        {
            BddTestSceneFactory.Reset();
            jumpConfig = BddTestSceneFactory.CreateJumpConfig();
            gameplayCamera = BddTestSceneFactory.CreateGameplayCamera(new Vector3(0f, 3f, -10f));
            jumpCalculator = new JumpCalculator();
        }

        [TearDown]
        public void TearDown()
        {
            BddTestSceneFactory.Reset();
        }

        // TC-001 | EP | Valid swipe length -> jump executes
        [UnityTest]
        public IEnumerator TC001_ValidSwipeLength_ExecutesJump()
        {
            BddTestSceneFactory.PlatformFixture platform = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                platform.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: 35f,
                swipeMagnitudePixels: 120f);
            Assert.That(
                jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera).sqrMagnitude,
                Is.GreaterThan(0.01f));

            player.SwipeInput.InjectSwipeRelease(swipe);
            yield return WaitUntilJumpApplied(player);

            Assert.That(player.Rigidbody.linearVelocity.sqrMagnitude, Is.GreaterThan(0.1f));
        }

        // TC-002 | BVA | Swipe exactly 25 px -> jump executes
        [UnityTest]
        public IEnumerator TC002_SwipeExactly25Pixels_ExecutesJump()
        {
            BddTestSceneFactory.PlatformFixture platform = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                platform.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: 30f,
                swipeMagnitudePixels: BddTestSceneFactory.MinSwipePixels);
            Assert.That(swipe.IsValid(jumpConfig.minSwipePixels), Is.True);

            player.SwipeInput.InjectSwipeRelease(swipe);
            yield return WaitUntilJumpApplied(player);

            Assert.That(player.Rigidbody.linearVelocity.sqrMagnitude, Is.GreaterThan(0.1f));
        }

        // TC-003 | EP | Release after hold -> jump starts on release
        [UnityTest]
        public IEnumerator TC003_SwipeRelease_StartsJumpAfterFingerUp()
        {
            BddTestSceneFactory.PlatformFixture platform = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                platform.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: 35f,
                swipeMagnitudePixels: 90f);

            player.Rigidbody.linearVelocity = Vector3.zero;
            player.Rigidbody.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            player.SwipeInput.InjectSwipeUpdate(swipe);

            Assert.That(
                player.Rigidbody.linearVelocity.sqrMagnitude,
                Is.LessThan(0.01f),
                "Swipe hold must not apply launch velocity.");

            yield return null;

            player.SwipeInput.InjectSwipeRelease(swipe);
            yield return WaitUntilJumpApplied(player);

            Assert.That(player.Rigidbody.linearVelocity.sqrMagnitude, Is.GreaterThan(0.1f));
        }

        // TC-004 | EP | Upward swipe in 8-75 deg range -> jump in chosen direction
        [UnityTest]
        public IEnumerator TC004_UpwardSwipeInAllowedAngleRange_JumpsInDirection()
        {
            BddTestSceneFactory.PlatformFixture platform = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                platform.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: 45f,
                swipeMagnitudePixels: 140f,
                launchToRight: true);
            Vector3 launchVelocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);
            float elevation = BddTestSceneFactory.GetLaunchElevationDegrees(launchVelocity);

            Assert.That(elevation, Is.InRange(jumpConfig.minLaunchElevation, jumpConfig.maxLaunchAngle));
            Assert.That(launchVelocity.x, Is.GreaterThan(0f));

            player.SwipeInput.InjectSwipeRelease(swipe);
            yield return WaitUntilJumpApplied(player);

            Assert.That(Mathf.Sign(player.Rigidbody.linearVelocity.x), Is.EqualTo(1f));
            Assert.That(player.Rigidbody.linearVelocity.y, Is.GreaterThan(0f));
        }

        // TC-005 | BVA | Launch angle 8 deg -> jump calculated correctly
        [Test]
        public void TC005_LaunchAngleAtMinimumBoundary_IsCalculatedCorrectly()
        {
            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: jumpConfig.minLaunchElevation,
                swipeMagnitudePixels: 160f);
            Vector3 velocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);
            float elevation = BddTestSceneFactory.GetLaunchElevationDegrees(velocity);

            Assert.That(velocity.sqrMagnitude, Is.GreaterThan(0.01f));
            Assert.That(elevation, Is.EqualTo(jumpConfig.minLaunchElevation).Within(0.35f));
        }

        // TC-006 | BVA | Launch angle 75 deg -> jump calculated correctly
        [Test]
        public void TC006_LaunchAngleAtMaximumBoundary_IsCalculatedCorrectly()
        {
            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: jumpConfig.maxLaunchAngle,
                swipeMagnitudePixels: 220f);
            Vector3 velocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);
            float elevation = BddTestSceneFactory.GetLaunchElevationDegrees(velocity);

            Assert.That(velocity.sqrMagnitude, Is.GreaterThan(0.01f));
            Assert.That(elevation, Is.EqualTo(jumpConfig.maxLaunchAngle).Within(0.35f));
        }

        // TC-007 | BVA | Minimum impulse from shortest valid swipe
        [Test]
        public void TC007_MinimumImpulse_ProducesMinimumJump()
        {
            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: 30f,
                swipeMagnitudePixels: BddTestSceneFactory.MinSwipePixels);

            Assert.That(swipe.IsValid(jumpConfig.minSwipePixels), Is.True);

            float expectedImpulse = Mathf.Lerp(
                jumpConfig.minImpulse,
                jumpConfig.maxImpulse,
                BddTestSceneFactory.MinSwipePixels / BddTestSceneFactory.MaxReferencePixels);
            float impulse = jumpCalculator.EvaluateImpulse(swipe, jumpConfig);
            Vector3 velocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);

            Assert.That(impulse, Is.EqualTo(expectedImpulse).Within(0.01f));
            Assert.That(velocity.magnitude, Is.EqualTo(expectedImpulse).Within(0.05f));
        }

        // TC-008 | BVA | Maximum impulse (18) -> maximum jump
        [Test]
        public void TC008_MaximumImpulse_ProducesMaximumJump()
        {
            SwipeData swipe = BddTestSceneFactory.BuildSwipeForMaxImpulse(
                jumpCalculator,
                jumpConfig,
                gameplayCamera);

            float impulse = jumpCalculator.EvaluateImpulse(swipe, jumpConfig);
            Vector3 velocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);

            Assert.That(impulse, Is.EqualTo(jumpConfig.maxImpulse).Within(0.01f));
            Assert.That(velocity.magnitude, Is.EqualTo(jumpConfig.maxImpulse).Within(0.05f));
        }

        // TC-009 | EP | Trajectory preview while holding -> no more than 40 points
        [UnityTest]
        public IEnumerator TC009_TrajectoryPreviewWhileHolding_UsesAtMost40Points()
        {
            BddTestSceneFactory.PlatformFixture platform = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                platform.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            SwipeData swipe = BddTestSceneFactory.BuildSwipeForLaunch(
                jumpCalculator,
                jumpConfig,
                gameplayCamera,
                targetElevationDegrees: 40f,
                swipeMagnitudePixels: 180f);
            player.SwipeInput.InjectSwipeUpdate(swipe);
            yield return null;

            TrajectoryPreviewRenderer preview = player.Root.GetComponentInChildren<TrajectoryPreviewRenderer>();
            LineRenderer lineRenderer = preview.GetComponent<LineRenderer>();

            Bounds bodyBounds = player.Root.GetComponent<Collider>().bounds;
            Vector3 trajectoryOrigin = new Vector3(bodyBounds.center.x, bodyBounds.min.y, bodyBounds.center.z);
            Vector3 launchVelocity = jumpCalculator.ScreenDeltaToLaunchVelocity(swipe, jumpConfig, gameplayCamera);
            Vector3[] expectedArc = jumpCalculator.SampleTrajectory(trajectoryOrigin, launchVelocity, jumpConfig);

            Assert.That(lineRenderer.enabled, Is.True);
            Assert.That(lineRenderer.positionCount, Is.EqualTo(expectedArc.Length));
            Assert.That(lineRenderer.positionCount, Is.LessThanOrEqualTo(40));
            Assert.That(lineRenderer.positionCount, Is.GreaterThan(1));
        }

        // TC-010 | EP | Landing on platform -> checkpoint updates
        [UnityTest]
        public IEnumerator TC010_LandingOnPlatform_UpdatesCheckpoint()
        {
            BddTestSceneFactory.PlatformFixture start = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlatformFixture next = BddTestSceneFactory.CreatePlatform(
                1,
                new Vector3(3f, 2.25f, 0f),
                new Vector3(4f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                start.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            yield return BddTestSceneFactory.LandPlayerOnPlatform(player, next, expectedCheckpointId: 1);

            Assert.That(player.Respawn.ActiveCheckpointId, Is.EqualTo(1));
            Assert.That(SaveService.LoadCheckpoint(), Is.EqualTo(1));
        }

        // TC-011 | EP | Fall after saved checkpoint -> respawn at last checkpoint
        [UnityTest]
        public IEnumerator TC011_FallAfterCheckpoint_RespawnsAtLastCheckpoint()
        {
            BddTestSceneFactory.PlatformFixture start = BddTestSceneFactory.CreatePlatform(
                0,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(6f, 0.5f, 1f));
            BddTestSceneFactory.PlatformFixture checkpoint = BddTestSceneFactory.CreatePlatform(
                1,
                new Vector3(3f, 2.25f, 0f),
                new Vector3(4f, 0.5f, 1f));
            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                start.SpawnPoint.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);

            Vector3 checkpointSpawn = checkpoint.SpawnPoint.GetSpawnPosition(0.45f);
            yield return BddTestSceneFactory.LandPlayerOnPlatform(player, checkpoint, expectedCheckpointId: 1);

            Assert.That(player.Respawn.ActiveCheckpointId, Is.EqualTo(1));

            player.Rigidbody.linearVelocity = new Vector3(0f, -8f, 0f);
            player.Root.transform.position = new Vector3(checkpointSpawn.x, -5f, 0f);
            player.Rigidbody.position = player.Root.transform.position;
            Physics.SyncTransforms();

            yield return WaitUntilRespawnedNear(player, checkpointSpawn, minY: 1.5f);

            Assert.That(player.Root.transform.position.y, Is.GreaterThan(1.5f));
            Assert.That(
                Mathf.Abs(player.Root.transform.position.x - checkpointSpawn.x),
                Is.LessThan(1.5f));
        }

        // TC-012 | EP | Final platform landing -> level completed event
        [UnityTest]
        public IEnumerator TC012_FinalPlatformLanding_RaisesLevelCompletedEvent()
        {
            PlatformPrefabCatalog catalog = BddTestSceneFactory.LoadPlatformCatalog();
            Assume.That(catalog.HasPrefabs, Is.True, "Platform catalog must contain prefabs.");

            bool levelCompleted = false;
            void OnCompleted() => levelCompleted = true;
            GameEvents.LevelCompleted += OnCompleted;

            try
            {
                ProceduralLevelGenerator generator = BddTestSceneFactory.CreateLevelGenerator(
                    jumpConfig,
                    catalog,
                    gameplayCamera,
                    seed: 131313);

                yield return WaitForGenerator(generator);

                Checkpoint finishCheckpoint = FindGeneratedCheckpoint(ProceduralLevelGenerator.PlatformCount - 1);
                Assume.That(finishCheckpoint, Is.Not.Null);

                Checkpoint startCheckpoint = FindGeneratedCheckpoint(0);
                Assume.That(startCheckpoint, Is.Not.Null);

                BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                    jumpConfig,
                    gameplayCamera,
                    startCheckpoint.Platform.GetSpawnPosition(0.45f));

                yield return WaitForInitialSnap(player);
                yield return BddTestSceneFactory.LandPlayerOnCheckpoint(
                    player,
                    finishCheckpoint,
                    expectedCheckpointId: ProceduralLevelGenerator.PlatformCount - 1);

                Assert.That(levelCompleted, Is.True);
            }
            finally
            {
                GameEvents.LevelCompleted -= OnCompleted;
            }
        }

        // TC-013 | EP | Level complete -> victory message shown
        [UnityTest]
        public IEnumerator TC013_LevelComplete_ShowsVictoryMessage()
        {
            PlatformPrefabCatalog catalog = BddTestSceneFactory.LoadPlatformCatalog();
            Assume.That(catalog.HasPrefabs, Is.True, "Platform catalog must contain prefabs.");

            var uiGo = new GameObject("LevelCompleteUI");
            BddTestSceneFactory.TrackObject(uiGo);
            LevelCompleteUI ui = uiGo.AddComponent<LevelCompleteUI>();
            yield return null;

            GameObject panel = BddTestUiAccess.GetVictoryPanel(ui);
            Assume.That(panel, Is.Not.Null);
            BddTestSceneFactory.TrackObject(panel.transform.root.gameObject);

            ProceduralLevelGenerator generator = BddTestSceneFactory.CreateLevelGenerator(
                jumpConfig,
                catalog,
                gameplayCamera,
                seed: 141414);
            yield return WaitForGenerator(generator);

            Checkpoint finishCheckpoint = FindGeneratedCheckpoint(ProceduralLevelGenerator.PlatformCount - 1);
            Assume.That(finishCheckpoint, Is.Not.Null);

            Checkpoint startCheckpoint = FindGeneratedCheckpoint(0);
            Assume.That(startCheckpoint, Is.Not.Null);

            BddTestSceneFactory.PlayerFixture player = BddTestSceneFactory.CreatePlayer(
                jumpConfig,
                gameplayCamera,
                startCheckpoint.Platform.GetSpawnPosition(0.45f));

            yield return WaitForInitialSnap(player);
            yield return BddTestSceneFactory.LandPlayerOnCheckpoint(
                player,
                finishCheckpoint,
                expectedCheckpointId: ProceduralLevelGenerator.PlatformCount - 1);

            Assert.That(panel.activeSelf, Is.True);

            Text label = panel.GetComponentInChildren<Text>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Does.Contain("Вы прошли уровень"));
        }

        // TC-014 | EP | Multiple platforms -> visual layer blends over 1.5 seconds
        [UnityTest]
        public IEnumerator TC014_VisualLayerChange_BlendsOverOnePointFiveSeconds()
        {
            const float blendDuration = 1.5f;
            VisualLayerConfig layer0 = BddTestSceneFactory.CreateLayerConfig(
                0,
                new Color(0.2f, 0.1f, 0.08f),
                new Color(0.3f, 0.2f, 0.15f));
            VisualLayerConfig layer1 = BddTestSceneFactory.CreateLayerConfig(
                1,
                new Color(0.75f, 0.85f, 0.95f),
                new Color(0.8f, 0.9f, 1f));

            VisualLayerController controller = BddTestSceneFactory.CreateVisualLayerController(
                new[] { layer0, layer1 },
                gameplayCamera,
                blendDuration);

            yield return null;

            Color fogAtStart = RenderSettings.fogColor;
            GameEvents.RaiseCheckpointTouched(10);
            yield return null;

            Assert.That(RenderSettings.fogColor, Is.Not.EqualTo(fogAtStart));

            yield return new WaitForSeconds(blendDuration + 0.1f);

            Color fogAtEnd = RenderSettings.fogColor;
            Assert.That(ColorDistance(fogAtEnd, layer1.fogColor), Is.LessThan(0.12f));
        }

        // TC-015 | EP | Level generation -> creates 11 platforms
        [UnityTest]
        public IEnumerator TC015_LevelGeneration_CreatesElevenPlatforms()
        {
            PlatformPrefabCatalog catalog = BddTestSceneFactory.LoadPlatformCatalog();
            Assume.That(catalog.HasPrefabs, Is.True, "Platform catalog must contain prefabs.");

            ProceduralLevelGenerator generator = BddTestSceneFactory.CreateLevelGenerator(
                jumpConfig,
                catalog,
                gameplayCamera,
                seed: 424242);

            yield return WaitForGenerator(generator);

            Transform generatedRoot = GameObject.Find("GeneratedLevel")?.transform;
            Assert.That(generatedRoot, Is.Not.Null);
            Assert.That(generatedRoot.childCount, Is.EqualTo(ProceduralLevelGenerator.PlatformCount));

            Checkpoint[] checkpoints = generatedRoot.GetComponentsInChildren<Checkpoint>();
            Assert.That(checkpoints.Length, Is.EqualTo(ProceduralLevelGenerator.PlatformCount));
        }

        // TC-016 | EP | After generation -> all platforms are reachable
        [UnityTest]
        public IEnumerator TC016_GeneratedRoute_AllPlatformsAreReachable()
        {
            PlatformPrefabCatalog catalog = BddTestSceneFactory.LoadPlatformCatalog();
            Assume.That(catalog.HasPrefabs, Is.True, "Platform catalog must contain prefabs.");

            ProceduralLevelGenerator generator = BddTestSceneFactory.CreateLevelGenerator(
                jumpConfig,
                catalog,
                gameplayCamera,
                seed: 515151);

            yield return WaitForGenerator(generator);

            Transform generatedRoot = GameObject.Find("GeneratedLevel")?.transform;
            Assert.That(generatedRoot, Is.Not.Null);

            Checkpoint[] checkpoints = generatedRoot.GetComponentsInChildren<Checkpoint>()
                .OrderBy(checkpoint => checkpoint.CheckpointId)
                .ToArray();

            Assert.That(checkpoints.Length, Is.EqualTo(ProceduralLevelGenerator.PlatformCount));

            for (int i = 1; i < checkpoints.Length; i++)
            {
                PlatformSpec from = PlatformSpecUtility.FromCollider(checkpoints[i - 1].Platform.PlatformCollider);
                PlatformSpec to = PlatformSpecUtility.FromCollider(checkpoints[i].Platform.PlatformCollider);

                Assert.That(
                    BddTestSceneFactory.TrajectoryReachesPlatform(
                        jumpConfig,
                        from,
                        to,
                        minImpulseFraction: 0.45f,
                        maxImpulseFraction: 0.82f),
                    Is.True,
                    $"Platforms {i - 1} -> {i} must be reachable.");
            }
        }

        static Checkpoint FindGeneratedCheckpoint(int checkpointId)
        {
            Transform generatedRoot = GameObject.Find("GeneratedLevel")?.transform;
            if (generatedRoot == null)
                return null;

            Checkpoint[] checkpoints = generatedRoot.GetComponentsInChildren<Checkpoint>();
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i].CheckpointId == checkpointId)
                    return checkpoints[i];
            }

            return null;
        }

        static float ColorDistance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        static IEnumerator WaitUntilJumpApplied(
            BddTestSceneFactory.PlayerFixture player,
            float minSqrVelocity = 0.1f,
            float timeout = 2f)
        {
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (player.Rigidbody.linearVelocity.sqrMagnitude > minSqrVelocity)
                    yield break;

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Assert.Fail($"Jump did not start within {timeout:F1}s (sqrMag <= {minSqrVelocity}).");
        }

        static IEnumerator WaitUntilRespawnedNear(
            BddTestSceneFactory.PlayerFixture player,
            Vector3 expectedSpawn,
            float minY,
            float maxHorizontalDelta = 1.5f,
            float timeout = 3f)
        {
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                Vector3 position = player.Root.transform.position;
                if (position.y > minY
                    && Mathf.Abs(position.x - expectedSpawn.x) <= maxHorizontalDelta)
                {
                    yield break;
                }

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Assert.Fail($"Player did not respawn near checkpoint within {timeout:F1}s.");
        }

        static IEnumerator WaitForInitialSnap(BddTestSceneFactory.PlayerFixture player)
        {
            const float timeout = 2f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (player.Respawn.ActiveCheckpointId >= 0
                    && player.GroundDetectorIsGrounded())
                {
                    yield break;
                }

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        static IEnumerator WaitForGenerator(ProceduralLevelGenerator generator)
        {
            const float timeout = 8f;
            float elapsed = 0f;

            while (!generator.IsReady && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(generator.IsReady, Is.True, "Procedural level generator did not finish in time.");
        }
    }

    internal static class BddTestUiAccess
    {
        public static GameObject GetVictoryPanel(LevelCompleteUI ui)
        {
            if (ui == null)
                return null;

            FieldInfo field = typeof(LevelCompleteUI).GetField(
                "panelRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(ui) as GameObject;
        }
    }

    internal static class BddPlayerFixtureExtensions
    {
        public static bool GroundDetectorIsGrounded(this BddTestSceneFactory.PlayerFixture player)
        {
            return player.Root.GetComponent<GroundDetector>().IsGrounded();
        }
    }
}
