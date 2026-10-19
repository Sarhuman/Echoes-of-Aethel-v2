using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EchoesOfAethel.V2;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit, opt-in prefab edits only. No scene, compilation or build callbacks.
public static class AethelAssetIntegration
{
    static readonly string[] Names = { "Bastiao", "LoboEtereo", "Machado", "Escudo", "Arvore", "Rocha", "Pilar", "Piso" };
    static string Model(string name) => name == "Bastiao" ? AethelBastiaoImport.ModelPath : "Assets/Art/Models/Exported/MD_" + name + ".fbx";
    static string MaterialPath(string name) => "Assets/Art/Materials/MT_" + name + "_Painted.mat";
    static string TexturePrefix(string name) => new Dictionary<string, string> {
        {"Bastiao", "Meshy_AI_knight_rigged_biped_texture_0"}, {"LoboEtereo", "Meshy_AI_mystic_wolf_5k"},
        {"Machado", "Meshy_AI_battle_axe_3k"}, {"Escudo", "Meshy_AI_paladin_shield"},
        {"Arvore", "Meshy_AI_ancient_tree"}, {"Rocha", "Meshy_AI_mossy_boulder"},
        {"Pilar", "Meshy_AI_ruined_pillar"}, {"Piso", "Meshy_AI_modular_ground_tile"} }[name];

