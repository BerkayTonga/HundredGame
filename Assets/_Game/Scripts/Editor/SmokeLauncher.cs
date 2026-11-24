using UnityEditor;
using UnityEngine;

namespace HundredGame.Editor
{
    /// <summary>Starts the editor smoke bot when play mode is entered through <see cref="Run"/>.</summary>
    [InitializeOnLoad]
    public static class SmokeLauncher
    {
        private const string Flag = "hg_smoke";

        static SmokeLauncher()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(Flag, false))
                {
                    EditorPrefs.DeleteKey(Flag);
                    new GameObject("SmokeTest").AddComponent<EditorSmokeTest>();
                }
            };
        }

        [MenuItem("HundredGame/Run Smoke Test")]
        public static void Run()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorPrefs.SetBool(Flag, true);
            PlayerPrefs.DeleteAll();
            EditorApplication.EnterPlaymode();
        }
    }
}
