using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HundredGame.Editor
{
    /// <summary>One-click project setup: scene, player settings, app icon. Also a headless rules check.</summary>
    public static class HundredSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string IconPath = "Assets/_Game/Icon.png";

        [MenuItem("HundredGame/Setup Project")]
        public static void Run()
        {
            Directory.CreateDirectory("Assets/_Game/Scenes");
            CreateScene();
            CreateIcon();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HundredGame] Setup complete.");
        }

        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIKit.BgBottom;
            camGo.AddComponent<AudioListener>();
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Berkay Tonga";
            PlayerSettings.productName = "100";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.berkaytonga.hundredgame");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.berkaytonga.hundredgame");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Application);
        }

        /// <summary>Gradient tile-grid icon with a path of lit squares.</summary>
        private static void CreateIcon()
        {
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            Color top = UIKit.H("2B2F7A"), bottom = UIKit.H("0A0C24");
            int[] lit = { 0, 1, 2, 5, 4, 3, 6, 7, 8 };
            const float cell = 118f, gap = 22f;
            float start = (size - (3 * cell + 2 * gap)) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color c = Color.Lerp(bottom, top, y / (float)size);
                for (int i = 0; i < 9; i++)
                {
                    int gx = i % 3, gy = 2 - i / 3;
                    float cx = start + gx * (cell + gap) + cell * 0.5f, cy = start + gy * (cell + gap) + cell * 0.5f;
                    float qx = Mathf.Abs(x + 0.5f - cx) - (cell * 0.5f - 26f), qy = Mathf.Abs(y + 0.5f - cy) - (cell * 0.5f - 26f);
                    float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - 26f;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) continue;
                    int order = System.Array.IndexOf(lit, i);
                    c = Color.Lerp(c, UIKit.NumberColor(1 + order * 11), a);
                }
                px[y * size + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath);
        }

        // ------------------------------------------------------------------ headless rules check
        [MenuItem("HundredGame/Simulate Board")]
        public static void Simulate()
        {
            var def = Levels.All[0];
            var rng = new System.Random(1);
            int bestRandom = 0, sumRandom = 0;
            const int games = 300;
            var moves = new System.Collections.Generic.List<int>();
            for (int g = 0; g < games; g++)
            {
                var b = new Board(def);
                while (true)
                {
                    b.Moves(moves);
                    if (moves.Count == 0) break;
                    b.Place(moves[rng.Next(moves.Count)]);
                }
                bestRandom = Mathf.Max(bestRandom, b.Count);
                sumRandom += b.Count;
            }

            var hinted = new Board(def);
            while (hinted.Hint() >= 0) hinted.Place(hinted.Hint());

            var r = new Board(def);
            r.Place(0);
            bool ok = !r.CanPlace(1) && !r.CanPlace(2) && r.CanPlace(3) && r.CanPlace(30) && r.CanPlace(22) && !r.CanPlace(11) && !r.CanPlace(33);
            var restored = new Board(def);
            restored.Restore("0,3,33");
            Debug.Log("[HundredGame] rules ok=" + ok + "; random avg=" + (sumRandom / (float)games).ToString("0.0") + " best=" + bestRandom + "; hint-bot reaches " + hinted.Count + "; restore count=" + restored.Count);
        }

        public static void SimulateBatch()
        {
            Simulate();
            EditorApplication.Exit(0);
        }
    }
}
