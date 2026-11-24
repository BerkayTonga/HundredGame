using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HundredGame
{
    public sealed class CellView : MonoBehaviour, IPointerClickHandler
    {
        public int Index;
        public GamePage Owner;
        public RectTransform Rect;
        public Image Bg, Ring, Border;
        public Text Num;
        public void OnPointerClick(PointerEventData e) => Owner.OnCell(Index);
    }

    /// <summary>Forwards Update to the (non-MonoBehaviour) page.</summary>
    public sealed class Ticker : MonoBehaviour
    {
        public System.Action OnTick;
        private void Update() => OnTick?.Invoke();
    }

    public sealed class GamePage : Page
    {
        private const float BoardPixels = 1040f;
        private const float Pad = 12f, Gap = 6f;

        private Board _board;
        public Board Board => _board;
        private int _level;
        private CellView[] _cells;
        private float[] _phase;
        private bool[] _valid;
        private readonly List<int> _moves = new List<int>();
        private BoardLayout _layout;

        private readonly RectTransform _boardRt, _cellsRoot, _fx;
        private readonly Text _title, _score, _best;

        private bool _ended, _intro;
        private int _bestAtStart, _prevCurrent = -1;

        public GamePage(UIManager ui, Transform parent) : base(ui, "Game", parent)
        {
            var back = UIKit.IconButton(Root, "Back", UIKit.Icon("back"), UIKit.Slate, 120f, new Vector2(0f, 1f), new Vector2(90f, -100f));
            back.onClick.AddListener(() => ui.GoTo(ui.Levels, -1));
            var gear = UIKit.IconButton(Root, "Gear", UIKit.Icon("gear"), UIKit.Slate, 120f, new Vector2(1f, 1f), new Vector2(-90f, -100f));
            gear.onClick.AddListener(Popup.Settings);
            var help = UIKit.IconButton(Root, "Help", UIKit.Icon("help"), UIKit.Slate, 120f, new Vector2(1f, 1f), new Vector2(-230f, -100f));
            help.onClick.AddListener(() => Popup.Help(_board != null ? _board.Def : null));
            Object.Destroy(help.transform.Find("Face/Icon").gameObject);
            UIKit.Label(help.transform.Find("Face"), "Q", "?", 72, Color.white);

            _title = UIKit.Label(Root, "Title", "", 52, UIKit.Ink, TextAnchor.MiddleCenter);
            _title.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(-20f, -100f), new Vector2(440f, 100f));

            Stat("Score", "SKOR", -260f, out _score);
            Stat("Best", "REKOR", 260f, out _best);

            _boardRt = UIKit.Anchored(Root, "Board", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(BoardPixels, BoardPixels));
            var edge = UIKit.Panel(_boardRt, "Edge", UIKit.BoardEdge);
            edge.rectTransform.offsetMin = new Vector2(-4f, -4f);
            edge.rectTransform.offsetMax = new Vector2(4f, 4f);
            UIKit.Panel(_boardRt, "Bg", UIKit.BoardBg);
            _cellsRoot = UIKit.Fill(_boardRt, "Cells");
            _fx = UIKit.Fill(_boardRt, "Fx");

            var undo = UIKit.ActionButton(Root, "Undo", UIKit.Icon("undo"), "Geri Al", UIKit.Blue, new Vector2(340f, 150f), new Vector2(0.5f, 0f), new Vector2(-200f, 170f));
            undo.onClick.AddListener(Undo);
            var restart = UIKit.ActionButton(Root, "Restart", UIKit.Icon("restart"), "Yeniden", UIKit.Pink, new Vector2(340f, 150f), new Vector2(0.5f, 0f), new Vector2(200f, 170f));
            restart.onClick.AddListener(Restart);

            Root.gameObject.AddComponent<Ticker>().OnTick = Tick;
        }

        private void Stat(string name, string label, float x, out Text value)
        {
            var rt = UIKit.Anchored(Root, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -270f), new Vector2(480f, 130f));
            UIKit.Panel(rt, "Shadow", UIKit.H("DCCBB4")).rectTransform.offsetMin = new Vector2(0f, -8f);
            UIKit.Panel(rt, "Face", UIKit.Card);
            var l = UIKit.Label(rt, "Label", label, 28, UIKit.Muted, TextAnchor.MiddleLeft);
            l.rectTransform.SetAnchored(new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(240f, 60f));
            value = UIKit.Label(rt, "Value", "0", 76, UIKit.Ink, TextAnchor.MiddleRight);
            value.rectTransform.SetAnchored(new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(240f, 110f));
        }

        // ------------------------------------------------------------------ lifecycle
        /// <summary>Loads (or resumes) a level. Call before showing the page.</summary>
        public void Prepare(int level)
        {
            _level = level;
            var def = Levels.All[level];
            _board = new Board(def);
            _title.text = "BÖLÜM " + (level + 1);
            BuildGrid();

            _board.Restore(SaveData.Progress(level));
            _bestAtStart = SaveData.Best(level);
            _ended = false;
            _prevCurrent = _board.Current;
            for (int i = 0; i < _cells.Length; i++) RefreshCell(i);
            RecomputeValid();
            UpdateStats(false);
            _intro = true;
        }

        public override void OnShow()
        {
            if (!_intro) return;
            _intro = false;
            for (int i = 0; i < _cells.Length; i++)
            {
                var rt = _cells[i].Rect;
                rt.localScale = Vector3.zero;
                Anim.Pop(rt, 0f, 1f, 0.45f, 0.15f + _phase[i] * 0.022f);
            }

            // show the rules the first time a level is opened
            if (!SaveData.SeenRules(_level))
            {
                SaveData.MarkRulesSeen(_level);
                var def = _board.Def;
                Anim.After(0.9f, () => { if (!UI.PopupOpen) Popup.Help(def); });
            }
        }

        private void BuildGrid()
        {
            for (int i = _cellsRoot.childCount - 1; i >= 0; i--) Object.Destroy(_cellsRoot.GetChild(i).gameObject);
            var def = _board.Def;
            _layout = BoardLayout.Create(def, BoardPixels, Pad, Gap, 40f);
            int total = _board.CellCount;
            _cells = new CellView[total];
            _valid = new bool[total];
            _phase = new float[total];

            for (int z = 0; z < _layout.Layers.Length; z++)
            {
                var r = _layout.Layers[z];
                var panel = UIKit.Anchored(_cellsRoot, "Layer" + z, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(r.xMin, r.yMin), new Vector2(r.width, r.height));
                UIKit.Panel(panel, "Bg", UIKit.H("DCCDB8"));
                var label = UIKit.Label(panel, "Label", "KAT " + (z + 1), 26, UIKit.Muted, TextAnchor.MiddleLeft);
                label.rectTransform.anchorMin = new Vector2(0f, 1f);
                label.rectTransform.anchorMax = new Vector2(1f, 1f);
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
                label.rectTransform.sizeDelta = new Vector2(-40f, 40f);
                label.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            }

            float cell = _layout.CellSize;
            int fontSize = Mathf.RoundToInt(cell * 0.52f);
            for (int i = 0; i < total; i++)
            {
                var rt = UIKit.Anchored(_cellsRoot, "Cell" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), _layout.Centers[i], new Vector2(cell, cell));
                var ring = UIKit.Panel(rt, "Ring", Color.clear);
                ring.rectTransform.offsetMin = new Vector2(-8f, -8f);
                ring.rectTransform.offsetMax = new Vector2(8f, 8f);
                var border = UIKit.Panel(rt, "Border", BorderColor);
                border.rectTransform.offsetMin = new Vector2(-3f, -3f);
                border.rectTransform.offsetMax = new Vector2(3f, 3f);
                var bg = UIKit.Panel(rt, "Bg", UIKit.Cell);
                bg.raycastTarget = true;
                var num = UIKit.Label(rt, "Num", "", fontSize, UIKit.Dark);
                var view = rt.gameObject.AddComponent<CellView>();
                view.Index = i;
                view.Owner = this;
                view.Rect = rt;
                view.Bg = bg;
                view.Ring = ring;
                view.Border = border;
                view.Num = num;
                _cells[i] = view;
                _phase[i] = _board.X(i) + _board.Y(i) + _board.Z(i) * 3f;
                if (_board.Value(i) < 0)
                {
                    bg.color = Color.clear;
                    border.color = Color.clear;
                    bg.raycastTarget = false;
                }
            }
        }

        private static readonly Color BorderColor = UIKit.H("B7A387");

        private static void ClearMark(CellView c)
        {
            c.Ring.color = Color.clear;
            c.Border.color = BorderColor;
            c.Border.rectTransform.offsetMin = new Vector2(-3f, -3f);
            c.Border.rectTransform.offsetMax = new Vector2(3f, 3f);
        }

        private Vector2 CellPos(int i) => _layout.Centers[i];

        // ------------------------------------------------------------------ rendering
        private void RefreshCell(int i)
        {
            var c = _cells[i];
            int v = _board.Value(i);
            if (v < 0) return;
            if (v > 0)
            {
                c.Bg.color = UIKit.NumberColor(v, _board.Def.Goal);
                c.Num.text = v.ToString();
                c.Num.fontSize = Mathf.RoundToInt(_layout.CellSize * (v >= 100 ? 0.42f : 0.52f));
                c.Num.color = UIKit.Dark;
            }
            else
            {
                c.Bg.color = UIKit.Cell;
                c.Num.text = "";
            }
            ClearMark(c);
        }

        private void RecomputeValid()
        {
            foreach (int i in _moves)
            {
                _valid[i] = false;
                RefreshCell(i);
            }
            _board.Moves(_moves);
            foreach (int i in _moves) _valid[i] = true;
        }

        private void UpdateStats(bool animate)
        {
            int score = _board.Count;
            _score.text = score.ToString();
            _best.text = Mathf.Max(SaveData.Best(_level), score).ToString();
            if (animate) Anim.Pop(_score.transform, 0.8f, 1f, 0.3f);
        }

        private void Tick()
        {
            if (_cells == null || _board == null) return;
            float t = Time.unscaledTime;
            bool first = _board.Count == 0;

            // legal cells breathe softly (no text, just a gentle glow)
            foreach (int i in _moves)
            {
                float k = first
                    ? 0.05f + 0.04f * Mathf.Sin(t * 2f - _phase[i] * 0.5f)
                    : 0.2f + 0.09f * Mathf.Sin(t * 3f + i * 0.7f);
                _cells[i].Bg.color = Color.Lerp(UIKit.Cell, UIKit.Teal, k);
            }

            int cur = _board.Current;
            if (cur >= 0)
            {
                // the last number pulses a dark outline drawn above its neighbours
                var cv = _cells[cur];
                cv.Border.color = Color.Lerp(BorderColor, UIKit.Ink, 0.5f + 0.5f * Mathf.Sin(t * 4f));
                cv.Border.rectTransform.offsetMin = new Vector2(-5f, -5f);
                cv.Border.rectTransform.offsetMax = new Vector2(5f, 5f);
                cv.Rect.SetAsLastSibling();
            }
        }

        // ------------------------------------------------------------------ input
        public void OnCell(int i)
        {
            if (_board == null || !Group.interactable) return;
            if (_board.Value(i) < 0 || _board.Won) return;

            if (_ended) { Shake(i); return; }
            if (_board.Place(i)) Placed(i);
            else Invalid(i);
        }

        private void Placed(int i)
        {
            int n = _board.Count;
            if (_prevCurrent >= 0) ClearMark(_cells[_prevCurrent]);
            _prevCurrent = i;

            RefreshCell(i);
            var c = _cells[i];
            float cell = _layout.CellSize;
            Color col = UIKit.NumberColor(n, _board.Def.Goal);
            Anim.Scale(c.Rect, Vector3.one * 0.35f, Vector3.one, 0.38f, Anim.Ease.OutBack);
            UIFx.Ripple(_fx, CellPos(i), new Color(col.r, col.g, col.b, 0.45f), cell * 0.8f, cell * 2.4f, 0.45f);
            UIFx.Burst(_fx, CellPos(i), col, 6, 240f, 14f, 0.45f);

            AudioManager.Sound(Sfx.Place, 0.9f, AudioManager.NotePitch(n), 0.02f);
            Haptics.Light();

            SaveData.TotalPlaced++;
            if (n == 1) SaveData.GamesPlayed++;
            if (n > SaveData.Best(_level)) SaveData.SetBest(_level, n);
            SaveData.SetProgress(_level, _board.Serialize());
            SaveData.Save();

            RecomputeValid();
            UpdateStats(true);

            if (_board.Won) Win();
            else if (_board.Stuck) Lose();
        }

        private void Invalid(int i)
        {
            Shake(i);
            AudioManager.Sound(Sfx.Error, 0.8f, Random.Range(0.95f, 1.05f), 0.08f);
            Haptics.Medium();
        }

        private void Shake(int i)
        {
            var c = _cells[i];
            Vector2 home = c.Rect.anchoredPosition;
            Color from = UIKit.Pink;
            Anim.Run(0.32f, e =>
            {
                if (c == null) return;
                c.Rect.anchoredPosition = home + new Vector2(Mathf.Sin(e * 38f) * (1f - e) * 11f, 0f);
                if (!_valid[i] && _board.Value(i) >= 0)
                {
                    Color baseCol = _board.Value(i) > 0 ? UIKit.NumberColor(_board.Value(i), _board.Def.Goal) : UIKit.Cell;
                    c.Bg.color = Color.Lerp(from, baseCol, e);
                }
            }, () => { if (c != null) { c.Rect.anchoredPosition = home; if (!_valid[i]) RefreshCell(i); } }, 0f, Anim.Ease.Linear);
        }

        // ------------------------------------------------------------------ actions
        public void Undo()
        {
            if (_board == null || _board.Count == 0) return;
            int idx = _board.Undo();
            _ended = false;
            var c = _cells[idx];
            RefreshCell(idx);
            ClearMark(c);
            Anim.Scale(c.Rect, Vector3.one * 1.25f, Vector3.one, 0.25f, Anim.Ease.OutCubic);
            UIFx.Ripple(_fx, CellPos(idx), new Color(UIKit.Ink.r, UIKit.Ink.g, UIKit.Ink.b, 0.2f), _layout.CellSize * 1.6f, _layout.CellSize * 0.6f, 0.3f);
            AudioManager.Sound(Sfx.Undo, 0.8f);
            Haptics.Light();

            _prevCurrent = _board.Current;
            SaveData.SetProgress(_level, _board.Serialize());
            SaveData.Save();
            RecomputeValid();
            UpdateStats(false);
        }

        private void Restart()
        {
            if (_board == null || _board.Count == 0) return;
            if (_board.Count >= 5 && !_ended) Popup.Confirm("Yeniden başla?", "Mevcut oyunun silinecek.", "Başla", DoRestart);
            else DoRestart();
        }

        private void DoRestart()
        {
            _board.Reset();
            _ended = false;
            _prevCurrent = -1;
            _bestAtStart = SaveData.Best(_level);
            SaveData.SetProgress(_level, "");
            SaveData.Save();
            for (int i = 0; i < _cells.Length; i++)
            {
                RefreshCell(i);
                Anim.Pop(_cells[i].Rect, 0.2f, 1f, 0.4f, _phase[i] * 0.02f);
            }
            RecomputeValid();
            UpdateStats(false);
            AudioManager.Sound(Sfx.Undo, 0.8f, 1.2f);
        }

        // ------------------------------------------------------------------ end of game
        private void Win()
        {
            _ended = true;
            SaveData.SetProgress(_level, "");
            SaveData.Save();
            AudioManager.Sound(Sfx.Win, 1f);
            Haptics.Heavy();
            var layer = (RectTransform)UI.PopupLayer;
            UIFx.Confetti(UI.PopupLayer, 90, layer.rect.width, layer.rect.height);
            Anim.After(1.4f, () => ShowResult(true));
        }

        private void Lose()
        {
            _ended = true;
            SaveData.SetProgress(_level, "");
            SaveData.Save();
            AudioManager.Sound(Sfx.Lose, 0.9f);
            Haptics.Heavy();
            Vector2 home = _boardRt.anchoredPosition;
            Anim.Run(0.4f, e => { if (_boardRt != null) _boardRt.anchoredPosition = home + new Vector2(Mathf.Sin(e * 40f) * (1f - e) * 14f, 0f); },
                () => { if (_boardRt != null) _boardRt.anchoredPosition = home; }, 0f, Anim.Ease.Linear);
            Anim.After(0.9f, () => ShowResult(false));
        }

        private void ShowResult(bool win)
        {
            if (!_ended || UI.PopupOpen) return;
            int score = _board.Count;
            bool record = score > _bestAtStart;
            var p = new Popup(900f, 1160f);
            var c = p.Card;

            var title = UIKit.Label(c, "Title", win ? "TEBRİKLER!" : "HAMLE KALMADI", win ? 80 : 66, win ? UIKit.Gold : UIKit.Ink, TextAnchor.MiddleCenter);
            title.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(840f, 130f));
            Anim.Pop(title.transform, 0.5f, 1f, 0.5f, 0.1f);

            var num = UIKit.Label(c, "Score", "0", 250, UIKit.Ink, TextAnchor.MiddleCenter);
            num.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(800f, 270f));
            Anim.Run(0.9f, e => { if (num != null) num.text = Mathf.RoundToInt(score * e).ToString(); }, null, 0.25f, Anim.Ease.OutCubic);

            var thresholds = _board.Def.Stars;
            int stars = 0;
            for (int s = 0; s < thresholds.Length; s++) if (score >= thresholds[s]) stars = s + 1;
            for (int s = 0; s < 3; s++)
            {
                var star = UIKit.Sprite_(c, "Star" + s, UIKit.Star, s < stars ? UIKit.Gold : UIKit.H("E6DBCB"));
                star.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2((s - 1) * 150f, -620f - (s == 1 ? -20f : 0f)), new Vector2(s == 1 ? 140f : 120f, s == 1 ? 140f : 120f));
                if (s < stars)
                {
                    int sIndex = s;
                    star.rectTransform.localScale = Vector3.zero;
                    float delay = 0.7f + s * 0.28f;
                    Anim.Pop(star.rectTransform, 0f, 1f, 0.5f, delay);
                    Anim.After(delay, () => AudioManager.Sound(Sfx.Star, 0.9f, 1f + sIndex * 0.19f));
                }
            }

            if (record)
            {
                var chip = UIKit.Anchored(c, "Record", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -770f), new Vector2(440f, 80f));
                UIKit.Panel(chip, "Bg", UIKit.Gold);
                UIKit.Label(chip, "T", "YENİ REKOR!", 44, UIKit.Dark);
                chip.localScale = Vector3.zero;
                Anim.Pop(chip, 0f, 1f, 0.5f, 1.2f);
            }
            else
            {
                var info = UIKit.Label(c, "Best", "Rekor: " + SaveData.Best(_level), 44, UIKit.Muted);
                info.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -770f), new Vector2(700f, 80f));
            }

            var again = UIKit.PillButton(c, "Again", "Tekrar Oyna", UIKit.Teal, new Vector2(660f, 130f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), 54);
            again.onClick.AddListener(() => { p.Close(); DoRestart(); });

            if (win)
            {
                var levels = UIKit.PillButton(c, "Levels", "Bölümler", UIKit.Slate, new Vector2(660f, 120f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), 46);
                levels.onClick.AddListener(() => { p.Close(); UI.GoTo(UI.Levels, -1); });
            }
            else
            {
                // nothing left to place: restart, or step back and try another route
                var undo = UIKit.PillButton(c, "Undo", "Geri Al", UIKit.Blue, new Vector2(320f, 120f), new Vector2(0.5f, 0f), new Vector2(-170f, 100f), 46);
                undo.onClick.AddListener(() => { p.Close(); Undo(); });
                var levels = UIKit.PillButton(c, "Levels", "Bölümler", UIKit.Slate, new Vector2(320f, 120f), new Vector2(0.5f, 0f), new Vector2(170f, 100f), 40);
                levels.onClick.AddListener(() => { p.Close(); UI.GoTo(UI.Levels, -1); });
            }

            if (win)
            {
                var layer = (RectTransform)UI.PopupLayer;
                UIFx.Confetti(UI.PopupLayer, 70, layer.rect.width, layer.rect.height);
            }
            if (record) UI.Menu.Refresh();
        }
    }
}
