using System.Collections.Generic;

namespace HundredGame
{
    /// <summary>
    /// Pure game rules (no Unity types). The first number goes anywhere; every next number must be placed
    /// 3 cells away horizontally / vertically (two empty cells in between) or 2 cells away diagonally
    /// (one empty cell in between), on an empty cell.
    /// </summary>
    public sealed class Board
    {
        private static readonly int[] OffX = { 3, -3, 0, 0, 2, 2, -2, -2 };
        private static readonly int[] OffY = { 0, 0, 3, -3, 2, -2, 2, -2 };

        public readonly LevelDef Def;
        public readonly int Size;
        private readonly int[] _cells;           // 0 empty, -1 blocked, >0 number
        private readonly List<int> _history = new List<int>();

        public Board(LevelDef def)
        {
            Def = def;
            Size = def.Size;
            _cells = new int[Size * Size];
            Reset();
        }

        public int Count => _history.Count;
        public int Next => _history.Count + 1;
        public int Current => _history.Count == 0 ? -1 : _history[_history.Count - 1];
        public bool Won => Count >= Def.Goal;
        public bool Stuck => Count > 0 && !Won && MoveCount() == 0;

        public int Index(int x, int y) => y * Size + x;
        public int Value(int index) => _cells[index];
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

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
            int cur = Current;
            int dx = index % Size - cur % Size, dy = index / Size - cur / Size;
            for (int i = 0; i < 8; i++) if (OffX[i] == dx && OffY[i] == dy) return true;
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
            int cx = Current % Size, cy = Current / Size;
            for (int i = 0; i < 8; i++)
            {
                int x = cx + OffX[i], y = cy + OffY[i];
                if (InBounds(x, y) && _cells[Index(x, y)] == 0) result.Add(Index(x, y));
            }
        }

        public int MoveCount()
        {
            if (_history.Count == 0) return _cells.Length - Def.Blocked.Length;
            int cx = Current % Size, cy = Current / Size, n = 0;
            for (int i = 0; i < 8; i++)
            {
                int x = cx + OffX[i], y = cy + OffY[i];
                if (InBounds(x, y) && _cells[Index(x, y)] == 0) n++;
            }
            return n;
        }

        private int OnwardMoves(int index)
        {
            int cx = index % Size, cy = index / Size, n = 0;
            for (int i = 0; i < 8; i++)
            {
                int x = cx + OffX[i], y = cy + OffY[i];
                if (InBounds(x, y) && _cells[Index(x, y)] == 0 && Index(x, y) != index) n++;
            }
            return n;
        }

        /// <summary>Warnsdorff-style suggestion: the move that leaves the fewest (but still some) follow-ups.</summary>
        public int Hint()
        {
            var moves = new List<int>();
            Moves(moves);
            if (moves.Count == 0) return -1;
            int best = moves[0], bestScore = int.MaxValue;
            foreach (int m in moves)
            {
                int onward = OnwardMoves(m);
                int score = onward == 0 ? 100 : onward;
                if (_history.Count == 0) score = Corner(m) ? 0 : 1;
                if (score < bestScore) { bestScore = score; best = m; }
            }
            return best;
        }

        private bool Corner(int index)
        {
            int x = index % Size, y = index / Size;
            return (x == 0 || x == Size - 1) && (y == 0 || y == Size - 1);
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
