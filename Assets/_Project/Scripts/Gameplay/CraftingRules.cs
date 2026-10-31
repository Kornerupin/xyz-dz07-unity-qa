namespace EchoesOfAscent.Gameplay
{
    /// <summary>Deterministic teaching mechanic exercised by the course CI gate.</summary>
    public static class CraftingRules
    {
        public const int SwordWoodCost = 2;
        public const int SwordIronCost = 3;

        public static bool TryCraftSword(ref int wood, ref int iron, out string item)
        {
            item = null;
            if (wood < SwordWoodCost || iron < SwordIronCost)
                return false;
            wood -= SwordWoodCost;
            iron -= SwordIronCost;
            item = "Sword";
            return true;
        }
    }
}
