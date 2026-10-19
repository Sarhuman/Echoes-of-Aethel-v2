#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoesOfAethel.V2
{
    // Opt-in standalone-build diagnostic. No behavior unless explicitly passed -aethel-smoke.
    public sealed class AethelDevelopmentSmoke : MonoBehaviour
    {
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-aethel-smoke") >= 0)
                new GameObject("Development smoke test").AddComponent<AethelDevelopmentSmoke>();
        }

        private void OnEnable() { Application.logMessageReceived += WatchErrors; }
        private void OnDisable() { Application.logMessageReceived -= WatchErrors; }
        private void WatchErrors(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }
        private void Check(bool condition, string message)
        {
            if (!condition) { failed = true; Debug.LogError("AETHEL_SMOKE_FAILURE: " + message); }
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            var smokeSettings = InputSystem.settings != null ? Instantiate(InputSystem.settings) : ScriptableObject.CreateInstance<InputSettings>();
            smokeSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            smokeSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings = smokeSettings;
            while (!SplashScreen.isFinished) yield return null;
            yield return new WaitForSeconds(.3f);
            var player = FindFirstObjectByType<AethelPlayerController>();
            var wolf = FindFirstObjectByType<AethelWolfAI>();
            Check(player != null && wolf != null && Camera.main != null, "Saved arena references");
            if (failed) { Application.Quit(1); yield break; }
            Debug.Log("AETHEL_SMOKE_HARDWARE: " + SystemInfo.processorType + "; " + SystemInfo.graphicsDeviceName + "; RAM=" + SystemInfo.systemMemorySize + "MB");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Vector3 start = player.transform.position;
            Quaternion view = Camera.main.transform.rotation;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(start, player.transform.position) > .5f, "Keyboard movement");
            Check(Quaternion.Angle(view, Camera.main.transform.rotation) < .01f, "Fixed isometric camera");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return new WaitForSeconds(.05f);
            Check(player.IsDashing && player.Stamina < 85f, "Dash and stamina");
            Check(player.GetComponent<AethelHealth>().IsInvulnerable, "Dash i-frames");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.RemoveDevice(keyboard);
            string capture = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "arena-runtime.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(capture));
            if (File.Exists(capture)) File.Delete(capture);
            var target = new RenderTexture(1920, 1080, 24);
            target.Create();
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            var pixels = image.GetPixels32();
            bool hasSceneContent = false;
            for (int i=1; i<pixels.Length; i+=257)
                if (!pixels[i].Equals(pixels[0])) { hasSceneContent = true; break; }
            Check(hasSceneContent, "Camera capture must contain scene content, not a blank frame");
            File.WriteAllBytes(capture, image.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release(); Destroy(target); Destroy(image);
            Check(File.Exists(capture), "Runtime screenshot");
            Debug.Log(failed ? "AETHEL_BUILD_SMOKE_FAILED" : "AETHEL_BUILD_SMOKE_PASSED");
            Application.Quit(failed ? 1 : 0);
        }
    }
}
#endif
