using System;
using System.IO;
using System.Linq;
using EchoesOfAethel.V2;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit authoring tools, never called by import, compilation, scene load or build.
public static class AethelWolfRigTools
{
    public const string Model = "Assets/Art/Models/Exported/MD_LoboEtereo_Rigged.fbx";
    public const string Prefab = "Assets/Prefabs/Characters/PF_LoboEtereo.prefab";
    public const string Controller = "Assets/Art/Animations/AC_LoboEtereo.controller";
    static readonly string[] States = { "Idle", "Run", "Windup", "Bite", "Recovery", "Death" };

    [Serializable]
    public sealed class FurContract { public string uvTriangles; public string bindRig; public string animationCurves; }
    public const string FurContractPath="docs/validation/wolf-local-fur/unity-contract.json";

    // Read-only fingerprints of the imported asset. UV triangles are sorted to
    // permit normal-related vertex splits/reordering without weakening UV checks.
    public static FurContract ImportedFurContract()
    {
        string Number(float value)=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        string Hash(string text) {
            using(var sha=System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();
        }
        var skin=AssetDatabase.LoadAssetAtPath<GameObject>(Model).GetComponentInChildren<SkinnedMeshRenderer>();
        var uv=skin.sharedMesh.uv;var triangles=skin.sharedMesh.triangles;
        var uvRows=new System.Collections.Generic.List<string>();
        for(int i=0;i<triangles.Length;i+=3) {
            var row=Enumerable.Range(0,3).Select(j=>Number(uv[triangles[i+j]].x)+","+Number(uv[triangles[i+j]].y));
            uvRows.Add(string.Join(";",row.OrderBy(v=>v,StringComparer.Ordinal)));
        }
        var bind=skin.sharedMesh.bindposes;
        var rigRows=skin.bones.Select((bone,i)=>bone.name+"|"+(bone.parent==null?"":bone.parent.name)+"|"+
            string.Join(",",Enumerable.Range(0,16).Select(j=>Number(bind[i][j])))).OrderBy(v=>v,StringComparer.Ordinal);
        var curves=new System.Text.StringBuilder();
        foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderBy(c=>c.name,StringComparer.Ordinal)) {
            curves.AppendLine(clip.name+"|"+Number(clip.length)+"|"+AnimationUtility.GetAnimationEvents(clip).Length);
            foreach(var binding in AnimationUtility.GetCurveBindings(clip).OrderBy(b=>b.path+"|"+b.propertyName,StringComparer.Ordinal)) {
                curves.AppendLine(binding.path+"|"+binding.propertyName+"|"+binding.type.FullName);
                foreach(var key in AnimationUtility.GetEditorCurve(clip,binding).keys)
                    curves.AppendLine(string.Join(",",new[]{key.time,key.value,key.inTangent,key.outTangent,key.inWeight,key.outWeight}.Select(Number))+","+(int)key.weightedMode);
            }
        }
        return new FurContract { uvTriangles=Hash(string.Join("\n",uvRows.OrderBy(v=>v,StringComparer.Ordinal))),
            bindRig=Hash(string.Join("\n",rigRows)),animationCurves=Hash(curves.ToString()) };
    }

    public static void CaptureFurBaseline()
    {
        if(File.Exists(FurContractPath))throw new InvalidOperationException("Fur baseline already captured; never overwrite it with revised geometry.");
        Directory.CreateDirectory(Path.GetDirectoryName(FurContractPath));
        File.WriteAllText(FurContractPath,JsonUtility.ToJson(ImportedFurContract(),true)+"\n");
        Debug.Log("AETHEL_WOLF_IMPORTED_FUR_BASELINE_CAPTURED");
    }

