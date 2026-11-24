using UnityEngine;

namespace HundredGame
{
    public enum Sfx { Click, Place, Error, Undo, Milestone, Win, Lose, Hint, Star }

    /// <summary>Procedural audio: marimba plucks, FM bells and a soft ambient loop, so the project ships without audio files.</summary>
    public static class Synth
    {
        public const int Rate = 32000;
        private const float TwoPi = Mathf.PI * 2f;

        public static AudioClip Create(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click: return Click();
                case Sfx.Place: return Place();
                case Sfx.Error: return ErrorThud();
                case Sfx.Undo: return Undo();
                case Sfx.Milestone: return Chime("Milestone", new[] { 1046.5f, 1318.5f, 1568f, 2093f }, 0.075f, 0.9f, 0.085f);
                case Sfx.Win: return Win();
                case Sfx.Lose: return Lose();
                case Sfx.Hint: return Chime("Hint", new[] { 1568f, 2349f }, 0.07f, 0.5f, 0.07f);
                default: return Chime("Star", new[] { 1318.5f }, 0.1f, 0.6f, 0.08f);
            }
        }

        // ------------------------------------------------------------------ helpers
        private static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Soft(float x) => (float)System.Math.Tanh(x);

        private static float Edge(int i, int n, float attack, float release)
        {
            int a = Mathf.Max(1, (int)(attack * Rate)), r = Mathf.Max(1, (int)(release * Rate));
            return Mathf.Min(1f, Mathf.Min(i / (float)a, (n - i) / (float)r));
        }

        private static void Pluck(float[] d, float start, float freq, float volume, float decay)
        {
            int s = (int)(start * Rate);
            int len = Mathf.Min(d.Length - s, (int)(decay * 6f * Rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t / decay) * Mathf.Min(1f, i / 40f);
                float v = Mathf.Sin(TwoPi * freq * t)
                          + 0.32f * Mathf.Sin(TwoPi * freq * 4f * t) * Mathf.Exp(-t / (decay * 0.25f))
                          + 0.12f * Mathf.Sin(TwoPi * freq * 2f * t);
                d[s + i] += v * env * volume;
            }
        }

