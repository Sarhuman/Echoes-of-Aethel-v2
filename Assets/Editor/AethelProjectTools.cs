using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoesOfAethel.V2;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Explicit commands only. No scene generation on import, compilation or build.
public static class AethelProjectTools
{
    public const string Arena = "Assets/Scenes/Aethel_FirstArena.unity";

    [MenuItem("Echoes of Aethel/Configure Windows baseline")]
    public static void Configure()
    {
        PlayerSettings.companyName = "Sarhuman";
        PlayerSettings.productName = "Echoes of Aethel v2";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        QualitySettings.SetQualityLevel(1);
        QualitySettings.vSyncCount = 1;
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        pipeline.shadowDistance = 30f;
        pipeline.shadowCascadeCount = 2;
        pipeline.mainLightShadowmapResolution = 1024;
        pipeline.supportsCameraOpaqueTexture = false;
        pipeline.supportsCameraDepthTexture = false;
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        renderer.renderingMode = RenderingMode.Forward;
        // In URP 17.6 an inactive SSAO feature can still Create() in Player after
        // its resources were stripped. Remove this unused template feature entirely.
        for (int i = renderer.rendererFeatures.Count - 1; i >= 0; i--)
        {
            var feature = renderer.rendererFeatures[i];
            if (feature != null && feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
            {
                renderer.rendererFeatures.RemoveAt(i);
                UnityEngine.Object.DestroyImmediate(feature, true);
            }
        }
        EditorUtility.SetDirty(pipeline);
        EditorUtility.SetDirty(renderer);
        // Remove template-only sample scene and missing input-action asset references.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Arena, true) };
        EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
        AssetDatabase.SaveAssets();
        Debug.Log("AETHEL_WINDOWS_BASELINE_CONFIGURED");
    }

    [MenuItem("Echoes of Aethel/Validate saved arena")]
    public static void Validate()
    {
        if (!File.Exists(Arena)) throw new InvalidOperationException("Create the arena explicitly first; validation never creates scenes.");
        var scene = EditorSceneManager.OpenScene(Arena);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) != 0)
                throw new InvalidOperationException("Missing script: " + node.name);
            foreach (var component in node.GetComponents<Component>())
            {
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.NextVisible(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                        iterator.objectReferenceValue == null && iterator.objectReferenceEntityIdValue != default)
                        throw new InvalidOperationException("Broken reference: " + node.name + "/" + iterator.propertyPath);
            }
        }
        if (UnityEngine.Object.FindObjectsByType<AethelPlayerController>().Length != 1 ||
            UnityEngine.Object.FindObjectsByType<AethelWolfAI>().Length != 1 || Camera.main == null)
            throw new InvalidOperationException("Arena requires exactly one player, wolf and main camera.");
        if ((QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) == null)
            throw new InvalidOperationException("URP is missing.");
        foreach (var bindings in UnityEngine.Object.FindObjectsByType<AethelArtBindings>())
        {
            string message = bindings.ValidateBindings();
            if (message.StartsWith("Assign") || message.StartsWith("Bastiao requires") || message.StartsWith("Disable") || message.StartsWith("Visual"))
                throw new InvalidOperationException(bindings.name + ": " + message);
        }
        var shader = Shader.Find("Aethel/Painted URP");
        if (shader == null || ShaderUtil.GetShaderMessages(shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
            throw new InvalidOperationException("Painted URP shader missing or contains compiler errors.");
        Debug.Log("AETHEL_SAVED_ARENA_VALIDATED");
    }

    [MenuItem("Echoes of Aethel/Build Windows (preserve content)")]
    public static void BuildWindows()
    {
        Validate();
        var paths = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".meta")).ToArray();
        var before = paths.ToDictionary(p => p, Hash);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { Arena }, locationPathName = "Builds/Windows/EchoesOfAethel.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        foreach (var entry in before)
            if (!File.Exists(entry.Key) || Hash(entry.Key) != entry.Value)
                throw new InvalidOperationException("Build changed protected content: " + entry.Key);
        var after = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".meta")).ToArray();
        if (after.Length != before.Count) throw new InvalidOperationException("Build added/removed protected content.");
        if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed.");
        Debug.Log("AETHEL_WINDOWS_BUILD_PASSED; scene/prefab/meta SHA256 unchanged; " + result.summary.totalSize + " bytes");
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
}
