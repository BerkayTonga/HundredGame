using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HundredGame
{
    /// <summary>A full-screen page (menu, level select, game). Built once, shown / hidden by <see cref="UIManager"/>.</summary>
    public abstract class Page
    {
        public RectTransform Root { get; private set; }
        public CanvasGroup Group { get; private set; }
        protected UIManager UI { get; private set; }

        protected Page(UIManager ui, string name, Transform parent)
        {
            UI = ui;
            Root = UIKit.Fill(parent, name);
            Group = Root.gameObject.AddComponent<CanvasGroup>();
            Root.gameObject.SetActive(false);
        }

        /// <summary>Called every time the page becomes visible.</summary>
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        /// <summary>Return true when the back button was handled.</summary>
        public virtual bool OnBack() => false;
    }

    /// <summary>Owns the canvas, safe area, background, page navigation, popups and toasts.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }

        public RectTransform Safe { get; private set; }
        public RectTransform PopupLayer { get; private set; }
        public MenuPage Menu { get; private set; }
        public LevelsPage Levels { get; private set; }
        public GamePage Game { get; private set; }

        private CanvasScaler _scaler;
        private Page _current;
        private bool _switching;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;
        private GameObject _toast;
        private Popup _popup;

        public bool PopupOpen => _popup != null;

        private void Awake()
        {
            I = this;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1080f, 1920f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            gameObject.AddComponent<GraphicRaycaster>();

            var bg = UIKit.Fill(transform, "Background");
            bg.gameObject.AddComponent<BackgroundFx>().Build();

            Safe = UIKit.Fill(transform, "SafeArea");
            var pages = UIKit.Fill(Safe, "Pages");
            Menu = new MenuPage(this, pages);
            Levels = new LevelsPage(this, pages);
            Game = new GamePage(this, pages);
            PopupLayer = UIKit.Fill(transform, "Popups");

            UpdateLayout();
        }

        private void Start() => GoTo(Menu, 0);

        private void Update()
        {
            if (Screen.width != _lastScreen.x || Screen.height != _lastScreen.y || Screen.safeArea != _lastSafe) UpdateLayout();
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
        }

        private void UpdateLayout()
        {
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _lastSafe = Screen.safeArea;
            // Taller than 9:16 -> keep the width fixed; wider (tablets) -> keep the height fixed.
            _scaler.matchWidthOrHeight = Screen.width / (float)Screen.height > 9f / 16f ? 1f : 0f;
            var sa = Screen.safeArea;
            Safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            Safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ navigation
        public void GoTo(Page next, int direction)
        {
            if (_switching || next == _current) return;
            _switching = true;
            var prev = _current;
            _current = next;

            next.Root.gameObject.SetActive(true);
            next.Group.alpha = 0f;
            next.Group.interactable = false;
            next.OnShow();

            float slide = 90f * direction;
            if (prev != null)
            {
                prev.Group.interactable = false;
                Anim.Run(0.22f, e =>
                {
                    prev.Group.alpha = 1f - e;
                    prev.Root.anchoredPosition = new Vector2(-slide * e, 0f);
                }, () =>
                {
                    prev.Root.gameObject.SetActive(false);
                    prev.Root.anchoredPosition = Vector2.zero;
                    prev.OnHide();
                });
            }

            Anim.Run(0.3f, e =>
            {
                next.Group.alpha = e;
                next.Root.anchoredPosition = new Vector2(slide * (1f - e), 0f);
            }, () =>
            {
                next.Group.interactable = true;
                next.Root.anchoredPosition = Vector2.zero;
                _switching = false;
            }, prev != null ? 0.1f : 0f);
        }

        public void Back()
        {
            if (_popup != null) { _popup.Close(); return; }
            if (_switching) return;
            if (_current != null && _current.OnBack()) return;
            if (_current == Game) GoTo(Levels, -1);
            else if (_current == Levels) GoTo(Menu, -1);
            else Application.Quit();
        }

        public void OpenLevel(int index)
        {
            Game.Prepare(index);
            GoTo(Game, 1);
        }

        // ------------------------------------------------------------------ popups & toasts
        public void RegisterPopup(Popup p) => _popup = p;
        public void UnregisterPopup(Popup p) { if (_popup == p) _popup = null; }

        public void Toast(string message)
        {
            if (_toast != null) Destroy(_toast);
            var rt = UIKit.Anchored(PopupLayer, "Toast", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(880f, 110f));
            _toast = rt.gameObject;
            var cg = _toast.AddComponent<CanvasGroup>();
            UIKit.Panel(rt, "Bg", new Color(0.04f, 0.05f, 0.14f, 0.95f));
            UIKit.Label(rt, "Text", message, 34, Color.white);
            Anim.Run(0.25f, e => { if (cg != null) { cg.alpha = e; rt.anchoredPosition = new Vector2(0f, 330f + 30f * e); } }, null, 0f, Anim.Ease.OutCubic);
            var go = _toast;
            Anim.After(2.2f, () =>
            {
                if (go == null) return;
                Anim.Fade(cg, 1f, 0f, 0.3f, 0f, () => { if (go != null) Destroy(go); });
            });
        }
    }

    /// <summary>Animated gradient backdrop with slow drifting glows and faint floating number tiles.</summary>
    public sealed class BackgroundFx : MonoBehaviour
    {
        private struct Tile { public RectTransform Rt; public float Speed, Spin, Sway, Phase; }

        private readonly List<Tile> _tiles = new List<Tile>();
        private RectTransform _glowA, _glowB;
        private RectTransform _rect;

        public void Build()
        {
            _rect = (RectTransform)transform;
            var grad = gameObject.AddComponent<Image>();
            grad.sprite = UIKit.Gradient(UIKit.BgTop, UIKit.BgBottom);
            grad.raycastTarget = false;

            _glowA = Glow("GlowA", new Color(UIKit.Violet.r, UIKit.Violet.g, UIKit.Violet.b, 0.38f), 1300f);
            _glowB = Glow("GlowB", new Color(UIKit.Teal.r, UIKit.Teal.g, UIKit.Teal.b, 0.16f), 1100f);

            for (int i = 0; i < 16; i++)
            {
                float size = Random.Range(70f, 130f);
                var rt = UIKit.Anchored(transform, "Tile", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                Color c = UIKit.NumberColor(Random.Range(1, 100));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = UIKit.Rounded;
                img.type = Image.Type.Sliced;
                img.color = new Color(c.r, c.g, c.b, Random.Range(0.05f, 0.11f));
                img.raycastTarget = false;
                var t = UIKit.Label(rt, "N", Random.Range(1, 100).ToString(), (int)(size * 0.45f), new Color(1f, 1f, 1f, 0.12f));
                t.raycastTarget = false;
                rt.anchoredPosition = new Vector2(Random.Range(-540f, 540f), Random.Range(-1000f, 1000f));
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
                _tiles.Add(new Tile { Rt = rt, Speed = Random.Range(18f, 50f), Spin = Random.Range(-6f, 6f), Sway = Random.Range(10f, 40f), Phase = Random.value * 6.28f });
            }
        }

        private RectTransform Glow(string name, Color color, float size)
        {
            var rt = UIKit.Anchored(transform, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIKit.Glow;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float w = _rect.rect.width, h = _rect.rect.height;
            _glowA.anchoredPosition = new Vector2(Mathf.Sin(t * 0.13f) * w * 0.3f, h * 0.28f + Mathf.Cos(t * 0.17f) * 120f);
            _glowB.anchoredPosition = new Vector2(Mathf.Cos(t * 0.11f) * w * 0.35f, -h * 0.3f + Mathf.Sin(t * 0.14f) * 140f);

            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                var p = tile.Rt.anchoredPosition;
                p.y += tile.Speed * Time.unscaledDeltaTime;
                p.x += Mathf.Sin(t * 0.4f + tile.Phase) * tile.Sway * Time.unscaledDeltaTime;
                if (p.y > h * 0.5f + 150f) { p.y = -h * 0.5f - 150f; p.x = Random.Range(-w * 0.5f, w * 0.5f); }
                tile.Rt.anchoredPosition = p;
                tile.Rt.Rotate(0f, 0f, tile.Spin * Time.unscaledDeltaTime);
            }
        }
    }
}
