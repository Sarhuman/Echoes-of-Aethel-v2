using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit inspection/import only. Never invoked by compilation or build.
public static class AethelBastiaoImport
{
    public const string ModelPath = "Assets/Art/Models/Exported/Meshy_AI_knight_rigged_biped_Animation_Walking_frame_rate_30.fbx";
    public const string PrefabPath = "Assets/Prefabs/Characters/PF_Bastiao.prefab";

    [MenuItem("Echoes of Aethel/Art/Configure Bastiao import")]
    public static void ConfigureImport()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.optimizeGameObjects = false; // Sockets need accessible bones.
        importer.isReadable = false;
        // The source names its spine bones in reverse numerical order.
        string[] human = { "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head", "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes", "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes" };
        string[] bones = { "Hips", "Spine02", "Spine01", "Spine", "neck", "Head", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase" };
        var description = importer.humanDescription;
        description.human = human.Select((name, i) => new HumanBone { humanName = name, boneName = bones[i], limit = new HumanLimit { useDefaultValues = true } }).ToArray();
        importer.humanDescription = description;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = "Walking"; clip.loopTime = true; clip.loopPose = true;
            clip.lockRootRotation = true; clip.lockRootPositionXZ = true; clip.lockRootHeightY = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Textures" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (!Path.GetFileName(path).StartsWith("Meshy_AI_knight_rigged_biped")) continue;
            var texture = (TextureImporter)AssetImporter.GetAtPath(path);
            texture.sRGBTexture = path.EndsWith("texture_0.png");
            texture.maxTextureSize = 2048; texture.mipmapEnabled = true;
            texture.isReadable = false; texture.textureCompression = TextureImporterCompression.Compressed;
            texture.SaveAndReimport();
        }
        Audit();
    }

    [MenuItem("Echoes of Aethel/Art/Audit Bastiao model")]
    public static void Audit()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (asset == null) throw new InvalidOperationException("Bastiao FBX is missing.");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, preview);
            var renders = model.GetComponentsInChildren<Renderer>(true);
            var bounds = renders[0].bounds;
            foreach (var renderer in renders) bounds.Encapsulate(renderer.bounds);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            var animator = model.GetComponentInChildren<Animator>();
            var lines = new System.Collections.Generic.List<string> {
                "Model: " + ModelPath, "Rig: " + importer.animationType,
                "Scale: " + importer.globalScale, "Bounds center: " + bounds.center.ToString("F4"),
                "Bounds size: " + bounds.size.ToString("F4"),
                "Avatar valid/human: " + (animator != null && animator.avatar != null ? animator.avatar.isValid + "/" + animator.avatar.isHuman : "none")
            };
            foreach (var node in model.GetComponentsInChildren<Transform>(true))
                lines.Add("Node: " + AnimationUtility.CalculateTransformPath(node, model.transform) + " | pos " + node.position.ToString("F4"));
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                lines.Add("Mesh: " + skin.name + " | vertices " + skin.sharedMesh.vertexCount + " | triangles " + skin.sharedMesh.triangles.Length / 3 + " | bones " + skin.bones.Length);
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
                lines.Add("Clip: " + clip.name + " | duration " + clip.length + " | fps " + clip.frameRate);
            Directory.CreateDirectory("docs/validation/bastiao");
            File.WriteAllLines("docs/validation/bastiao/model-audit.txt", lines);
            Debug.Log("AETHEL_BASTIAO_AUDIT " + bounds.size.ToString("F4"));
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
