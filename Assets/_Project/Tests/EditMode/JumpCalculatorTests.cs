using EchoesOfAscent.Core;
using EchoesOfAscent.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace EchoesOfAscent.Tests.EditMode
{
    public sealed class JumpCalculatorTests
    {
        JumpCalculator calculator;
        JumpConfig config;

        [SetUp]
        public void SetUp()
        {
            calculator = new JumpCalculator();
            config = ScriptableObject.CreateInstance<JumpConfig>();
            config.minImpulse = 4f;
            config.maxImpulse = 20f;
            config.maxLaunchAngle = 75f;
            config.minLaunchElevation = 8f;
            config.trajectorySteps = 10;
            config.trajectoryTimeStep = 0.1f;
            config.trajectoryMaxSimTime = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ScreenDeltaToLaunchVelocity_PullDownRight_LaunchesLeft()
        {
            var camera = CreateGameplayCamera();
            // A slingshot launch travels opposite to the downward pull gesture.
            var swipe = new SwipeData(new Vector2(150f, 300f), new Vector2(400f, 280f), 300f, 0.2f);
            Vector3 velocity = calculator.ScreenDeltaToLaunchVelocity(swipe, config, camera);

            Assert.That(velocity.x, Is.LessThan(0f));
            Assert.That(velocity.y, Is.GreaterThan(0f));

            Object.DestroyImmediate(camera.gameObject);
        }

        [Test]
        public void ScreenDeltaToLaunchVelocity_PullDownLeft_LaunchesRight()
        {
            var camera = CreateGameplayCamera();
            var swipe = new SwipeData(new Vector2(400f, 300f), new Vector2(150f, 280f), 300f, 0.2f);
            Vector3 velocity = calculator.ScreenDeltaToLaunchVelocity(swipe, config, camera);

            Assert.That(velocity.x, Is.GreaterThan(0f));
            Assert.That(velocity.y, Is.GreaterThan(0f));

            Object.DestroyImmediate(camera.gameObject);
        }

        [Test]
        public void ClampLaunchDirection_BlocksDownwardLaunch()
        {
            Vector3 downRight = JumpCalculator.ClampLaunchDirection(new Vector3(1f, -1f, 0f), config);
            Vector3 downLeft = JumpCalculator.ClampLaunchDirection(new Vector3(-1f, -0.5f, 0f), config);

            Assert.That(downRight, Is.EqualTo(Vector3.zero));
            Assert.That(downLeft, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ClampLaunchDirection_AllowsSymmetricElevation()
        {
            Vector3 right = JumpCalculator.ClampLaunchDirection(new Vector3(1f, 1f, 0f), config);
            Vector3 left = JumpCalculator.ClampLaunchDirection(new Vector3(-1f, 1f, 0f), config);

            Assert.That(right.y, Is.EqualTo(left.y).Within(0.001f));
            Assert.That(Mathf.Sign(right.x), Is.EqualTo(1f));
            Assert.That(Mathf.Sign(left.x), Is.EqualTo(-1f));
        }

        static Camera CreateGameplayCamera()
        {
            var cameraGo = new GameObject("TestCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 3f, -10f);
            camera.transform.rotation = Quaternion.identity;
            return camera;
        }
    }
}
