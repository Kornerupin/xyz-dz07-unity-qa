using EchoesOfAscent.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace EchoesOfAscent.Tests.EditMode
{
    public sealed class ProceduralLevelGenerationTests
    {
        JumpConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<JumpConfig>();
            config.minImpulse = 4f;
            config.maxImpulse = 18f;
            config.maxLaunchAngle = 75f;
            config.minLaunchElevation = 8f;
            config.trajectoryTimeStep = 0.05f;
            config.trajectoryMaxSimTime = 2.5f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void JumpReachAnalyzer_ProducesNonZeroReach()
        {
            JumpReachMetrics reach = JumpReachAnalyzer.Compute(config);

            Assert.That(reach.MaxHorizontal, Is.GreaterThan(2f));
            Assert.That(reach.MaxRise, Is.GreaterThan(1f));
        }

        [Test]
        public void JumpReachAnalyzer_ComfortableReach_IsWithinMaxReach()
        {
            JumpReachMetrics maxReach = JumpReachAnalyzer.Compute(config);
            JumpReachMetrics comfortableReach = JumpReachAnalyzer.ComputeComfortableReach(config);

            Assert.That(comfortableReach.MaxHorizontal, Is.LessThan(maxReach.MaxHorizontal));
            Assert.That(comfortableReach.MaxRise, Is.LessThan(maxReach.MaxRise));

            // The analyzer measures physical reach; camera bounds are applied by
            // ProceduralLevelGenerator. At the configured comfortable speed,
            // ideal 45-degree range is v^2 / g (about 24.4), not a fixed 8 units.
            float comfortableSpeed = Mathf.Lerp(config.minImpulse, config.maxImpulse, 0.82f);
            float idealRange = comfortableSpeed * comfortableSpeed / Mathf.Abs(Physics.gravity.y);
            Assert.That(comfortableReach.MaxHorizontal, Is.InRange(idealRange * 0.8f, idealRange));
        }

        [Test]
        public void JumpReachAnalyzer_ValidatesReachablePair()
        {
            var from = new PlatformSpec(new Vector3(0f, 0.25f, 0f), new Vector3(4f, 0.5f, 1f));
            var to = new PlatformSpec(new Vector3(2.6f, 1.75f, 0f), new Vector3(2.6f, 0.4f, 1f));

            Assert.That(
                JumpReachAnalyzer.CanJumpBetween(from, to, config, minImpulseFraction: 0.45f, maxImpulseFraction: 0.82f),
                Is.True);
        }

        [Test]
        public void PlatformMaterialPalette_NormalizeIndex_CyclesVariants()
        {
            Assert.That(PlatformMaterialPalette.NormalizeIndex(0, 3), Is.EqualTo(0));
            Assert.That(PlatformMaterialPalette.NormalizeIndex(2, 3), Is.EqualTo(2));
            Assert.That(PlatformMaterialPalette.NormalizeIndex(3, 3), Is.EqualTo(0));
            Assert.That(PlatformMaterialPalette.NormalizeIndex(-1, 3), Is.EqualTo(2));
        }

        [Test]
        public void PlatformMaterialPalette_CreatesDistinctRuntimeMaterials()
        {
            var palette = ScriptableObject.CreateInstance<PlatformMaterialPalette>();
            var greenShader = Shader.Find("EchoesOfAscent/GeneratedVertexColor");
            var purpleShader = Shader.Find("EchoesOfAscent/GeneratedVertexColor_20260619_073741");
            var fresnelShader = Shader.Find("EchoesOfAscent/GeneratedVertexColor_20260619_073725");

            Assume.That(greenShader, Is.Not.Null);
            Assume.That(purpleShader, Is.Not.Null);
            Assume.That(fresnelShader, Is.Not.Null);

            palette.ReplaceShaders(new[] { greenShader, purpleShader, fresnelShader });

            Assert.That(palette.TryCreateMaterialAtIndex(0, out Material greenMaterial), Is.True);
            Assert.That(palette.TryCreateMaterialAtIndex(1, out Material purpleMaterial), Is.True);
            Assert.That(palette.TryCreateMaterialAtIndex(2, out Material fresnelMaterial), Is.True);

            Assert.That(greenMaterial.shader, Is.SameAs(greenShader));
            Assert.That(purpleMaterial.shader, Is.SameAs(purpleShader));
            Assert.That(fresnelMaterial.shader, Is.SameAs(fresnelShader));

            Object.DestroyImmediate(greenMaterial);
            Object.DestroyImmediate(purpleMaterial);
            Object.DestroyImmediate(fresnelMaterial);
            Object.DestroyImmediate(palette);
        }
    }
}