    [MenuItem("Echoes of Aethel/Art/Configure supplied assets")]
    public static void ConfigureImports()
    {
        AethelBastiaoImport.ConfigureImport();
        foreach (string name in Names.Skip(1))
        {
            var importer = AssetImporter.GetAtPath(Model(name)) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing model: " + name);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.addCollider = false; importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.SaveAndReimport();
        }
        foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[]{"Assets/Art/Textures"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            string file = Path.GetFileNameWithoutExtension(path);
            bool mask = file.EndsWith("_metallic") || file.EndsWith("_roughness") || file.EndsWith("_metallic_roughness");
            bool normal = file.EndsWith("_normal");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !mask && !normal; importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true; importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
        foreach (string name in Names)
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D", new[]{"Assets/Art/Textures"}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(TexturePrefix(name)))
                .Where(p => !p.Contains("metallic") && !p.Contains("roughness") && !p.Contains("normal")).ToArray();
            if (textures.Length != 1) throw new InvalidOperationException("Ambiguous albedo for " + name + ": " + string.Join(",",textures));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name));
            if (material == null) { material = new Material(Shader.Find("Aethel/Painted URP")); AssetDatabase.CreateAsset(material,MaterialPath(name)); }
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures[0]));
            material.SetColor("_BaseColor",Color.white); material.SetFloat("_LightSteps",4);
            material.SetColor("_ShadowTint",new Color(.3f,.36f,.48f));
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets(); Audit();
    }

    [MenuItem("Echoes of Aethel/Art/Audit supplied assets")]
    public static void Audit()
    {
        var lines = new List<string>();
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(string name in Names)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model(name)),scene);
                Bounds b = GeometryBounds(model);
                int triangles = model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3)
                    + model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(m=>m.sharedMesh.triangles.Length/3);
                lines.Add(name + " | triangles " + triangles + " | center " + b.center.ToString("F4") + " | size " + b.size.ToString("F4"));
                Object.DestroyImmediate(model);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        Directory.CreateDirectory("docs/validation/assets"); File.WriteAllLines("docs/validation/assets/unity-model-audit.txt",lines);
        Debug.Log("AETHEL_ASSET_IMPORTS_VALIDATED");
    }

    static Bounds GeometryBounds(GameObject model, Transform relativeTo = null)
    {
        bool first = true; Bounds b = default;
        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var baked = new Mesh(); skin.BakeMesh(baked);
            foreach(var p in baked.vertices) { var v=skin.transform.TransformPoint(p); if(relativeTo!=null)v=relativeTo.InverseTransformPoint(v); if(first){b=new Bounds(v,Vector3.zero);first=false;} else b.Encapsulate(v); }
            Object.DestroyImmediate(baked);
        }
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            var meshBounds=filter.sharedMesh.bounds;
            for(int i=0;i<8;i++) {
                var p=meshBounds.center+Vector3.Scale(meshBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var v=filter.transform.TransformPoint(p); if(relativeTo!=null)v=relativeTo.InverseTransformPoint(v);
                if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);
            }
        }
        if(first) throw new InvalidOperationException("Model has no geometry: "+model.name);
        return b;
    }

    static GameObject Visual(string name, Transform parent, float height, Vector3 center, bool bottomAligned)
    {
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model(name)),parent);
        model.name = "Model_" + name;
        Fit(model,parent,height,center,bottomAligned);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(_=>AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name))).ToArray();
        foreach(var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        return model;
    }

    static void Fit(GameObject model,Transform parent,float height,Vector3 center,bool bottomAligned)
    {
        // Measure in the socket/visual frame, including actual skin geometry rather than padded culling bounds.
        var bounds = GeometryBounds(model,parent);
        float scale = height / bounds.size.y;
        model.transform.localScale *= scale;
        Vector3 localCenter = bounds.center;
        model.transform.localPosition = center - localCenter * scale;
        if(bottomAligned) model.transform.localPosition += Vector3.up * height * .5f;
    }

    static void RemovePlaceholder(Transform visual)
    {
        foreach(Transform child in visual.Cast<Transform>().ToArray())
            if(child.name.StartsWith("Visual_PLACEHOLDER")) Object.DestroyImmediate(child.gameObject);
    }

    [MenuItem("Echoes of Aethel/Art/Integrate supplied prefabs (explicit)")]
    public static void IntegratePrefabs()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Edit prefabs only outside play/build.");
        // Refuse repeat destructive replacement. New exports are swapped through the nested model reference.
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(AethelBastiaoImport.PrefabPath);
        if(existing.transform.Find("VisualRoot").GetComponentsInChildren<AethelLocomotionVisual>(true).Length>0)
            throw new InvalidOperationException("Assets already integrated. Edit the existing visual children instead.");
        foreach(string name in Names)
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Model(name))==null || AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name))==null)
                throw new InvalidOperationException("Configure and audit all imports first: "+name);
        var savedScene=File.ReadAllBytes(AethelProjectTools.Arena);
        IntegrateBastiao(); IntegrateWolf();
        Environment("Arvore","PF_BosqueArvore_01",6,Vector3.zero);
        Environment("Rocha","PF_BosqueRocha_01",1.4f,Vector3.zero);
        Environment("Pilar","PF_BosquePilar_01",4,Vector3.zero);
        // Tile is prepared separately; the saved arena floor/physics are never rewritten by this command.
        string floorPath="Assets/Prefabs/Environment/PF_BosquePiso_01.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(floorPath)==null)
        {
            var tile=new GameObject("PF_BosquePiso_01");
            try {
                var model=Visual("Piso",tile.transform,.25f,Vector3.zero,false);
                var b=GeometryBounds(model);
                var collision=new GameObject("Collision"); collision.transform.SetParent(tile.transform,false);
                var box=collision.AddComponent<BoxCollider>(); box.size=b.size; box.center=b.center;
                PrefabUtility.SaveAsPrefabAsset(tile,floorPath);
            } finally {Object.DestroyImmediate(tile);}
        }
        AssetDatabase.SaveAssets();
        RefineVisualFit();
        if(!savedScene.SequenceEqual(File.ReadAllBytes(AethelProjectTools.Arena))) throw new InvalidOperationException("Saved scene changed unexpectedly.");
        Debug.Log("AETHEL_ASSET_PREFABS_INTEGRATED_SCENE_UNCHANGED");
    }

    static void IntegrateBastiao()
    {
        string path=AethelBastiaoImport.PrefabPath;
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var visual=root.transform.Find("VisualRoot"); RemovePlaceholder(visual);
            var old=visual.GetComponent<Animator>(); if(old!=null) Object.DestroyImmediate(old);
            var model=Visual("Bastiao",visual,1.9f,Vector3.zero,true);
            var animator=model.GetComponentInChildren<Animator>(); animator.applyRootMotion=false;
            if(animator.avatar==null || !animator.avatar.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Invalid Humanoid Avatar");
            const string controllerPath="Assets/Art/Animations/AC_Bastiao_Locomotion.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null) {
                controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var clip=AssetDatabase.LoadAllAssetsAtPath(Model("Bastiao")).OfType<AnimationClip>().Single(c=>c.name=="Walking");
                var state=controller.layers[0].stateMachine.AddState("Walking"); state.motion=clip;
                controller.layers[0].stateMachine.defaultState=state;
            }
            animator.runtimeAnimatorController=controller;
            var weapon=visual.Find("WeaponSocket"); var shield=visual.Find("ShieldSocket");
            weapon.SetParent(animator.GetBoneTransform(HumanBodyBones.RightHand),false);
            shield.SetParent(animator.GetBoneTransform(HumanBodyBones.LeftHand),false);
            weapon.localPosition=Vector3.zero; weapon.localRotation=Quaternion.identity;
            shield.localPosition=Vector3.zero; shield.localRotation=Quaternion.identity;
            Visual("Machado",weapon,1.1f,new Vector3(0,.2f,0),false);
            Visual("Escudo",shield,.85f,Vector3.zero,false);
            var body=root.GetComponent<CharacterController>();
            model.AddComponent<AethelLocomotionVisual>().Configure(animator,body,root.GetComponent<AethelPlayerController>(),root.GetComponent<AethelHealth>());
            root.GetComponent<AethelArtBindings>().Configure(animator,visual,root.transform.Find("GroundCheck"),visual.Find("HitOrigin"),body,weapon,shield);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    static void IntegrateWolf()
    {
        const string path="Assets/Prefabs/Characters/PF_LoboEtereo.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var visual=root.transform.Find("VisualRoot"); RemovePlaceholder(visual);
            Visual("LoboEtereo",visual,.9f,Vector3.zero,true);
            // Source has no rig/clips. Existing Animator stays explicitly unconfigured.
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    static void Environment(string asset,string prefab,float height,Vector3 center)
    {
        string path="Assets/Prefabs/Environment/"+prefab+".prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            RemovePlaceholder(root.transform);
            Visual(asset,root.transform,height,center,false);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    [MenuItem("Echoes of Aethel/Art/Apply reviewed tree and wolf visual fit")]
    public static void RefineVisualFit()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Edit visual fit only outside play/build.");
        var wolfPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/PF_LoboEtereo.prefab");
        if(wolfPrefab.GetComponentInChildren<AethelWolfVisual>(true)!=null)
            throw new InvalidOperationException("Rigged wolf already has reviewed visual fit. This legacy tool only supports the static import.");
        foreach(string name in new[]{"Arvore","LoboEtereo"})
        {
            string path=name=="Arvore"?"Assets/Prefabs/Environment/PF_BosqueArvore_01.prefab":"Assets/Prefabs/Characters/PF_LoboEtereo.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try {
                var parent=name=="Arvore"?root.transform:root.transform.Find("VisualRoot");
                var model=parent.Find("Model_"+name).gameObject;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model(name));
                model.transform.localPosition=Vector3.zero;
                model.transform.localScale=source.transform.localScale;
                model.transform.localRotation=(name=="Arvore"?Quaternion.Euler(90,0,0):Quaternion.identity)*source.transform.localRotation;
                Fit(model,parent,name=="Arvore"?6:.9f,Vector3.zero,name!="Arvore");
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        Debug.Log("AETHEL_REVIEWED_VISUAL_FIT_APPLIED");
    }
}
