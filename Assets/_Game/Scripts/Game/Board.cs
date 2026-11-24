using System.Collections.Generic;

namespace HundredGame
{
    /// <summary>
    /// Pure game rules (no Unity types). The first number goes anywhere; each next number must be placed on an
    /// empty cell reachable from the last one by one of the level's offsets.
    /// </summary>
    public sealed class Board
    {
        public readonly LevelDef Def;
        public readonly int SX, SY, SZ;
        private readonly int[] _cells;           // 0 empty, -1 blocked, >0 number
        private readonly int[][] _neighbors;     // legal targets per cell (ignoring occupancy)
        private readonly List<int> _history = new List<int>();

        public Board(LevelDef def)
        {
            Def = def;
            SX = def.SX; SY = def.SY; SZ = def.SZ;
            _cells = new int[def.Cells];
            _neighbors = BuildNeighbors(def);
            Reset();
        }

        public int Count => _history.Count;
        public int Next => _history.Count + 1;
        public int Current => _history.Count == 0 ? -1 : _history[_history.Count - 1];
        public bool Won => Count >= Def.Goal;
        public bool Stuck => Count > 0 && !Won && MoveCount() == 0;

        public int CellCount => _cells.Length;
        public int Value(int index) => _cells[index];
        public int X(int i) => i % SX;
        public int Y(int i) => (i / SX) % SY;
        public int Z(int i) => i / (SX * SY);

        private static int[][] BuildNeighbors(LevelDef def)
        {
            int total = def.Cells;
            var result = new int[total][];
            var set = new HashSet<int>();
            var blocked = new HashSet<int>(def.Blocked);
            for (int i = 0; i < total; i++)
            {
                set.Clear();
                int x = i % def.SX, y = (i / def.SX) % def.SY, z = i / (def.SX * def.SY);
                foreach (var o in def.Offsets)
                {
                    int nx = x + o[0], ny = y + o[1], nz = z + o[2];
                    if (!Fit(ref nx, def.SX, def.WrapX) || !Fit(ref ny, def.SY, def.WrapY) || !Fit(ref nz, def.SZ, def.WrapZ)) continue;
                    int idx = (nz * def.SY + ny) * def.SX + nx;
                    if (idx != i && !blocked.Contains(idx)) set.Add(idx);
                }
                var arr = new int[set.Count];
                set.CopyTo(arr);
                result[i] = arr;
            }
            return result;
        }

        private static bool Fit(ref int v, int size, bool wrap)
        {
            if (v >= 0 && v < size) return true;
            if (!wrap) return false;
            v = ((v % size) + size) % size;
            return true;
        }

        public void Reset()
        {
            _history.Clear();
            for (int i = 0; i < _cells.Length; i++) _cells[i] = 0;
            foreach (int b in Def.Blocked) _cells[b] = -1;
        }

        public bool CanPlace(int index)
        {
            if (_cells[index] != 0) return false;
            if (_history.Count == 0) return true;
            foreach (int n in _neighbors[Current]) if (n == index) return true;
            return false;
        }

        public bool Place(int index)
        {
            if (!CanPlace(index)) return false;
            _history.Add(index);
            _cells[index] = _history.Count;
            return true;
        }

        /// <summary>Removes the last number. Returns its cell index, or -1 when nothing to undo.</summary>
        public int Undo()
        {
            if (_history.Count == 0) return -1;
            int idx = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            _cells[idx] = 0;
            return idx;
        }

        /// <summary>All legal target cells for the next number.</summary>
        public void Moves(List<int> result)
        {
            result.Clear();
            if (_history.Count == 0)
            {
                for (int i = 0; i < _cells.Length; i++) if (_cells[i] == 0) result.Add(i);
                return;
            }
            foreach (int n in _neighbors[Current]) if (_cells[n] == 0) result.Add(n);
        }

        public int MoveCount()
        {
            if (_history.Count == 0) return Def.Goal;
            int c = 0;
            foreach (int n in _neighbors[Current]) if (_cells[n] == 0) c++;
            return c;
        }

        /// <summary>Number of empty cells reachable from <paramref name="index"/> (used by bots).</summary>
        public int OnwardMoves(int index)
        {
            int c = 0;
            foreach (int n in _neighbors[index]) if (_cells[n] == 0 && n != index) c++;
            return c;
        }

        /// <summary>Warnsdorff-style suggestion (used by the test bots): fewest, but still some, follow-up moves.</summary>
        public int Hint(System.Random rng = null)
        {
            var moves = new List<int>();
            Moves(moves);
            if (moves.Count == 0) return -1;
            int best = moves[0], bestScore = int.MaxValue;
            foreach (int m in moves)
            {
                int onward = OnwardMoves(m);
                int score = (onward == 0 ? 1000 : onward) * 10 + (rng != null ? rng.Next(10) : 0);
                if (_history.Count == 0) score = rng != null ? rng.Next(1000) : m;
                if (score < bestScore) { bestScore = score; best = m; }
            }
            return best;
        }

        public string Serialize() => string.Join(",", _history);

        public void Restore(string data)
        {
            Reset();
            if (string.IsNullOrEmpty(data)) return;
            foreach (var part in data.Split(','))
            {
                if (int.TryParse(part, out int idx) && idx >= 0 && idx < _cells.Length && !Place(idx)) { Reset(); return; }
            }
        }
    }
}
