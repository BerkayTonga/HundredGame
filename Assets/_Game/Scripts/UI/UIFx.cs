using UnityEngine;
using UnityEngine.UI;

namespace HundredGame
{
    /// <summary>Short vibration pulses on Android (no-op elsewhere / when disabled).</summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject s_vibrator;
        private static bool s_failed;

        private static AndroidJavaObject Vibrator
        {
            get
            {
                if (s_vibrator != null || s_failed) return s_vibrator;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        s_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                catch { s_failed = true; }
                return s_vibrator;
            }
        }
#endif

        public static void Light() => Pulse(14);
        public static void Medium() => Pulse(30);

        public static void Heavy()
        {
            if (!SaveData.VibrationOn) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate(); // also makes Unity add the Android VIBRATE permission
#endif
        }

        private static void Pulse(long ms)
        {
            if (!SaveData.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Vibrator?.Call("vibrate", ms); } catch { }
#endif
        }
    }

    /// <summary>UI particle effects: ripples, bursts, confetti, floating texts.</summary>
    public static class UIFx
    {
        public static void Ripple(Transform parent, Vector2 pos, Color color, float startSize, float endSize, float duration = 0.5f)
        {
            var img = UIKit.Sprite_(parent, "Ripple", UIKit.Circle, color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            Anim.Run(duration, e =>
            {
                if (img == null) return;
                float s = Mathf.Lerp(startSize, endSize, e);
                rt.sizeDelta = new Vector2(s, s);
                var c = color;
                c.a = color.a * (1f - e);
                img.color = c;
            }, () => { if (img != null) Object.Destroy(img.gameObject); });
        }

        public static void Burst(Transform parent, Vector2 pos, Color color, int count, float speed, float size = 18f, float life = 0.65f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value) / count * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float v = speed * Random.Range(0.5f, 1f);
                float s = size * Random.Range(0.5f, 1f);
                SpawnParticle(parent, pos, dir * v, s, Color.Lerp(color, Color.white, Random.value * 0.5f), life * Random.Range(0.7f, 1f), 600f, false);
            }
        }

        public static void Confetti(Transform parent, int count, float width, float height)
        {
            Color[] palette = { UIKit.Teal, UIKit.Pink, UIKit.Gold, UIKit.Violet, UIKit.Blue, UIKit.H("FF9F43") };
            for (int i = 0; i < count; i++)
            {
                Vector2 start = new Vector2(Random.Range(-width * 0.5f, width * 0.5f), height * 0.5f + Random.Range(0f, 200f));
                Vector2 vel = new Vector2(Random.Range(-160f, 160f), Random.Range(-1100f, -400f));
                SpawnParticle(parent, start, vel, Random.Range(16f, 30f), palette[Random.Range(0, palette.Length)], Random.Range(1.6f, 2.8f), 120f, true);
            }
        }

        private static void SpawnParticle(Transform parent, Vector2 pos, Vector2 vel, float size, Color color, float life, float gravity, bool confetti)
        {
            var img = UIKit.Sprite_(parent, "P", confetti ? UIKit.Rounded : UIKit.Circle, color);
            if (confetti) { img.type = Image.Type.Sliced; }
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = confetti ? new Vector2(size, size * 0.55f) : new Vector2(size, size);
            rt.anchoredPosition = pos;
            float spin = Random.Range(-400f, 400f);
            float phase = Random.value * 6f;
            Vector2 p = pos, v = vel;
            float elapsed = 0f;
            Anim.Run(life, e =>
            {
                if (img == null) return;
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                v.y -= gravity * dt;
                v *= confetti ? 0.995f : 0.96f;
                p += v * dt;
                if (confetti) p.x += Mathf.Sin(elapsed * 5f + phase) * 90f * dt;
                rt.anchoredPosition = p;
                rt.localRotation = Quaternion.Euler(0f, 0f, spin * elapsed);
                var c = color;
                c.a = confetti ? Mathf.Clamp01((1f - e) * 3f) : 1f - e * e;
                img.color = c;
                if (!confetti) rt.localScale = Vector3.one * (1f - e * 0.6f);
            }, () => { if (img != null) Object.Destroy(img.gameObject); }, 0f, Anim.Ease.Linear);
        }

        public static void FloatText(Transform parent, Vector2 pos, string text, Color color, int size = 60, float rise = 120f)
        {
            var label = UIKit.Label(parent, "Float", text, size, color, TextAnchor.MiddleCenter, true);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 120f);
            rt.anchoredPosition = pos;
            Anim.Run(1f, e =>
            {
                if (label == null) return;
                rt.anchoredPosition = pos + new Vector2(0f, rise * Anim.Ease.OutCubic(e));
                float pop = e < 0.2f ? Anim.Ease.OutBack(e / 0.2f) : 1f;
                rt.localScale = Vector3.one * pop;
                var c = color;
                c.a = e < 0.7f ? 1f : 1f - (e - 0.7f) / 0.3f;
                label.color = c;
            }, () => { if (label != null) Object.Destroy(label.gameObject); }, 0f, Anim.Ease.Linear);
        }
    }
}