    [MenuItem("Echoes of Aethel/Art/Refresh wolf rig export fit (explicit)")]
    public static void RefreshExport()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Refresh requires edit mode outside a build.");
        AssetDatabase.ImportAsset(Model,ImportAssetOptions.ForceUpdate);
        // Preserve clip names/IDs/settings, but follow the explicitly exported take
        // ranges when sampling density changes. Never run from an import hook.
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        var clips=importer.clipAnimations;
        foreach(var clip in clips) {
            var take=importer.defaultClipAnimations.Single(c=>c.takeName==clip.takeName);
            clip.firstFrame=take.firstFrame;clip.lastFrame=take.lastFrame;
        }
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try {
            var visual=root.transform.Find("VisualRoot");
            var model=visual.Find("Model_LoboEtereo");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            model.localScale=source.transform.localScale; model.localPosition=source.transform.localPosition; model.localRotation=source.transform.localRotation;
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
            model.GetComponentInChildren<Animator>().cullingMode=AnimatorCullingMode.AlwaysAnimate;
            float height=FitBindMesh(model,visual,skin);
            var bounds=skin.sharedMesh.bounds;bounds.Expand(bounds.size.magnitude*.7f);skin.localBounds=bounds;
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            var report="docs/validation/wolf-local-fur/unity-import.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            if(!File.Exists(report))File.WriteAllText(report,"Explicit refresh of existing rig; no scene write.\n");
            if(File.Exists(report))File.WriteAllLines(report,File.ReadAllLines(report).Where(l=>!l.StartsWith("Rest geometry height:")).Concat(new[]{"Rest geometry height: "+height.ToString("F5")}));
            Debug.Log("AETHEL_WOLF_EXPORT_FIT_REFRESHED restHeight="+height);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        Audit();
    }

