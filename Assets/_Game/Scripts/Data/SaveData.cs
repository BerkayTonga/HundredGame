using UnityEngine;

namespace HundredGame
{
    /// <summary>Everything persisted between sessions (PlayerPrefs).</summary>
    public static class SaveData
    {
        public static bool SfxOn { get => PlayerPrefs.GetInt("sfx", 1) == 1; set => PlayerPrefs.SetInt("sfx", value ? 1 : 0); }
        public static bool MusicOn { get => PlayerPrefs.GetInt("music", 1) == 1; set => PlayerPrefs.SetInt("music", value ? 1 : 0); }
        public static bool VibrationOn { get => PlayerPrefs.GetInt("vib", 1) == 1; set => PlayerPrefs.SetInt("vib", value ? 1 : 0); }

        public static int GamesPlayed { get => PlayerPrefs.GetInt("played", 0); set => PlayerPrefs.SetInt("played", value); }
        public static int TotalPlaced { get => PlayerPrefs.GetInt("placed", 0); set => PlayerPrefs.SetInt("placed", value); }

        public static int Best(int level) => PlayerPrefs.GetInt("best_" + level, 0);
        public static void SetBest(int level, int score) => PlayerPrefs.SetInt("best_" + level, score);

        /// <summary>Unfinished game as a comma separated list of cell indices (empty = none).</summary>
        public static string Progress(int level) => PlayerPrefs.GetString("prog_" + level, "");
        public static void SetProgress(int level, string history) => PlayerPrefs.SetString("prog_" + level, history);

        public static bool SeenRules(int level) => PlayerPrefs.GetInt("rules_" + level, 0) == 1;
        public static void MarkRulesSeen(int level) => PlayerPrefs.SetInt("rules_" + level, 1);

        public static int Stars(int level)
        {
            var def = Levels.All[level];
            int best = Best(level), stars = 0;
            var th = def.Stars;
            for (int i = 0; i < th.Length; i++) if (best >= th[i]) stars = i + 1;
            return stars;
        }

        public static int TotalStars()
        {
            int sum = 0;
            for (int i = 0; i < Levels.All.Count; i++) sum += Stars(i);
            return sum;
        }

        public static void ResetAll()
        {
            for (int i = 0; i < Levels.All.Count; i++) { PlayerPrefs.DeleteKey("best_" + i); PlayerPrefs.DeleteKey("prog_" + i); PlayerPrefs.DeleteKey("rules_" + i); }
            PlayerPrefs.DeleteKey("played");
            PlayerPrefs.DeleteKey("placed");
            Save();
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
