using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HundredGame
{
    /// <summary>Generated sprites, icons, palette and builders for the code-built uGUI (no art assets needed).</summary>
    public static class UIKit
    {
        // ------------------------------------------------------------------ palette
        public static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        public static readonly Color BgTop = H("FFF8EC");
        public static readonly Color BgBottom = H("FDE9D6");
        public static readonly Color Card = H("FFFFFF");
        public static readonly Color CardLight = H("F2E6D6");
        public static readonly Color Cell = H("FBF6EE");
        public static readonly Color BoardBg = H("E7DAC7");
        public static readonly Color BoardEdge = H("D3C2AA");
        public static readonly Color Teal = H("12C6A0");
        public static readonly Color Pink = H("FF6B7F");
        public static readonly Color Gold = H("FFB41F");
        public static readonly Color Violet = H("4A90FF");
        public static readonly Color Blue = H("4A90FF");
        public static readonly Color Muted = H("9C8F82");
        public static readonly Color Dark = H("2B2D42");
        public static readonly Color Ink = H("2B2D42");
        public static readonly Color Slate = H("3A3D5C");

        private static readonly Color[] Ramp =
        {
            H("FF6B6B"), // coral
            H("FF9F43"), // orange
            H("FFD84D"), // sunny yellow
            H("3FD9A0"), // green
            H("4DB5FF"), // sky
        };


        /// <summary>Colour of a placed number: a soft ramp from sky blue through mint and lime to amber and coral.</summary>
        public static Color NumberColor(int n, int goal = 100)
        {
            float t = Mathf.Clamp01((n - 1) / (float)Mathf.Max(1, goal - 1)) * (Ramp.Length - 1);
            int i = Mathf.Min((int)t, Ramp.Length - 2);
            return Color.Lerp(Ramp[i], Ramp[i + 1], t - i);
        }

        // ------------------------------------------------------------------ fonts & sprites
        private static Font s_font;
        public static Font Font => s_font != null ? s_font : s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static Sprite s_rounded, s_circle, s_glow, s_star;
        private static readonly Dictionary<string, Sprite> s_gradients = new Dictionary<string, Sprite>();

        private static Texture2D NewTex(int w, int h, string name)
            => new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };

        public static Sprite Rounded
        {
            get
            {
                if (s_rounded != null) return s_rounded;
                const int size = 64;
                const float radius = 24f;
                var tex = NewTex(size, size, "Rounded");
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float qy = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float dist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - radius;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - dist) * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s_rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return s_rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (s_circle != null) return s_circle;
                const int size = 96;
                var tex = NewTex(size, size, "Circle");
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) - size * 0.5f + 1f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s_circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return s_circle;
            }
        }

        /// <summary>Soft radial glow: opaque centre fading to transparent.</summary>
        public static Sprite Glow
        {
            get
            {
                if (s_glow != null) return s_glow;
                const int size = 128;
                var tex = NewTex(size, size, "Glow");
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - r);
                    a *= a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s_glow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return s_glow;
            }
        }

        public static Sprite Star
        {
            get
            {
                if (s_star != null) return s_star;
                const int size = 96;
                var poly = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                    float r = (i % 2 == 0 ? 0.48f : 0.21f) * size;
                    poly[i] = new Vector2(size * 0.5f + Mathf.Cos(a) * r, size * 0.52f + Mathf.Sin(a) * r);
                }
                var tex = NewTex(size, size, "Star");
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                        if (Inside(poly, new Vector2(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f))) hits++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 9));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s_star = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return s_star;
            }
        }

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        /// <summary>Vertical two-colour gradient sprite (cached).</summary>
        public static Sprite Gradient(Color top, Color bottom)
        {
            string key = ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom);
            if (s_gradients.TryGetValue(key, out var cached) && cached != null) return cached;
            const int h = 128;
            var tex = NewTex(4, h, "Gradient");
            var px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < 4; x++) px[y * 4 + x] = Color.Lerp(bottom, top, y / (h - 1f));
            tex.SetPixels(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            s_gradients[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ icons
        private static readonly Dictionary<string, Sprite> s_icons = new Dictionary<string, Sprite>();

        public static Sprite Icon(string name)
        {
            if (s_icons.TryGetValue(name, out var cached) && cached != null) return cached;
            Func<float, float, bool> shape;
            switch (name)
            {
                case "back": shape = (x, y) => Seg(x, y, 0.64f, 0.84f, 0.3f, 0.5f) < 0.075f || Seg(x, y, 0.3f, 0.5f, 0.64f, 0.16f) < 0.075f; break;
                case "undo":
                    shape = (x, y) =>
                    {
                        if (Seg(x, y, 0.3f, 0.64f, 0.6f, 0.64f) < 0.06f) return true;
                        float dx = x - 0.6f, dy = y - 0.43f, r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (x >= 0.6f && Mathf.Abs(r - 0.21f) < 0.06f) return true;
                        if (Seg(x, y, 0.6f, 0.22f, 0.36f, 0.22f) < 0.06f) return true;
                        return Tri(x, y, 0.1f, 0.64f, 0.34f, 0.86f, 0.34f, 0.42f);
                    };
                    break;
                case "restart":
                    shape = (x, y) =>
                    {
                        float dx = x - 0.5f, dy = y - 0.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (Mathf.Abs(r - 0.29f) < 0.065f && !(a > 15f && a < 85f)) return true;
                        float ca = 15f * Mathf.Deg2Rad;
                        float pxp = 0.5f + Mathf.Cos(ca) * 0.29f, pyp = 0.5f + Mathf.Sin(ca) * 0.29f;
                        float nx = Mathf.Cos(ca), ny = Mathf.Sin(ca), tx = -Mathf.Sin(ca), ty = Mathf.Cos(ca);
                        return Tri(x, y, pxp + tx * 0.2f, pyp + ty * 0.2f, pxp + nx * 0.15f, pyp + ny * 0.15f, pxp - nx * 0.15f, pyp - ny * 0.15f);
                    };
                    break;
                case "bulb":
                    shape = (x, y) =>
                    {
                        float dx = x - 0.5f, dy = y - 0.6f;
                        if (dx * dx + dy * dy < 0.25f * 0.25f) return true;
                        if (x > 0.38f && x < 0.62f && y > 0.2f && y < 0.4f) return true;
                        return false;
                    };
                    break;
                case "gear":
                    shape = (x, y) =>
                    {
                        float dx = x - 0.5f, dy = y - 0.5f, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                        float tooth = Mathf.Cos(a * 8f) > 0.15f ? 0.11f : 0f;
                        return r < 0.33f + tooth && r > 0.13f;
                    };
                    break;
                case "help":
                    shape = (x, y) =>
                    {
                        float dx = x - 0.5f, dy = y - 0.66f, r = Mathf.Sqrt(dx * dx + dy * dy);
                        bool arc = r < 0.23f && r > 0.11f && !(dy < 0f && dx > 0f && dy > -0.16f);
                        bool stem = x > 0.45f && x < 0.57f && y > 0.34f && y < 0.5f;
                        float ddx = x - 0.51f, ddy = y - 0.2f;
                        return arc || stem || ddx * ddx + ddy * ddy < 0.075f * 0.075f;
                    };
                    break;
                case "close": shape = (x, y) => Seg(x, y, 0.26f, 0.26f, 0.74f, 0.74f) < 0.065f || Seg(x, y, 0.26f, 0.74f, 0.74f, 0.26f) < 0.065f; break;
                case "play": shape = (x, y) => Tri(x, y, 0.3f, 0.2f, 0.3f, 0.8f, 0.8f, 0.5f); break;
                case "grid":
                    shape = (x, y) => ((x > 0.16f && x < 0.46f) || (x > 0.54f && x < 0.84f)) && ((y > 0.16f && y < 0.46f) || (y > 0.54f && y < 0.84f));
                    break;
                case "check": shape = (x, y) => Seg(x, y, 0.22f, 0.5f, 0.42f, 0.3f) < 0.07f || Seg(x, y, 0.42f, 0.3f, 0.8f, 0.72f) < 0.07f; break;
                default: shape = (x, y) => false; break;
            }
            var sprite = DrawIcon(name, shape);
            s_icons[name] = sprite;
            return sprite;
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay;
            float t = Mathf.Clamp01(((px - ax) * vx + (py - ay) * vy) / (vx * vx + vy * vy));
            float dx = px - (ax + vx * t), dy = py - (ay + vy * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static bool Tri(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        private static Sprite DrawIcon(string name, Func<float, float, bool> inside)
        {
            const int size = 128;
            var tex = NewTex(size, size, "Icon_" + name);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 3; sy++)
                for (int sx = 0; sx < 3; sx++)
                    if (inside((x + (sx + 0.5f) / 3f) / size, (y + (sy + 0.5f) / 3f) / size)) hits++;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 9));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------ builders
        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            return rt;
        }

        public static RectTransform Fill(Transform parent, string name) => Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static RectTransform Anchored(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Fill(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded) { img.sprite = Rounded; img.type = Image.Type.Sliced; }
            img.raycastTarget = false;
            return img;
        }

        public static Image Sprite_(Transform parent, string name, Sprite sprite, Color color)
        {
            var rt = Fill(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool shadow = false)
        {
            var rt = Fill(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontStyle = FontStyle.Bold;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.35f);
                s.effectDistance = new Vector2(0f, -4f);
            }
            return t;
        }

        private static void AddClick(Button b, float pitch = 1f)
        {
            b.onClick.AddListener(() => AudioManager.Sound(Sfx.Click, 0.7f, pitch));
        }

        /// <summary>Chunky pill button: dark slab under a glossy face. Add listeners to the returned Button.</summary>
        public static Button PillButton(Transform parent, string name, string text, Color color, Vector2 size, Vector2 anchor, Vector2 pos, int fontSize = 50)
        {
            var rt = Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, size);
            var shadow = Panel(rt, "Shadow", Color.Lerp(color, Color.black, 0.5f));
            shadow.rectTransform.offsetMin = new Vector2(0f, -12f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -12f);
            var face = Panel(rt, "Face", color);
            face.raycastTarget = true;
            var shine = Panel(face.transform, "Shine", new Color(1f, 1f, 1f, 0.16f));
            shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            shine.rectTransform.offsetMin = new Vector2(8f, 0f);
            shine.rectTransform.offsetMax = new Vector2(-8f, -8f);
            Label(face.transform, "Text", text, fontSize, Color.white, TextAnchor.MiddleCenter, true);
            return Finish(rt, face);
        }

        /// <summary>Vertical button with an icon above a small label (used for the in-game actions).</summary>
        public static Button ActionButton(Transform parent, string name, Sprite icon, string label, Color color, Vector2 size, Vector2 anchor, Vector2 pos)
        {
            var rt = Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, size);
            var shadow = Panel(rt, "Shadow", Color.Lerp(color, Color.black, 0.5f));
            shadow.rectTransform.offsetMin = new Vector2(0f, -10f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -10f);
            var face = Panel(rt, "Face", color);
            face.raycastTarget = true;
            var shine = Panel(face.transform, "Shine", new Color(1f, 1f, 1f, 0.14f));
            shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            shine.rectTransform.offsetMin = new Vector2(8f, 0f);
            shine.rectTransform.offsetMax = new Vector2(-8f, -8f);
            var ic = Sprite_(face.transform, "Icon", icon, Color.white);
            ic.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ic.rectTransform.sizeDelta = new Vector2(size.y * 0.5f, size.y * 0.5f);
            ic.rectTransform.anchoredPosition = new Vector2(0f, size.y * 0.1f);
            var t = Label(face.transform, "Label", label, 30, new Color(1f, 1f, 1f, 0.95f));
            t.rectTransform.anchorMin = new Vector2(0f, 0f);
            t.rectTransform.anchorMax = new Vector2(1f, 0f);
            t.rectTransform.pivot = new Vector2(0.5f, 0f);
            t.rectTransform.sizeDelta = new Vector2(0f, 38f);
            t.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            return Finish(rt, face);
        }

        public static Button IconButton(Transform parent, string name, Sprite icon, Color color, float size, Vector2 anchor, Vector2 pos)
        {
            var rt = Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var shadow = Sprite_(rt, "Shadow", Circle, Color.Lerp(color, Color.black, 0.5f));
            shadow.rectTransform.offsetMin = new Vector2(0f, -8f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -8f);
            var face = Sprite_(rt, "Face", Circle, color);
            face.raycastTarget = true;
            var ic = Sprite_(face.transform, "Icon", icon, Color.white);
            ic.rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
            ic.rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            ic.rectTransform.offsetMin = Vector2.zero;
            ic.rectTransform.offsetMax = Vector2.zero;
            return Finish(rt, face);
        }

        private static Button Finish(RectTransform rt, Image face)
        {
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            face.CrossFadeColor(Color.white, 0f, true, true);
            rt.gameObject.AddComponent<PressScale>();
            AddClick(button);
            return button;
        }

        /// <summary>Scrollable vertical list; returns the content rect (anchored to the top, stretch width).</summary>
        public static ScrollRect Scroll(Transform parent, string name, Vector2 offMin, Vector2 offMax, out RectTransform content)
        {
            var root = Rect(parent, name, Vector2.zero, Vector2.one, offMin, offMax);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            var viewport = Fill(root, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect(viewport, "Content", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40f;
            return sr;
        }
    }

    /// <summary>Squashes the button slightly while pressed.</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private float _target = 1f;
        private float _current = 1f;

        public void OnPointerDown(PointerEventData e) => _target = 0.92f;
        public void OnPointerUp(PointerEventData e) => _target = 1f;
        public void OnPointerExit(PointerEventData e) => _target = 1f;

        private void OnDisable() { _target = 1f; _current = 1f; transform.localScale = Vector3.one; }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime * 4f);
            transform.localScale = Vector3.one * _current;
        }
    }

    /// <summary>Animated on/off switch.</summary>
    public sealed class Switch : MonoBehaviour, IPointerClickHandler
    {
        private Image _track;
        private RectTransform _knob;
        private Action<bool> _changed;
        private bool _on;
        private float _t;

        public static Switch Create(Transform parent, string name, Vector2 anchor, Vector2 pos, bool value, Action<bool> changed)
        {
            var rt = UIKit.Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(136f, 72f));
            var sw = rt.gameObject.AddComponent<Switch>();
            sw._track = UIKit.Panel(rt, "Track", UIKit.Muted);
            sw._track.raycastTarget = true;
            sw._knob = UIKit.Anchored(rt, "Knob", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            var k = sw._knob.gameObject.AddComponent<Image>();
            k.sprite = UIKit.Circle;
            k.color = Color.white;
            k.raycastTarget = false;
            sw._changed = changed;
            sw._on = value;
            sw._t = value ? 1f : 0f;
            sw.Apply();
            return sw;
        }

        public void OnPointerClick(PointerEventData e)
        {
            _on = !_on;
            AudioManager.Sound(Sfx.Click, 0.7f, _on ? 1.15f : 0.9f);
            _changed?.Invoke(_on);
        }

        private void Update()
        {
            float target = _on ? 1f : 0f;
            if (!Mathf.Approximately(_t, target))
            {
                _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime * 6f);
                Apply();
            }
        }

        private void Apply()
        {
            float e = Anim.Ease.InOutSine(_t);
            _track.color = Color.Lerp(UIKit.H("CFC3B4"), UIKit.Teal, e);
            _knob.anchoredPosition = new Vector2(Mathf.Lerp(36f, 100f, e), 0f);
        }
    }
}