        private static void Bell(float[] d, float start, float freq, float volume, float decay)
        {
            int s = (int)(start * Rate);
            int len = Mathf.Min(d.Length - s, (int)(decay * 5f * Rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float mod = Mathf.Sin(TwoPi * freq * 3.5f * t) * 1.4f * Mathf.Exp(-t * 7f);
                float v = Mathf.Sin(TwoPi * freq * t + mod) + 0.25f * Mathf.Sin(TwoPi * freq * 2f * t) * Mathf.Exp(-t * 3f);
                d[s + i] += v * Mathf.Exp(-t / decay) * Mathf.Min(1f, i / 30f) * volume;
            }
        }

        private static void Pad(float[] d, float start, float length, float[] freqs, float volume, float fade)
        {
            int s = (int)(start * Rate);
            int len = Mathf.Min(d.Length - s, (int)(length * Rate));
            foreach (float f in freqs)
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, Mathf.Min(t / fade, (len - i) / (float)Rate / fade));
                float v = Mathf.Sin(TwoPi * f * t) + 0.6f * Mathf.Sin(TwoPi * f * 1.004f * t + 1.3f) + 0.18f * Mathf.Sin(TwoPi * f * 2f * t);
                d[s + i] += v * env * volume;
            }
        }

        private static void Echo(float[] d, float delay, float feedback)
        {
            int n = (int)(delay * Rate);
            for (int i = n; i < d.Length; i++) d[i] += d[i - n] * feedback;
        }

        private static AudioClip Finish(string name, float[] d, float rmsTarget)
        {
            float peak = 0.0001f;
            foreach (float s in d) peak = Mathf.Max(peak, Mathf.Abs(s));
            double sum = 0;
            int count = 0;
            float gate = peak * 0.05f;
            foreach (float s in d)
            {
                if (Mathf.Abs(s) < gate) continue;
                sum += s * s;
                count++;
            }
            float rms = count > 0 ? Mathf.Sqrt((float)(sum / count)) : peak;
            float gain = rmsTarget / Mathf.Max(0.0001f, rms);
            for (int i = 0; i < d.Length; i++) d[i] = Soft(d[i] * gain) * 0.95f;
            return Build(name, d);
        }

        // ------------------------------------------------------------------ effects
        private static AudioClip Click()
        {
            var d = new float[(int)(0.07f * Rate)];
            Pluck(d, 0f, 1500f, 0.7f, 0.012f);
            return Finish("Click", d, 0.05f);
        }

        /// <summary>Played with a pitch that follows the number, so counting up becomes a melody.</summary>
        private static AudioClip Place()
        {
            var d = new float[(int)(0.55f * Rate)];
            Pluck(d, 0f, 392f, 0.9f, 0.11f);
            Pluck(d, 0f, 784f, 0.25f, 0.06f);
            var rng = new System.Random(5);
            for (int i = 0; i < 300; i++) d[i] += ((float)rng.NextDouble() - 0.5f) * 0.25f * (1f - i / 300f);
            return Finish("Place", d, 0.085f);
        }

        private static AudioClip ErrorThud()
        {
            var d = new float[(int)(0.26f * Rate)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(170f, 80f, Mathf.Clamp01(t / 0.2f));
                phase += TwoPi * f / Rate;
                d[i] = (Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2f)) * Mathf.Exp(-t * 14f) * Edge(i, d.Length, 0.002f, 0.03f);
            }
            return Finish("Error", d, 0.07f);
        }

        private static AudioClip Undo()
        {
            var d = new float[(int)(0.22f * Rate)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(820f, 380f, Mathf.Clamp01(t / 0.2f));
                phase += TwoPi * f / Rate;
                d[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 11f) * Edge(i, d.Length, 0.004f, 0.04f);
            }
            return Finish("Undo", d, 0.06f);
        }

        private static AudioClip Chime(string name, float[] notes, float spacing, float tail, float volume)
        {
            var d = new float[(int)((notes.Length * spacing + tail) * Rate)];
            for (int k = 0; k < notes.Length; k++) Bell(d, k * spacing, notes[k], 0.7f, 0.22f);
            Echo(d, 0.13f, 0.28f);
            return Finish(name, d, 0.075f + volume * 0.1f);
        }

        private static AudioClip Win()
        {
            var d = new float[(int)(3.6f * Rate)];
            float[] mel = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1568f, 2093f };
            for (int k = 0; k < mel.Length; k++) Bell(d, k * 0.11f, mel[k], 0.75f, 0.35f);
            Pad(d, 0.55f, 2.8f, new[] { 261.63f, 329.63f, 392f, 523.25f }, 0.05f, 0.5f);
            Bell(d, 0.85f, 2093f, 0.7f, 0.5f);
            Bell(d, 1.05f, 2637f, 0.55f, 0.5f);
            Echo(d, 0.17f, 0.35f);
            return Finish("Win", d, 0.1f);
        }

        private static AudioClip Lose()
        {
            var d = new float[(int)(2.2f * Rate)];
            Pluck(d, 0f, 392f, 0.8f, 0.2f);
            Pluck(d, 0.22f, 329.63f, 0.8f, 0.2f);
            Pluck(d, 0.44f, 261.63f, 0.9f, 0.3f);
            Pad(d, 0.4f, 1.6f, new[] { 130.81f, 196f, 261.63f }, 0.05f, 0.4f);
            Echo(d, 0.19f, 0.3f);
            return Finish("Lose", d, 0.075f);
        }

        // ------------------------------------------------------------------ music
        /// <summary>Calm lo-fi style loop: warm pads, soft bass, a slow marimba arpeggio and a faint shaker.</summary>
        public static AudioClip CreateMusic()
        {
            const float bpm = 84f;
            float beat = 60f / bpm;
            int bars = 8;
            float barLen = beat * 4f;
            float loop = bars * barLen;
            float tailSeconds = 1.2f;
            int loopSamples = (int)(loop * Rate);
            var d = new float[loopSamples + (int)(tailSeconds * Rate)];

            // Cmaj7 - Am7 - Fmaj7 - G6 (twice)
            float[][] chords =
            {
                new[] { 130.81f, 164.81f, 196f, 246.94f },
                new[] { 110f, 130.81f, 164.81f, 196f },
                new[] { 87.31f, 130.81f, 164.81f, 220f },
                new[] { 98f, 123.47f, 146.83f, 164.81f },
            };
            float[] arpNotes = { 523.25f, 659.25f, 783.99f, 987.77f };

            for (int bar = 0; bar < bars; bar++)
            {
                var chord = chords[bar % chords.Length];
                float t0 = bar * barLen;
                float[] pad = { chord[0] * 2f, chord[1] * 2f, chord[2] * 2f, chord[3] * 2f };
                Pad(d, t0, barLen, pad, 0.035f, 0.55f);
                Pluck(d, t0, chord[0], 0.55f, 0.35f);
                Pluck(d, t0 + beat * 2.5f, chord[0] * 1.5f, 0.35f, 0.3f);

                for (int step = 0; step < 8; step++)
                {
                    int pick = (step * 3 + bar) % 4;
                    float f = chord[pick] * 4f;
                    if (step % 2 == 0 || step == 7) Pluck(d, t0 + step * beat * 0.5f, f, step % 4 == 0 ? 0.3f : 0.2f, 0.16f);
                }
                if (bar % 2 == 1) Pluck(d, t0 + beat * 3.5f, arpNotes[bar / 2 % 4], 0.22f, 0.22f);

                // faint shaker on the off-beats
                var rng = new System.Random(bar + 1);
                for (int step = 1; step < 8; step += 2)
                {
                    int s = (int)((t0 + step * beat * 0.5f) * Rate);
                    int len = (int)(0.05f * Rate);
                    float prev = 0f;
                    for (int i = 0; i < len && s + i < d.Length; i++)
                    {
                        float n = (float)rng.NextDouble() * 2f - 1f;
                        float hp = n - prev * 0.9f;
                        prev = n;
                        d[s + i] += hp * Mathf.Exp(-i / (0.012f * Rate)) * 0.05f;
                    }
                }
            }

            Echo(d, beat * 0.75f, 0.32f);
            Echo(d, 0.083f, 0.18f);

            // fold the tail back onto the start so the loop is seamless
            var loopData = new float[loopSamples];
            System.Array.Copy(d, loopData, loopSamples);
            for (int i = 0; i < d.Length - loopSamples; i++) loopData[i] += d[loopSamples + i];
            return Finish("Music", loopData, 0.06f);
        }
    }
}
