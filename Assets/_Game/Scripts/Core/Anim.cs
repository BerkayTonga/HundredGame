using System;
using System.Collections;
using UnityEngine;

namespace HundredGame
{
    /// <summary>Tiny tween runner (unscaled time). Every callback must tolerate destroyed targets.</summary>
    public sealed class Anim : MonoBehaviour
    {
        private static Anim s_instance;

        private static Anim Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("Anim");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<Anim>();
                }
                return s_instance;
            }
        }

        public static class Ease
        {
            public static float Linear(float t) => t;
            public static float OutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
            public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
            public static float OutBack(float t)
            {
                const float c1 = 1.9f, c3 = c1 + 1f;
                float u = t - 1f;
                return 1f + c3 * u * u * u + c1 * u * u;
            }
            public static float OutElastic(float t)
            {
                if (t <= 0f) return 0f;
                if (t >= 1f) return 1f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
            }
        }

        public static Coroutine Run(float duration, Action<float> step, Action done = null, float delay = 0f, Func<float, float> ease = null)
            => Instance.StartCoroutine(Routine(duration, step, done, delay, ease ?? Ease.OutCubic));

        public static Coroutine After(float delay, Action action)
            => Instance.StartCoroutine(Routine(0f, null, action, delay, Ease.Linear));

        public static void Stop(Coroutine c)
        {
            if (c != null && s_instance != null) s_instance.StopCoroutine(c);
        }

        private static IEnumerator Routine(float duration, Action<float> step, Action done, float delay, Func<float, float> ease)
        {
            while (delay > 0f) { delay -= Time.unscaledDeltaTime; yield return null; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step?.Invoke(ease(Mathf.Clamp01(t / duration)));
                yield return null;
            }
            step?.Invoke(ease(1f));
            done?.Invoke();
        }

        // ---------------------------------------------------------------- helpers
        public static Coroutine Scale(Transform tr, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null, float delay = 0f, Action done = null)
            => Run(duration, e => { if (tr != null) tr.localScale = Vector3.LerpUnclamped(from, to, e); }, done, delay, ease);

        public static Coroutine Pop(Transform tr, float from = 0f, float to = 1f, float duration = 0.4f, float delay = 0f)
            => Scale(tr, Vector3.one * from, Vector3.one * to, duration, Ease.OutBack, delay);

        public static Coroutine Fade(CanvasGroup g, float from, float to, float duration, float delay = 0f, Action done = null)
            => Run(duration, e => { if (g != null) g.alpha = Mathf.Lerp(from, to, e); }, done, delay, Ease.OutCubic);

        public static Coroutine Move(RectTransform rt, Vector2 from, Vector2 to, float duration, Func<float, float> ease = null, float delay = 0f, Action done = null)
            => Run(duration, e => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e); }, done, delay, ease);
    }
}