    [MenuItem("Echoes of Aethel/Art/Audit wolf rig")]
    public static void Audit()
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try {
            var animator=root.GetComponentInChildren<Animator>();
            var lines=new System.Collections.Generic.List<string>();
            foreach(var node in animator.GetComponentsInChildren<Transform>())
                lines.Add("NODE "+AnimationUtility.CalculateTransformPath(node,animator.transform)+" scale="+node.localScale+" pos="+node.localPosition);
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"))) {
                lines.Add("CLIP "+clip.name);
                foreach(var b in AnimationUtility.GetCurveBindings(clip).Take(16)) {
                    var c=AnimationUtility.GetEditorCurve(clip,b);
                    lines.Add(b.path+" "+b.propertyName+" keys="+c.length+" min="+c.keys.Min(k=>k.value)+" max="+c.keys.Max(k=>k.value));
                }
                clip.SampleAnimation(animator.gameObject,.02f);
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
                var a=new Mesh();skin.BakeMesh(a);
                clip.SampleAnimation(animator.gameObject,.19f);
                var bmesh=new Mesh();skin.BakeMesh(bmesh);
                lines.Add("SAMPLED_VERTICES_DELTA "+a.vertices.Zip(bmesh.vertices,(x,y)=>(x-y).magnitude).Max());
                Object.DestroyImmediate(a);Object.DestroyImmediate(bmesh);
            }
            Directory.CreateDirectory("docs/validation/wolf-local-fur");
            File.WriteAllLines("docs/validation/wolf-local-fur/binding-audit.txt",lines);
            Debug.Log("AETHEL_WOLF_BINDING_AUDIT_COMPLETE");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    [MenuItem("Echoes of Aethel/Art/Integrate authored wolf rig (explicit)")]
    public static void Integrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Wolf integration requires edit mode outside a build.");
        var scene = File.ReadAllBytes(AethelProjectTools.Arena);
        var current=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if(current.GetComponentInChildren<AethelWolfVisual>(true)!=null)
            throw new InvalidOperationException("Wolf already integrated. Use the explicit export refresh for revised models.");
        var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Run the offline wolf authoring script first.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true; importer.optimizeGameObjects = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.addCollider = false; importer.isReadable = false;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        var imports = importer.defaultClipAnimations;
        foreach (var clip in imports)
        {
            clip.name = clip.name.Split('|').Last();
            clip.loopTime = clip.loopPose = clip.name == "Idle" || clip.name == "Run";
            clip.lockRootRotation = clip.lockRootPositionXZ = clip.lockRootHeightY = true;
        }
        if (!States.All(s => imports.Count(c => c.name == s) == 1))
            throw new InvalidOperationException("Expected six uniquely named clips: " + string.Join(",", imports.Select(c => c.name)));
        importer.clipAnimations = imports; importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            foreach (string name in States)
            {
                var state = controller.layers[0].stateMachine.AddState(name);
                state.motion = clips[name]; state.writeDefaultValues = true;
                if (name == "Idle") controller.layers[0].stateMachine.defaultState = state;
            }
        }
        else foreach (var state in controller.layers[0].stateMachine.states) state.state.motion = clips[state.state.name];
        EditorUtility.SetDirty(controller);

        const string materialPath = "Assets/Art/Materials/MT_WolfTelegraph.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Aethel/Telegraph URP");
            if (shader == null) throw new InvalidOperationException("Telegraph shader missing.");
            material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath);
        }
        var prefab = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var visual = prefab.transform.Find("VisualRoot");
            var existing = visual.GetComponent<AethelWolfVisual>();
            if (existing != null) throw new InvalidOperationException("Wolf already integrated. Edit its visual/rig directly; do not recreate it.");
            var oldAnimator = visual.GetComponent<Animator>();
            if (oldAnimator != null) Object.DestroyImmediate(oldAnimator);
            var oldModel = visual.Find("Model_LoboEtereo");
            if (oldModel == null) throw new InvalidOperationException("Expected original visual child.");
            Object.DestroyImmediate(oldModel.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model), visual);
            model.name = "Model_LoboEtereo";
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid)
                throw new InvalidOperationException("Generic Avatar must be valid.");
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null || skin.bones.Length < 20) throw new InvalidOperationException("Skinned quadruped skeleton missing.");
            skin.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/MT_LoboEtereo_Painted.mat");
            float height=FitBindMesh(model.transform,visual,skin);
            // Conservative local culling bounds include run, jaw and collapse poses. No collider changes.
            var bounds = skin.localBounds; bounds.Expand(bounds.size.magnitude * .7f); skin.localBounds = bounds;
            var cue = new GameObject("AttackTelegraph"); cue.transform.SetParent(visual, false);
            var ring = Line(cue.transform, "ContactOutline", material, .035f);
            var progress = Line(cue.transform, "WindupProgress", material, .04f);
            var ai = prefab.GetComponent<AethelWolfAI>();
            var body = prefab.GetComponent<CharacterController>();
            visual.gameObject.AddComponent<AethelWolfVisual>().Configure(ai, prefab.GetComponent<AethelHealth>(), body, animator, ring, progress, clips["Bite"].length);
            prefab.GetComponent<AethelArtBindings>().Configure(animator, visual, prefab.transform.Find("GroundCheck"), visual.Find("BiteOrigin"), body);
            PrefabUtility.SaveAsPrefabAsset(prefab, Prefab);
            Directory.CreateDirectory("docs/validation/wolf");
            File.WriteAllLines("docs/validation/wolf/unity-import.txt", new[] {
                "Generic Avatar valid: " + animator.avatar.isValid, "Skin bones: " + skin.bones.Length,
                "Skin triangles: " + skin.sharedMesh.triangles.Length / 3,
                "Root motion: " + animator.applyRootMotion,
                "Rest geometry height: " + height.ToString("F5")
            }.Concat(clips.Select(c => c.Key + ": " + c.Value.length.ToString("F5") + " seconds; events=" + AnimationUtility.GetAnimationEvents(c.Value).Length)));
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        if (!scene.SequenceEqual(File.ReadAllBytes(AethelProjectTools.Arena))) throw new InvalidOperationException("Scene changed unexpectedly.");
        Debug.Log("AETHEL_WOLF_RIG_INTEGRATED_SCENE_UNCHANGED");
    }

    static LineRenderer Line(Transform parent, string name, Material material, float width)
    {
        var child = new GameObject(name); child.transform.SetParent(parent, false);
        var line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = material; line.useWorldSpace = true; line.loop = false;
        line.widthMultiplier = width; line.numCapVertices = 3; line.numCornerVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
        line.positionCount = 0; line.enabled = false;
        return line;
    }

    static float FitBindMesh(Transform model,Transform visual,SkinnedMeshRenderer skin)
    {
        // Bind geometry is stable; the FBX's selected take/current pose must not move the feet pivot.
        var bounds=skin.sharedMesh.bounds;
        var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        for(int i=0;i<8;i++) {
            var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            var v=visual.InverseTransformPoint(skin.transform.TransformPoint(corner));
            min=Vector3.Min(min,v);max=Vector3.Max(max,v);
        }
        float height=max.y-min.y;
        float scale=.9f/height; model.localScale*=scale;
        model.localPosition=-(min+max)*.5f*scale+Vector3.up*.45f;
        return height;
    }
}
