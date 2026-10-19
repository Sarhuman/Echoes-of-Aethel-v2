using System.Collections;
using System.IO;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SavedArenaRuntimeTests
{
    [UnityTest]
    public IEnumerator SavedArenaRunsAndCanBeCaptured()
    {
        yield return SceneManager.LoadSceneAsync("Aethel_FirstArena");
        yield return new WaitForSeconds(.3f);
        var player = Object.FindFirstObjectByType<AethelPlayerController>();
        var wolf = Object.FindFirstObjectByType<AethelWolfAI>();
        Assert.IsNotNull(player); Assert.IsNotNull(wolf); Assert.IsNotNull(Camera.main);
        Assert.IsNotNull(player.HitOrigin); Assert.IsNotNull(wolf.BiteOrigin);
        Assert.IsNotNull(player.CameraTransform);
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
        {
            Directory.CreateDirectory("docs/validation");
            const string path = "docs/validation/arena-runtime.png";
            if (File.Exists(path)) File.Delete(path);
            // Explicitly render the actual playing camera; batch mode has no GameView presentation frame.
            var target = new RenderTexture(1920, 1080, 24);
            target.Create();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(Camera.main, request);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release(); Object.Destroy(target); Object.Destroy(image);
            Assert.IsTrue(File.Exists(path), "Runtime capture was not written.");
        }
    }
}
