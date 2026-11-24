using UnityEngine;

namespace HundredGame
{
    /// <summary>
    /// Maps cell indices to positions inside a square area (origin = centre, y up). Flat levels use one grid;
    /// 3D levels are shown as a set of layers ("floors") arranged in a grid of their own.
    /// </summary>
    public sealed class BoardLayout
    {
        public Vector2[] Centers;
        public float CellSize;
        public Rect[] Layers = new Rect[0];

        public static BoardLayout Create(LevelDef def, float size, float pad, float gap, float labelZone)
        {
            var l = new BoardLayout { Centers = new Vector2[def.Cells] };
            int sx = def.SX, sy = def.SY, sz = def.SZ;

            if (sz <= 1)
            {
                float cell = Mathf.Min((size - 2f * pad - gap * (sx - 1)) / sx, (size - 2f * pad - gap * (sy - 1)) / sy);
                float gw = sx * cell + (sx - 1) * gap, gh = sy * cell + (sy - 1) * gap;
                l.CellSize = cell;
                for (int i = 0; i < def.Cells; i++)
                {
                    int x = i % sx, y = i / sx;
                    l.Centers[i] = new Vector2(-gw * 0.5f + cell * 0.5f + x * (cell + gap), gh * 0.5f - cell * 0.5f - y * (cell + gap));
                }
                return l;
            }

            int cols = Mathf.CeilToInt(Mathf.Sqrt(sz)), rows = Mathf.CeilToInt(sz / (float)cols);
            const float layerGap = 20f, inner = 14f;
            float lw = (size - (cols - 1) * layerGap) / cols, lh = (size - (rows - 1) * layerGap) / rows;
            float c2 = Mathf.Min((lw - 2f * inner - gap * (sx - 1)) / sx, (lh - labelZone - 2f * inner - gap * (sy - 1)) / sy);
            float w = sx * c2 + (sx - 1) * gap, h = sy * c2 + (sy - 1) * gap;
            l.CellSize = c2;
            l.Layers = new Rect[sz];
            for (int z = 0; z < sz; z++)
            {
                int col = z % cols, row = z / cols;
                float left = col * (lw + layerGap) - size * 0.5f;
                float top = size * 0.5f - row * (lh + layerGap);
                l.Layers[z] = new Rect(left, top - lh, lw, lh);
                float gx0 = left + (lw - w) * 0.5f;
                float gy0 = top - labelZone - ((lh - labelZone) - h) * 0.5f;   // top edge of the grid
                for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    int i = (z * sy + y) * sx + x;
                    l.Centers[i] = new Vector2(gx0 + c2 * 0.5f + x * (c2 + gap), gy0 - c2 * 0.5f - y * (c2 + gap));
                }
            }
            return l;
        }
    }
}
