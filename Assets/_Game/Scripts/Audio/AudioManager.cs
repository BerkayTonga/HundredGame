using UnityEngine;

namespace HundredGame
{
    /// <summary>Persistent music + pooled one-shot voices. Clips can be overridden by Resources/Audio/&lt;SfxName&gt; (or "Music").</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const float MusicVolume = 0.32f;

        public static AudioManager Instance { get; private set; }

        private AudioSource _music;
        private AudioSource[] _voices;
        private AudioClip[] _clips;
        private float[] _last;
        private int _next;

        public static AudioManager GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            Instance = this;
            var all = (Sfx[])System.Enum.GetValues(typeof(Sfx));
            _clips = new AudioClip[all.Length];
            _last = new float[all.Length];
            foreach (var sfx in all) _clips[(int)sfx] = Resources.Load<AudioClip>("Audio/" + sfx) ?? Synth.Create(sfx);

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = Resources.Load<AudioClip>("Audio/Music") ?? Synth.CreateMusic();
            _music.loop = true;
            _music.playOnAwake = false;

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }
            ApplySettings();
        }

        public void ApplySettings()
        {
            _music.mute = !SaveData.MusicOn;
            _music.volume = MusicVolume;
            if (SaveData.MusicOn && !_music.isPlaying) _music.Play();
        }

        public void PlayMusic()
        {
            if (!_music.isPlaying) _music.Play();
        }

        public void Play(Sfx sfx, float volume = 1f, float pitch = 1f, float minInterval = 0.03f)
        {
            if (!SaveData.SfxOn) return;
            int id = (int)sfx;
            float now = Time.unscaledTime;
            if (now - _last[id] < minInterval) return;
            _last[id] = now;
            var voice = _voices[_next];
            _next = (_next + 1) % VoiceCount;
            voice.pitch = pitch;
            voice.PlayOneShot(_clips[id], volume);
        }

        public static void Sound(Sfx sfx, float volume = 1f, float pitch = 1f, float minInterval = 0.03f)
        {
            if (Instance != null) Instance.Play(sfx, volume, pitch, minInterval);
        }

        /// <summary>Pentatonic pitch for the n-th number so counting up sounds like a melody.</summary>
        public static float NotePitch(int n)
        {
            int[] scale = { 0, 2, 4, 7, 9 };
            int step = Mathf.Max(0, n - 1) % 10;
            int semitone = scale[step % 5] + 12 * (step / 5);
            return Mathf.Pow(2f, semitone / 12f) * 0.7f;
        }
    }
}
