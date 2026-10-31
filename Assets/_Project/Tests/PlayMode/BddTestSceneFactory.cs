using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EchoesOfAscent.Core;
using EchoesOfAscent.Gameplay;
using EchoesOfAscent.Input;
using EchoesOfAscent.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EchoesOfAscent.Tests.PlayMode
{
    /// <summary>
    /// Builds isolated runtime scenes for BDD integration tests.
    /// </summary>
    internal static class BddTestSceneFactory
    {
        internal const float MaxReferencePixels = 280f;
        internal const float MinSwipePixels = 25f;
        internal const int TestViewportWidth = 1920;
        internal const int TestViewportHeight = 1080;

        internal static readonly List<GameObject> CreatedObjects = new List<GameObject>();
        static readonly List<ScriptableObject> CreatedAssets = new List<ScriptableObject>();

        internal static void Reset()
        {
            Time.timeScale = 1f;
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            for (int i = CreatedObjects.Count - 1; i >= 0; i--)
            {
                if (CreatedObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(CreatedObjects[i]);
            }

            CreatedObjects.Clear();

            for (int i = CreatedAssets.Count - 1; i >= 0; i--)
            {
                if (CreatedAssets[i] != null)
                    UnityEngine.Object.DestroyImmediate(CreatedAssets[i]);
            }

            CreatedAssets.Clear();
        }

        internal static JumpConfig CreateJumpConfig()
        {
#if UNITY_EDITOR
            JumpConfig asset = AssetDatabase.LoadAssetAtPath<JumpConfig>("Assets/_Project/Data/DefaultJumpConfig.asset");
            if (asset != null)
                return asset;
#endif

            JumpConfig config = ScriptableObject.CreateInstance<JumpConfig>();
            config.minImpulse = 4f;
            config.maxImpulse = 18f;
            config.maxLaunchAngle = 75f;
            config.minLaunchElevation = 8f;
            config.trajectorySteps = 40;
            config.trajectoryTimeStep = 0.05f;
            config.trajectoryMaxSimTime = 2.5f;
            config.coyoteTime = 0.12f;
            config.minSwipePixels = MinSwipePixels;
            Track(config);
            return config;
        }

        internal static PlatformPrefabCatalog LoadPlatformCatalog()
        {
#if UNITY_EDITOR
            PlatformPrefabCatalog catalog =
                AssetDatabase.LoadAssetAtPath<PlatformPrefabCatalog>("Assets/_Project/Data/PlatformPrefabCatalog.asset");
            if (catalog != null && catalog.HasPrefabs)
                return catalog;
#endif

            return CreateRuntimePlatformCatalog();
        }

        internal static Camera CreateGameplayCamera(Vector3 position)
        {
            var cameraGo = new GameObject("GameplayCamera");
            Track(cameraGo);
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.identity;
            camera.pixelRect = new Rect(0f, 0f, TestViewportWidth, TestViewportHeight);
            return camera;
        }

        internal static Vector2 GetCameraScreenCenter(Camera camera)
        {
            Rect pixelRect = camera != null
                ? camera.pixelRect
                : new Rect(0f, 0f, TestViewportWidth, TestViewportHeight);

            return new Vector2(
                pixelRect.x + pixelRect.width * 0.5f,
                pixelRect.y + pixelRect.height * 0.5f);
        }

        internal static PlatformFixture CreatePlatform(
            int checkpointId,
            Vector3 center,
            Vector3 scale,
            bool isFinish = false)
        {
            var platformGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformGo.name = isFinish ? "Platform_Finish" : $"Platform_{checkpointId}";
            platformGo.tag = "Ground";
            platformGo.transform.position = center;
            platformGo.transform.localScale = scale;
            Track(platformGo);

            var spawnPoint = platformGo.AddComponent<PlatformSpawnPoint>();
            spawnPoint.BindPlatformCollider(platformGo.GetComponent<Collider>());

            var checkpoint = platformGo.AddComponent<Checkpoint>();
            checkpoint.Configure(checkpointId);

            return new PlatformFixture(platformGo, spawnPoint, checkpoint);
        }

        internal static PlayerFixture CreatePlayer(
            JumpConfig jumpConfig,
            Camera gameplayCamera,
            Vector3 spawnPosition,
            int defaultCheckpointId = 0)
        {
            var playerGo = new GameObject("Player");
            playerGo.SetActive(false);
            playerGo.transform.position = spawnPosition;
            Track(playerGo);

            Rigidbody rb = playerGo.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SphereCollider bodyCollider = playerGo.AddComponent<SphereCollider>();
            bodyCollider.radius = 0.45f;

            playerGo.AddComponent<GroundDetector>();
            SwipeInputProvider swipeInput = playerGo.AddComponent<SwipeInputProvider>();

            var trajectoryGo = new GameObject("TrajectoryPreview");
            trajectoryGo.transform.SetParent(playerGo.transform, false);
            Track(trajectoryGo);
            trajectoryGo.AddComponent<LineRenderer>();
            TrajectoryPreviewRenderer trajectoryPreview = trajectoryGo.AddComponent<TrajectoryPreviewRenderer>();

            PlayerSpiritController spirit = playerGo.AddComponent<PlayerSpiritController>();
            SetField(spirit, "jumpConfig", jumpConfig);
            SetField(spirit, "swipeInput", swipeInput);
            SetField(spirit, "trajectoryPreview", trajectoryPreview);
            SetField(spirit, "gameplayCamera", gameplayCamera);

            PlayerRespawnController respawn = playerGo.AddComponent<PlayerRespawnController>();
            SetField(respawn, "defaultCheckpointId", defaultCheckpointId);
            SetField(respawn, "loadSavedCheckpointOnStart", false);

            playerGo.SetActive(true);
            Physics.SyncTransforms();
            return new PlayerFixture(playerGo, rb, swipeInput, spirit, respawn);
        }

        internal static ProceduralLevelGenerator CreateLevelGenerator(
            JumpConfig jumpConfig,
            PlatformPrefabCatalog catalog,
            Camera gameplayCamera,
            int seed)
        {
            var generatorGo = new GameObject("ProceduralLevelGenerator");
            Track(generatorGo);

            ProceduralLevelGenerator generator = generatorGo.AddComponent<ProceduralLevelGenerator>();
            SetField(generator, "jumpConfig", jumpConfig);
            SetField(generator, "platformPrefabCatalog", catalog);
            SetField(generator, "randomSeed", seed);
            SetField(generator, "useSharedServerSeed", false);
            SetField(generator, "deactivateScenePlatforms", false);
            SetField(generator, "gameplayCamera", gameplayCamera);
            return generator;
        }

        internal static VisualLayerController CreateVisualLayerController(
            VisualLayerConfig[] layerConfigs,
            Camera targetCamera,
            float blendDuration = 1.5f)
        {
            var controllerGo = new GameObject("VisualLayerController");
            Track(controllerGo);

            Volume volume = controllerGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Track(volume.profile);

            VisualLayerController controller = controllerGo.AddComponent<VisualLayerController>();
            SetField(controller, "layerConfigs", layerConfigs);
            SetField(controller, "blendDuration", blendDuration);
            SetField(controller, "localPostProcessVolume", volume);
            SetField(controller, "targetCamera", targetCamera);
            SetField(controller, "enableFogOnAwake", true);
            SetField(controller, "loadSavedLayerOnStart", false);
            SetField(controller, "maxCheckpointId", 10);
            return controller;
        }

        internal static VisualLayerConfig CreateLayerConfig(int layerIndex, Color fog, Color ambient)
        {
            VisualLayerConfig config = ScriptableObject.CreateInstance<VisualLayerConfig>();
            config.layerIndex = layerIndex;
            config.fogColor = fog;
            config.ambientColor = ambient;
            Track(config);
            return config;
        }

        internal static SwipeData BuildSwipe(Vector2 start, Vector2 end, float duration = 0.2f)
        {
            return new SwipeData(start, end, MaxReferencePixels, duration);
        }

        internal static SwipeData BuildSwipeWithMagnitude(
            float magnitude,
            float elevationDegrees,
            bool toRight = true,
            float duration = 0.2f)
        {
            float horizontal = magnitude * Mathf.Cos(elevationDegrees * Mathf.Deg2Rad);
            float vertical = magnitude * Mathf.Sin(elevationDegrees * Mathf.Deg2Rad);
            float sign = toRight ? -1f : 1f;
            Vector2 start = new Vector2(400f, 320f);
            Vector2 end = start + new Vector2(sign * horizontal, -vertical);
            return BuildSwipe(start, end, duration);
        }

        internal static SwipeData BuildSwipeForLaunch(
            JumpCalculator calculator,
            JumpConfig config,
            Camera camera,
            float targetElevationDegrees,
            float swipeMagnitudePixels,
            bool launchToRight = true,
            float duration = 0.2f,
            float maxElevationError = 0.35f)
        {
            Vector2 screenCenter = GetCameraScreenCenter(camera);
            SwipeData bestSwipe = BuildSwipe(screenCenter, screenCenter + Vector2.up * swipeMagnitudePixels);
            float bestError = float.MaxValue;

            for (int angle = 0; angle < 360; angle += 5)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 delta = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * swipeMagnitudePixels;
                SwipeData swipe = BuildSwipe(screenCenter, screenCenter + delta);
                if (!swipe.IsValid(MinSwipePixels))
                    continue;

                Vector3 velocity = calculator.ScreenDeltaToLaunchVelocity(swipe, config, camera);
                if (velocity.sqrMagnitude < 0.01f)
                    continue;

                bool directionMatches = launchToRight ? velocity.x > 0.01f : velocity.x < -0.01f;
                if (!directionMatches)
                    continue;

                float error = Mathf.Abs(GetLaunchElevationDegrees(velocity) - targetElevationDegrees);
                if (error < bestError)
                {
                    bestError = error;
                    bestSwipe = swipe;
                }
            }

            if (bestError > maxElevationError)
                throw new InvalidOperationException(
                    $"Could not build swipe within {maxElevationError:F2}° of target elevation {targetElevationDegrees:F2}°.");

            return bestSwipe;
        }

        internal static SwipeData BuildSwipeForMaxImpulse(
            JumpCalculator calculator,
            JumpConfig config,
            Camera camera,
            float duration = 0.2f)
        {
            return BuildSwipeForLaunch(
                calculator,
                config,
                camera,
                targetElevationDegrees: 45f,
                swipeMagnitudePixels: MaxReferencePixels,
                launchToRight: true,
                duration);
        }

        internal static float GetLaunchElevationDegrees(Vector3 velocity)
        {
            return Mathf.Atan2(velocity.y, Mathf.Abs(velocity.x)) * Mathf.Rad2Deg;
        }

        internal static IEnumerator LandPlayerOnPlatform(
            PlayerFixture player,
            PlatformFixture platform,
            int expectedCheckpointId)
        {
            Vector3 spawn = platform.SpawnPoint.GetSpawnPosition(0.45f);
            Vector3 approach = spawn + Vector3.up * 1.5f;

            player.Rigidbody.linearVelocity = Vector3.zero;
            player.Rigidbody.angularVelocity = Vector3.zero;
            player.Rigidbody.position = approach;
            player.Root.transform.position = approach;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 20; i++)
            {
                player.Rigidbody.MovePosition(spawn);
                player.Root.transform.position = spawn;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();

                if (player.Respawn.ActiveCheckpointId == expectedCheckpointId)
                    yield break;
            }
        }

        internal static IEnumerator LandPlayerOnCheckpoint(
            PlayerFixture player,
            Checkpoint checkpoint,
            int expectedCheckpointId)
        {
            PlatformSpawnPoint spawnPoint = checkpoint.Platform;
            if (spawnPoint == null)
                yield break;

            Vector3 spawn = spawnPoint.GetSpawnPosition(0.45f);
            Vector3 approach = spawn + Vector3.up * 1.5f;

            player.Rigidbody.linearVelocity = Vector3.zero;
            player.Rigidbody.angularVelocity = Vector3.zero;
            player.Rigidbody.position = approach;
            player.Root.transform.position = approach;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 20; i++)
            {
                player.Rigidbody.MovePosition(spawn);
                player.Root.transform.position = spawn;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();

                if (player.Respawn.ActiveCheckpointId == expectedCheckpointId)
                    yield break;
            }
        }

        internal static bool TrajectoryReachesPlatform(
            JumpConfig config,
            PlatformSpec from,
            PlatformSpec to,
            float playerHalfWidth = 0.45f,
            float minImpulseFraction = 0.45f,
            float maxImpulseFraction = 0.82f)
        {
            var calculator = new JumpCalculator();
            float launchY = from.TopY + playerHalfWidth;
            float[] launchXs =
            {
                from.CenterX,
                from.MinX + playerHalfWidth + 0.05f,
                from.MaxX - playerHalfWidth - 0.05f
            };

            const int impulseSteps = 8;
            const int angleSteps = 12;

            foreach (float launchX in launchXs)
            {
                for (int impulseIndex = 0; impulseIndex <= impulseSteps; impulseIndex++)
                {
                    float impulseT = impulseIndex / (float)impulseSteps;
                    float impulse = Mathf.Lerp(
                        Mathf.Lerp(config.minImpulse, config.maxImpulse, minImpulseFraction),
                        Mathf.Lerp(config.minImpulse, config.maxImpulse, maxImpulseFraction),
                        impulseT);

                    for (int angleIndex = 0; angleIndex <= angleSteps; angleIndex++)
                    {
                        float elevation = Mathf.Lerp(
                            config.minLaunchElevation,
                            config.maxLaunchAngle,
                            angleIndex / (float)angleSteps);
                        float elevationRad = elevation * Mathf.Deg2Rad;

                        foreach (float sign in new[] { -1f, 1f })
                        {
                            var velocity = new Vector3(
                                sign * Mathf.Cos(elevationRad) * impulse,
                                Mathf.Sin(elevationRad) * impulse,
                                0f);
                            Vector3[] arc = calculator.SampleTrajectory(
                                new Vector3(launchX, launchY, 0f),
                                velocity,
                                config);

                            if (ArcLandsOnPlatform(arc, to, playerHalfWidth))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        static bool ArcLandsOnPlatform(Vector3[] arc, PlatformSpec target, float playerHalfWidth)
        {
            if (arc == null || arc.Length < 2)
                return false;

            for (int i = 1; i < arc.Length; i++)
            {
                Vector3 point = arc[i];
                Vector3 previous = arc[i - 1];
                bool withinX = point.x >= target.MinX + playerHalfWidth
                            && point.x <= target.MaxX - playerHalfWidth;
                bool nearTop = point.y <= target.TopY + 0.2f && point.y >= target.TopY - 0.35f;
                bool descending = point.y <= previous.y;

                if (withinX && nearTop && descending)
                    return true;
            }

            return false;
        }

        static PlatformPrefabCatalog CreateRuntimePlatformCatalog()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.SetActive(false);
            Track(prefab);

            var catalog = ScriptableObject.CreateInstance<PlatformPrefabCatalog>();
            var entries = new List<PlatformPrefabCatalog.Entry>
            {
                new PlatformPrefabCatalog.Entry
                {
                    prefab = prefab,
                    baseSize = new Vector2(1f, 1f),
                    paletteShaderIndex = 0
                }
            };

            SetField(catalog, "entries", entries);
            Track(catalog);
            return catalog;
        }

        internal static void TrackObject(GameObject gameObject)
        {
            if (gameObject != null)
                CreatedObjects.Add(gameObject);
        }

        static void Track(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
                CreatedObjects.Add(go);
            else if (obj is Component component)
                CreatedObjects.Add(component.gameObject);
            else if (obj is ScriptableObject asset)
                CreatedAssets.Add(asset);
        }

        static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            field.SetValue(target, value);
        }

        internal readonly struct PlatformFixture
        {
            public PlatformFixture(GameObject root, PlatformSpawnPoint spawnPoint, Checkpoint checkpoint)
            {
                Root = root;
                SpawnPoint = spawnPoint;
                Checkpoint = checkpoint;
            }

            public GameObject Root { get; }
            public PlatformSpawnPoint SpawnPoint { get; }
            public Checkpoint Checkpoint { get; }
        }

        internal readonly struct PlayerFixture
        {
            public PlayerFixture(
                GameObject root,
                Rigidbody rigidbody,
                SwipeInputProvider swipeInput,
                PlayerSpiritController spirit,
                PlayerRespawnController respawn)
            {
                Root = root;
                Rigidbody = rigidbody;
                SwipeInput = swipeInput;
                Spirit = spirit;
                Respawn = respawn;
            }

            public GameObject Root { get; }
            public Rigidbody Rigidbody { get; }
            public SwipeInputProvider SwipeInput { get; }
            public PlayerSpiritController Spirit { get; }
            public PlayerRespawnController Respawn { get; }
        }
    }
}
