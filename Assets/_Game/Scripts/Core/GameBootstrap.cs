using UnityEngine;
using UnityEngine.EventSystems;

namespace HundredGame
{
    /// <summary>The only object in the scene: builds camera settings, input, audio and the whole UI at startup.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIKit.BgBottom;
            cam.cullingMask = 0;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            AudioManager.GetOrCreate().PlayMusic();
            new GameObject("UI").AddComponent<UIManager>();
        }
    }
}
