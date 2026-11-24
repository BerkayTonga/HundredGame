using UnityEngine;
using UnityEngine.UI;

namespace HundredGame
{
    /// <summary>Bobs the logo tiles in a wave.</summary>
    public sealed class LogoWave : MonoBehaviour
    {
        public RectTransform[] Tiles;
        public float Enabled;

        private void Update()
        {
            if (Tiles == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < Tiles.Length; i++)
            {
                if (Tiles[i] == null) continue;
                var p = Tiles[i].anchoredPosition;
                p.y = Mathf.Sin(t * 2.2f - i * 0.8f) * 14f * Enabled;
                Tiles[i].anchoredPosition = p;
                Tiles[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.6f + i) * 3.5f * Enabled);
            }
        }
    }

    public sealed class MenuPage : Page
    {
        private readonly RectTransform _logo;
        private readonly RectTransform[] _tiles = new RectTransform[3];
        private readonly LogoWave _wave;
        private readonly Text _footer;
        private readonly RectTransform _subtitle;
        private readonly RectTransform[] _buttons = new RectTransform[3];

        public MenuPage(UIManager ui, Transform parent) : base(ui, "Menu", parent)
        {
            _logo = UIKit.Anchored(Root, "Logo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(900f, 300f));
            string[] digits = { "1", "0", "0" };
            int[] colorKey = { 8, 46, 88 };
            for (int i = 0; i < 3; i++)
            {
                var holder = UIKit.Anchored(_logo, "Slot" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 290f, 0f), new Vector2(260f, 260f));
                var tile = UIKit.Fill(holder, "Tile");
                _tiles[i] = holder;
                Color c = UIKit.NumberColor(colorKey[i]);
                var glow = UIKit.Sprite_(tile, "Glow", UIKit.Glow, new Color(c.r, c.g, c.b, 0.45f));
                glow.rectTransform.offsetMin = new Vector2(-90f, -90f);
                glow.rectTransform.offsetMax = new Vector2(90f, 90f);
                UIKit.Panel(tile, "Shadow", Color.Lerp(c, Color.black, 0.55f)).rectTransform.offsetMin = new Vector2(0f, -16f);
                var shadowRt = tile.Find("Shadow") as RectTransform;
                shadowRt.offsetMax = new Vector2(0f, -16f);
                UIKit.Panel(tile, "Face", c);
                var shine = UIKit.Panel(tile, "Shine", new Color(1f, 1f, 1f, 0.2f));
                shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                shine.rectTransform.offsetMin = new Vector2(12f, 0f);
                shine.rectTransform.offsetMax = new Vector2(-12f, -12f);
                UIKit.Label(tile, "Digit", digits[i], 190, UIKit.Dark);
            }
            _wave = _logo.gameObject.AddComponent<LogoWave>();
            _wave.Tiles = _tiles;

            var sub = UIKit.Label(Root, "Subtitle", "SAYI BULMACASI", 44, UIKit.Muted);
            sub.rectTransform.SetAnchored(new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 80f));
            _subtitle = sub.rectTransform;

            var play = UIKit.PillButton(Root, "Play", "OYNA", UIKit.Teal, new Vector2(720f, 190f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), 88);
            play.onClick.AddListener(() => ui.GoTo(ui.Levels, 1));
            _buttons[0] = (RectTransform)play.transform;

            var how = UIKit.PillButton(Root, "How", "Nasıl Oynanır?", UIKit.Violet, new Vector2(420f, 130f), new Vector2(0.5f, 0.5f), new Vector2(-225f, -400f), 38);
            how.onClick.AddListener(Popup.Help);
            _buttons[1] = (RectTransform)how.transform;

            var settings = UIKit.PillButton(Root, "Settings", "Ayarlar", UIKit.Blue, new Vector2(420f, 130f), new Vector2(0.5f, 0.5f), new Vector2(225f, -400f), 42);
            settings.onClick.AddListener(Popup.Settings);
            _buttons[2] = (RectTransform)settings.transform;

            _footer = UIKit.Label(Root, "Footer", "", 34, UIKit.Muted);
            _footer.rectTransform.SetAnchored(new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(980f, 70f));
            Refresh();
        }

        public void Refresh()
        {
            int best = 0;
            for (int i = 0; i < Levels.All.Count; i++) best = Mathf.Max(best, SaveData.Best(i));
            _footer.text = SaveData.GamesPlayed == 0
                ? "Hadi başlayalım!"
                : "En yüksek skor: " + best + "   •   Oynanan oyun: " + SaveData.GamesPlayed;
        }

        public override void OnShow()
        {
            Refresh();
            _wave.Enabled = 0f;
            for (int i = 0; i < 3; i++)
            {
                _tiles[i].localScale = Vector3.zero;
                Anim.Pop(_tiles[i], 0f, 1f, 0.6f, 0.15f + i * 0.12f);
            }
            Anim.Run(0.6f, e => { if (_wave != null) _wave.Enabled = e; }, null, 0.7f, Anim.Ease.Linear);

            var subColor = _subtitle.GetComponent<Text>();
            subColor.color = new Color(UIKit.Muted.r, UIKit.Muted.g, UIKit.Muted.b, 0f);
            Anim.Run(0.5f, e => { if (subColor != null) subColor.color = new Color(UIKit.Muted.r, UIKit.Muted.g, UIKit.Muted.b, e); }, null, 0.55f);

            for (int i = 0; i < _buttons.Length; i++)
            {
                var rt = _buttons[i];
                Vector2 end = rt.anchoredPosition;
                rt.anchoredPosition = end + new Vector2(0f, -90f);
                rt.localScale = Vector3.one * 0.85f;
                Anim.Move(rt, end + new Vector2(0f, -90f), end, 0.5f, Anim.Ease.OutBack, 0.5f + i * 0.1f);
                Anim.Scale(rt, Vector3.one * 0.85f, Vector3.one, 0.4f, Anim.Ease.OutCubic, 0.5f + i * 0.1f);
            }
        }
    }

    public sealed class LevelsPage : Page
    {
        private readonly RectTransform _content;

        public LevelsPage(UIManager ui, Transform parent) : base(ui, "Levels", parent)
        {
            var back = UIKit.IconButton(Root, "Back", UIKit.Icon("back"), UIKit.CardLight, 120f, new Vector2(0f, 1f), new Vector2(90f, -100f));
            back.onClick.AddListener(() => ui.GoTo(ui.Menu, -1));

            var title = UIKit.Label(Root, "Title", "BÖLÜMLER", 78, Color.white, TextAnchor.MiddleCenter, true);
            title.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(700f, 130f));

            var hint = UIKit.Label(Root, "Hint", "Tüm bölümler açık. İstediğini seç!", 34, UIKit.Muted);
            hint.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(900f, 60f));

            UIKit.Scroll(Root, "Scroll", new Vector2(0f, 40f), new Vector2(0f, -250f), out _content);
        }

        public override void OnShow() => Refresh();

        public void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) Object.Destroy(_content.GetChild(i).gameObject);
            const float cardH = 340f, gap = 36f;
            int count = Levels.All.Count;
            for (int i = 0; i < count; i++) BuildCard(i, -20f - i * (cardH + gap), cardH);
            BuildSoon(count, -20f - count * (cardH + gap), cardH);
            _content.sizeDelta = new Vector2(0f, 20f + (count + 1) * (cardH + gap));
            _content.anchoredPosition = Vector2.zero;
        }

        private void BuildCard(int index, float y, float height)
        {
            var def = Levels.All[index];
            var rt = UIKit.Anchored(_content, "Card" + index, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(980f, height));
            var shadow = UIKit.Panel(rt, "Shadow", UIKit.H("0C0F2A"));
            shadow.rectTransform.offsetMin = new Vector2(0f, -12f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -12f);
            var face = UIKit.Panel(rt, "Face", UIKit.H("262E68"));
            face.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<PressScale>();
            int captured = index;
            btn.onClick.AddListener(() => { AudioManager.Sound(Sfx.Click, 0.7f); UI.OpenLevel(captured); });

            int best = SaveData.Best(index);

            // progress preview: the grid fills with colour as the best score grows
            float cell = 21f, g = 3f, size = def.Size * cell + (def.Size - 1) * g;
            var preview = UIKit.Anchored(face.transform, "Preview", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(size + 28f, size + 28f));
            UIKit.Panel(preview, "Bg", new Color(0f, 0f, 0.1f, 0.4f));
            for (int n = 0; n < def.Size * def.Size; n++)
            {
                int cx = n % def.Size, cy = n / def.Size;
                var c = UIKit.Anchored(preview, "c", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f + cx * (cell + g), -14f - cy * (cell + g)), new Vector2(cell, cell));
                var img = c.gameObject.AddComponent<Image>();
                img.sprite = UIKit.Rounded;
                img.type = Image.Type.Sliced;
                img.color = n < best ? UIKit.NumberColor(n + 1) : UIKit.Cell;
                img.raycastTarget = false;
            }

            float left = 46f + size + 28f + 40f;
            var info = UIKit.Rect(face.transform, "Info", Vector2.zero, Vector2.one, new Vector2(left, 0f), new Vector2(-30f, 0f));

            var tag = UIKit.Label(info, "Tag", "BÖLÜM " + (index + 1), 32, UIKit.Teal, TextAnchor.MiddleLeft);
            tag.rectTransform.SetAnchored(new Vector2(0f, 1f), new Vector2(0f, -44f), new Vector2(520f, 50f));
            tag.rectTransform.pivot = new Vector2(0f, 0.5f);
            tag.rectTransform.anchoredPosition = new Vector2(0f, -44f);

            var name = UIKit.Label(info, "Name", def.Name, 68, Color.white, TextAnchor.MiddleLeft, true);
            name.rectTransform.SetAnchored(new Vector2(0f, 1f), new Vector2(0f, -104f), new Vector2(520f, 90f));
            name.rectTransform.pivot = new Vector2(0f, 0.5f);

            var sub = UIKit.Label(info, "Sub", def.Subtitle, 30, UIKit.Muted, TextAnchor.MiddleLeft);
            sub.rectTransform.SetAnchored(new Vector2(0f, 1f), new Vector2(0f, -164f), new Vector2(520f, 44f));
            sub.rectTransform.pivot = new Vector2(0f, 0.5f);

            // stars
            int stars = SaveData.Stars(index);
            for (int s = 0; s < 3; s++)
            {
                var star = UIKit.Sprite_(info, "Star" + s, UIKit.Star, s < stars ? UIKit.Gold : new Color(1f, 1f, 1f, 0.14f));
                star.rectTransform.SetAnchored(new Vector2(0f, 0f), new Vector2(30f + s * 60f, 118f), new Vector2(54f, 54f));
            }

            // best score + bar
            var bestText = UIKit.Label(info, "Best", best > 0 ? "Rekor " + best + " / " + def.Goal : "Henüz oynanmadı", 32, Color.white, TextAnchor.MiddleLeft);
            bestText.rectTransform.SetAnchored(new Vector2(0f, 0f), new Vector2(0f, 58f), new Vector2(520f, 44f));
            bestText.rectTransform.pivot = new Vector2(0f, 0.5f);

            var track = UIKit.Anchored(info, "Track", new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 26f), new Vector2(430f, 18f));
            UIKit.Panel(track, "Bg", new Color(1f, 1f, 1f, 0.1f));
            var fill = UIKit.Panel(track, "Fill", UIKit.Teal);
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(best / (float)def.Goal), 1f);

            if (!string.IsNullOrEmpty(SaveData.Progress(index)))
            {
                var chip = UIKit.Anchored(face.transform, "Resume", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(190f, 56f));
                UIKit.Panel(chip, "Bg", UIKit.Gold);
                UIKit.Label(chip, "T", "DEVAM", 28, UIKit.Dark);
            }

            Anim.Move(rt, new Vector2(0f, y - 70f), new Vector2(0f, y), 0.5f, Anim.Ease.OutBack, 0.08f * index);
        }

        private void BuildSoon(int index, float y, float height)
        {
            var rt = UIKit.Anchored(_content, "Soon", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(980f, height));
            var face = UIKit.Panel(rt, "Face", new Color(1f, 1f, 1f, 0.05f));
            face.raycastTarget = true;
            UIKit.Label(rt, "Tag", "BÖLÜM " + (index + 1), 32, UIKit.Muted).rectTransform.SetAnchored(new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(700f, 50f));
            UIKit.Label(rt, "T", "Çok yakında", 66, new Color(1f, 1f, 1f, 0.55f)).rectTransform.SetAnchored(new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(800f, 100f));
            UIKit.Label(rt, "S", "Yeni bölümler geliyor", 32, UIKit.Muted).rectTransform.SetAnchored(new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(800f, 50f));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => UI.Toast("Yeni bölümler yakında!"));
            Anim.Move(rt, new Vector2(0f, y - 70f), new Vector2(0f, y), 0.5f, Anim.Ease.OutBack, 0.08f * index);
        }
    }
}
