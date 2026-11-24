using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HundredGame
{
    /// <summary>Modal card with a dimmed backdrop. Content goes into <see cref="Card"/>.</summary>
    public sealed class Popup
    {
        public RectTransform Card { get; private set; }
        public Action OnClosed;

        private readonly GameObject _root;
        private readonly CanvasGroup _group;
        private bool _closing;

        public Popup(float width, float height, bool dismissOnBackdrop = true)
        {
            var ui = UIManager.I;
            ui.RegisterPopup(this);
            var rt = UIKit.Fill(ui.PopupLayer, "Popup");
            _root = rt.gameObject;
            _group = _root.AddComponent<CanvasGroup>();

            var dim = UIKit.Panel(rt, "Dim", new Color(0.02f, 0.03f, 0.1f, 0.78f), false);
            dim.raycastTarget = true;
            if (dismissOnBackdrop) dim.gameObject.AddComponent<BackdropClick>().Owner = this;

            var border = UIKit.Anchored(rt, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.sprite = UIKit.Rounded;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = UIKit.CardLight;
            borderImg.raycastTarget = true;
            Card = UIKit.Rect(border, "Inner", Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            var innerRounded = UIKit.Panel(Card, "Bg", UIKit.H("162A52"));
            innerRounded.rectTransform.SetAsFirstSibling();

            _group.alpha = 0f;
            Anim.Run(0.25f, e => { if (_group != null) _group.alpha = e; });
            Anim.Run(0.4f, e => { if (border != null) border.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, e); }, null, 0f, Anim.Ease.OutBack);
        }

        public void Close()
        {
            if (_closing) return;
            _closing = true;
            UIManager.I?.UnregisterPopup(this);
            Anim.Run(0.18f, e => { if (_group != null) _group.alpha = 1f - e; }, () =>
            {
                if (_root != null) UnityEngine.Object.Destroy(_root);
                OnClosed?.Invoke();
            });
        }

        private sealed class BackdropClick : MonoBehaviour, IPointerClickHandler
        {
            public Popup Owner;
            public void OnPointerClick(PointerEventData e) => Owner?.Close();
        }

        // ------------------------------------------------------------------ ready-made popups
        public static void Settings()
        {
            var p = new Popup(900f, 1180f);
            var c = p.Card;
            UIKit.Label(c, "Title", "AYARLAR", 72, Color.white, TextAnchor.MiddleCenter, true).rectTransform
                .SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(800f, 120f));

            float y = -270f;
            Row(c, "Ses Efektleri", SaveData.SfxOn, v => { SaveData.SfxOn = v; SaveData.Save(); }, ref y);
            Row(c, "Müzik", SaveData.MusicOn, v => { SaveData.MusicOn = v; SaveData.Save(); AudioManager.Instance?.ApplySettings(); }, ref y);
            Row(c, "Titreşim", SaveData.VibrationOn, v => { SaveData.VibrationOn = v; SaveData.Save(); if (v) Haptics.Medium(); }, ref y);

            var reset = UIKit.PillButton(c, "Reset", "İlerlemeyi Sıfırla", UIKit.H("7A3C4F"), new Vector2(620f, 120f), new Vector2(0.5f, 1f), new Vector2(0f, y - 40f), 40);
            bool armed = false;
            var resetLabel = reset.GetComponentInChildren<Text>();
            reset.onClick.AddListener(() =>
            {
                if (!armed)
                {
                    armed = true;
                    resetLabel.text = "Emin misin? Tekrar dokun";
                    resetLabel.fontSize = 34;
                    return;
                }
                SaveData.ResetAll();
                UIManager.I.Levels.Refresh();
                UIManager.I.Menu.Refresh();
                UIManager.I.Toast("İlerleme sıfırlandı");
                p.Close();
            });

            var close = UIKit.PillButton(c, "Close", "Tamam", UIKit.Teal, new Vector2(560f, 130f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), 54);
            close.onClick.AddListener(p.Close);
        }

        private static void Row(RectTransform c, string label, bool value, Action<bool> changed, ref float y)
        {
            var row = UIKit.Anchored(c, "Row", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 120f));
            UIKit.Panel(row, "Bg", new Color(1f, 1f, 1f, 0.06f));
            var t = UIKit.Label(row, "Label", label, 46, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.offsetMin = new Vector2(40f, 0f);
            Switch.Create(row, "Switch", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), value, changed);
            y -= 150f;
        }

        public static void Help(LevelDef def = null)
        {
            def = def ?? Levels.All[0];
            var p = new Popup(940f, 1420f);
            var c = p.Card;
            UIKit.Label(c, "Title", "NASIL OYNANIR?", 68, Color.white, TextAnchor.MiddleCenter, true).rectTransform
                .SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(860f, 120f));

            var body = UIKit.Label(c, "Body",
                "Sayıları 1'den başlayarak sırayla yerleştir.",
                38, UIKit.Muted, TextAnchor.UpperCenter);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(820f, 130f));
            body.rectTransform.pivot = new Vector2(0.5f, 1f);

            // mini diagram: current cell in the middle, legal targets glowing
            const int grid = 7;
            float cell = 76f, gap = 8f;
            float size = grid * cell + (grid - 1) * gap;
            var board = UIKit.Anchored(c, "Diagram", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(size + 36f, size + 36f));
            UIKit.Panel(board, "Bg", new Color(0f, 0f, 0.1f, 0.35f));
            for (int gy = 0; gy < grid; gy++)
            for (int gx = 0; gx < grid; gx++)
            {
                int dx = gx - 3, dy = 3 - gy;
                bool center = dx == 0 && dy == 0, legal = false;
                foreach (var o in def.Offsets) if (o[2] == 0 && o[0] == dx && o[1] == dy) legal = true;
                var rt = UIKit.Anchored(board, "C", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(18f + gx * (cell + gap), -18f - gy * (cell + gap)), new Vector2(cell, cell));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = UIKit.Rounded;
                img.type = Image.Type.Sliced;
                img.color = center ? UIKit.NumberColor(12, 100) : legal ? new Color(UIKit.Teal.r, UIKit.Teal.g, UIKit.Teal.b, 0.85f) : UIKit.Cell;
                img.raycastTarget = false;
                if (center) UIKit.Label(rt, "N", "5", 40, UIKit.Dark);
                else if (legal) UIKit.Label(rt, "N", "6", 36, UIKit.Dark);
                if (legal) Anim.Pop(rt, 0.6f, 1f, 0.5f, 0.3f + 0.05f * (gx + gy));
            }

            var rules = UIKit.Label(c, "Rules",
                def.Rule,
                40, Color.white, TextAnchor.UpperCenter);
            rules.horizontalOverflow = HorizontalWrapMode.Wrap;
            rules.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -360f - size - 70f), new Vector2(840f, 140f));
            rules.rectTransform.pivot = new Vector2(0.5f, 1f);

            var close = UIKit.PillButton(c, "Close", "Anladım", UIKit.Teal, new Vector2(560f, 130f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), 54);
            close.onClick.AddListener(p.Close);
        }

        public static void Confirm(string title, string message, string yes, Action onYes)
        {
            var p = new Popup(860f, 640f);
            var c = p.Card;
            UIKit.Label(c, "Title", title, 64, Color.white, TextAnchor.MiddleCenter, true).rectTransform
                .SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(780f, 120f));
            var msg = UIKit.Label(c, "Msg", message, 40, UIKit.Muted);
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            msg.rectTransform.SetAnchored(new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(740f, 200f));

            var no = UIKit.PillButton(c, "No", "Vazgeç", UIKit.H("3A5080"), new Vector2(340f, 120f), new Vector2(0.5f, 0f), new Vector2(-190f, 110f), 46);
            no.onClick.AddListener(p.Close);
            var ok = UIKit.PillButton(c, "Yes", yes, UIKit.Pink, new Vector2(340f, 120f), new Vector2(0.5f, 0f), new Vector2(190f, 110f), 46);
            ok.onClick.AddListener(() => { p.Close(); onYes?.Invoke(); });
        }
    }

    internal static class RectExt
    {
        public static void SetAnchored(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
