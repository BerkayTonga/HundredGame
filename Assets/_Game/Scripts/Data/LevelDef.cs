using System.Collections.Generic;

namespace HundredGame
{
    /// <summary>
    /// One level / variation. Add new ones to <see cref="Levels.All"/>; the menu, saves, stars and board UI adapt automatically.
    /// Cells are indexed (z * SY + y) * SX + x. A level is won when every playable cell is filled.
    /// </summary>
    public sealed class LevelDef
    {
        public string Name;
        public string Subtitle;
        /// <summary>One or two short lines shown in the how-to-play popup.</summary>
        public string Rule;

        public int SX = 10, SY = 10, SZ = 1;
        /// <summary>Legal jumps from the last number: (dx, dy, dz).</summary>
        public int[][] Offsets;
        public bool WrapX, WrapY, WrapZ;
        /// <summary>Cells that can never be used.</summary>
        public int[] Blocked = new int[0];
        /// <summary>Score needed for 1, 2 and 3 stars; defaults to 40% / 70% / 100% of the goal.</summary>
        public int[] StarScores;

        public int Cells => SX * SY * SZ;
        public int Goal => Cells - Blocked.Length;
        public bool Is3D => SZ > 1;

        public int[] Stars => StarScores ?? new[] { (int)(Goal * 0.4f), (int)(Goal * 0.7f), Goal };
    }

    public static class Offsets
    {
        /// <summary>Straight jumps of <paramref name="straight"/> cells and diagonal jumps of <paramref name="diagonal"/> cells (in every pair of axes).</summary>
        public static int[][] Build(int straight, int diagonal, bool threeD, bool spaceDiagonal = false)
        {
            var list = new List<int[]>();
            int dims = threeD ? 3 : 2;
            for (int a = 0; a < dims; a++)
            for (int s = -1; s <= 1; s += 2)
            {
                var v = new int[3];
                v[a] = s * straight;
                list.Add(v);
            }
            for (int a = 0; a < dims; a++)
            for (int b = a + 1; b < dims; b++)
            for (int sa = -1; sa <= 1; sa += 2)
            for (int sb = -1; sb <= 1; sb += 2)
            {
                var v = new int[3];
                v[a] = sa * diagonal;
                v[b] = sb * diagonal;
                list.Add(v);
            }
            if (threeD && spaceDiagonal)
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    list.Add(new[] { sx * diagonal, sy * diagonal, sz * diagonal });
            return list.ToArray();
        }

        public static int[][] Knight()
        {
            var list = new List<int[]>();
            int[] s = { -1, 1 };
            foreach (int a in s)
            foreach (int b in s)
            {
                list.Add(new[] { a, 2 * b, 0 });
                list.Add(new[] { 2 * a, b, 0 });
            }
            return list.ToArray();
        }
    }

    public static class Levels
    {
        public static readonly IReadOnlyList<LevelDef> All = new[]
        {
            new LevelDef
            {
                Name = "Klasik", Subtitle = "10 × 10 düz tahta",
                Rule = "Yatay / dikey: 3 kare ileri\nÇapraz: 2 kare ileri",
                SX = 10, SY = 10, Offsets = Offsets.Build(3, 2, false),
            },
            new LevelDef
            {
                Name = "Elmas", Subtitle = "Elmas biçimli tahta",
                Rule = "Klasik kurallar. Köşeler kapalı, tahta elmas şeklinde.",
                SX = 12, SY = 12, Blocked = Diamond(12, 8), Offsets = Offsets.Build(3, 2, false),
            },
            new LevelDef
            {
                Name = "Sarmal", Subtitle = "Kenarlar birleşik",
                Rule = "Klasik kurallar. Kenardan taşan hamle karşı taraftan devam eder.",
                SX = 10, SY = 10, WrapX = true, WrapY = true, Offsets = Offsets.Build(3, 2, false),
            },
            new LevelDef
            {
                Name = "At", Subtitle = "Satranç atı hamleleri",
                Rule = "Satranç atı gibi: 2 kare düz, 1 kare yana.",
                SX = 10, SY = 10, Offsets = Offsets.Knight(),
            },
            new LevelDef
            {
                Name = "Halka", Subtitle = "Ortası boş, 96 kare",
                Rule = "Klasik kurallar. Ortadaki boşluğu aşıp atlayabilirsin.",
                SX = 11, SY = 11, Blocked = CenterHole(11, 5), Offsets = Offsets.Build(3, 2, false),
            },
            new LevelDef
            {
                Name = "Adalar", Subtitle = "4 ada, 100 kare",
                Rule = "Klasik kurallar. Adalar arasındaki suyu 3 kare düz atlayarak geç.",
                SX = 12, SY = 12, Blocked = Channels(12, 5, 2), Offsets = Offsets.Build(3, 2, false),
            },
        };

        /// <summary>Cells of a square hole in the middle of an n × n board.</summary>
        public static int[] CenterHole(int n, int hole)
        {
            var list = new List<int>();
            int a = (n - hole) / 2;
            for (int y = a; y < a + hole; y++)
            for (int x = a; x < a + hole; x++) list.Add(y * n + x);
            return list.ToArray();
        }

        /// <summary>Corner cells removed so that an n × n board becomes a diamond.</summary>
        public static int[] Diamond(int n, int radius)
        {
            var list = new List<int>();
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - (n - 1) / 2f, dy = y - (n - 1) / 2f;
                if (System.Math.Abs(dx) + System.Math.Abs(dy) > radius - 0.5f) list.Add(y * n + x);
            }
            return list.ToArray();
        }

        /// <summary>A vertical and a horizontal channel that split the board into four islands.</summary>
        public static int[] Channels(int n, int start, int width)
        {
            var list = new List<int>();
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                if ((x >= start && x < start + width) || (y >= start && y < start + width)) list.Add(y * n + x);
            return list.ToArray();
        }
    }
}
