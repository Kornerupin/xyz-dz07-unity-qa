using EchoesOfAscent.Gameplay;

// Executes the actual pure C# gameplay class. This is not a Unity test run.
int expectedWood = args.Length == 0 ? 3 : int.Parse(args[0]);
int failures = 0;
void Check(string label, int initialWood, int initialIron, bool expectedCrafted,
           int remainingWood, int remainingIron, string expectedItem)
{
    int wood = initialWood, iron = initialIron;
    bool crafted = CraftingRules.TryCraftSword(ref wood, ref iron, out string item);
    bool passed = crafted == expectedCrafted && wood == remainingWood && iron == remainingIron && item == expectedItem;
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")}: {label}; crafted={crafted}; wood={wood}; iron={iron}; item={item ?? "null"}");
    if (!passed) failures++;
}
Check("sufficient resources", 5, 4, true, expectedWood, 1, "Sword");
Check("insufficient wood leaves inventory untouched", 1, 4, false, 1, 4, null);
Check("insufficient iron leaves inventory untouched", 5, 2, false, 5, 2, null);
Check("exact cost", 2, 3, true, 0, 0, "Sword");
return failures == 0 ? 0 : 1;
