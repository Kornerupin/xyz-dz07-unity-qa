using System.Collections;
using EchoesOfAscent.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoesOfAscent.Tests.PlayMode
{
    /// <summary>Automatic crafting gate; mutation changes only the expected wood count.</summary>
    [Category("DemoAI")]
    public sealed class CraftTest
    {
        [UnityTest]
        public IEnumerator Test_CraftSword_ConsumesResources()
        {
            int wood = 5;
            int iron = 4;
            const int expectedRemainingWood = 4; // COURSE_MUTATION_POINT
            bool crafted = CraftingRules.TryCraftSword(ref wood, ref iron, out string item);
            Debug.Log($"[QA TEST] CraftSword crafted={crafted}; wood={wood}; iron={iron}; item={item}");
            Assert.That(crafted, Is.True);
            Assert.That(item, Is.EqualTo("Sword"));
            Assert.That(wood, Is.EqualTo(expectedRemainingWood), "CraftSword remaining wood must equal initial 5 minus cost 2.");
            Assert.That(iron, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CraftSword_WithoutResources_DoesNotConsumeInventory()
        {
            int wood = 1;
            int iron = 4;
            bool crafted = CraftingRules.TryCraftSword(ref wood, ref iron, out string item);
            Assert.That(crafted, Is.False);
            Assert.That(item, Is.Null);
            Assert.That(wood, Is.EqualTo(1));
            Assert.That(iron, Is.EqualTo(4));
            yield return null;
        }
    }
}
