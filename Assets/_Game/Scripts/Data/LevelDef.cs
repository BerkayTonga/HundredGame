using System.Collections.Generic;

namespace HundredGame
{
    /// <summary>One level. Add new levels to <see cref="Levels.All"/>; nothing else needs to change.</summary>
    public sealed class LevelDef
    {
        public string Name;
        public string Subtitle;
        public int Size;
        /// <summary>Cells (index = y * Size + x) that can never be used. Empty for a plain grid.</summary>
        public int[] Blocked = new int[0];
        /// <summary>Score needed for 1, 2 and 3 stars.</summary>
        public int[] StarScores = { 40, 70, 100 };

        public int PlayableCells => Size * Size - Blocked.Length;
        public int Goal => PlayableCells;
    }

    public static class Levels
    {
        public static readonly IReadOnlyList<LevelDef> All = new[]
        {
            new LevelDef { Name = "Klasik", Subtitle = "10 × 10 bitişik kare", Size = 10, StarScores = new[] { 40, 70, 100 } },
        };
    }
}
