#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace HundredGame
{
    /// <summary>Editor-only bot: walks through the screens, plays a game with the hint bot and saves screenshots to Logs/Smoke.</summary>
    public sealed class EditorSmokeTest : MonoBehaviour
    {
        private RenderTexture _rt;
        private Camera _cam;
        private string _dir;

        private IEnumerator Start()
        {
            _dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Smoke");
            Directory.CreateDirectory(_dir);
            yield return new WaitForSeconds(3f);

            _cam = Camera.main;
            _cam.cullingMask = ~0;
            _rt = new RenderTexture(1080, 1920, 24);
            _cam.targetTexture = _rt;
            var canvas = UIManager.I.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 1f;
            yield return null;
            yield return new WaitForSeconds(0.6f);

            yield return Shot("menu");
            Popup.Help();
            yield return new WaitForSeconds(1.6f);
            yield return Shot("help");
            UIManager.I.Back();
            yield return new WaitForSeconds(0.4f);
            Popup.Settings();
            yield return new WaitForSeconds(0.8f);
            yield return Shot("settings");
            UIManager.I.Back();
            yield return new WaitForSeconds(0.4f);

            UIManager.I.GoTo(UIManager.I.Levels, 1);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("levels");

            UIManager.I.OpenLevel(0);
            yield return new WaitForSeconds(1.6f);
            yield return Shot("game_empty");

            var game = UIManager.I.Game;
            int guard = 0;
            while (!game.Board.Won && !game.Board.Stuck && guard++ < 200)
            {
                int hint = game.Board.Hint();
                if (hint < 0) break;
                game.OnCell(hint);
                int c = game.Board.Count;
                if (c == 1) { yield return new WaitForSeconds(0.15f); yield return Shot("game_first"); }
                if (c == 14)
                {
                    // wrong tap -> shake + toast
                    for (int i = 0; i < 100; i++) if (game.Board.Value(i) == 0 && !game.Board.CanPlace(i)) { game.OnCell(i); break; }
                    yield return new WaitForSeconds(0.12f);
                    yield return Shot("game_invalid");
                    yield return new WaitForSeconds(0.5f);
                }
                if (c == 40) yield return Shot("game_mid");
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("[Smoke] game ended count=" + game.Board.Count + " won=" + game.Board.Won);
            yield return new WaitForSeconds(game.Board.Won ? 3.2f : 2.8f);
            yield return Shot("result");
            Debug.Log("[Smoke] finished");
            UnityEditor.EditorApplication.Exit(0);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
#endif
