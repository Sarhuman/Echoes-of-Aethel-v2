using System.Linq;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class WolfRigTests
{
    [Test]
    public void RepeatingInitialIntegrationRefusesBeforeEditingAssets()
    {
        var before=System.IO.File.ReadAllBytes(AethelWolfRigTools.Prefab);
        Assert.Throws<System.InvalidOperationException>(()=>AethelWolfRigTools.Integrate());
        CollectionAssert.AreEqual(before,System.IO.File.ReadAllBytes(AethelWolfRigTools.Prefab));
    }

    [Test]
    public void IdleFeetRemainAtStableVisualOrigin()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var mesh=new Mesh();
        try {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AethelWolfRigTools.Prefab),scene);
            var animator=instance.GetComponentInChildren<Animator>();
            var idle=AssetDatabase.LoadAllAssetsAtPath(AethelWolfRigTools.Model).OfType<AnimationClip>().Single(c=>c.name=="Idle");
            idle.SampleAnimation(animator.gameObject,0);
            var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh);
            var visual=instance.transform.Find("VisualRoot");
            float lowest=mesh.vertices.Min(v=>visual.InverseTransformPoint(skin.transform.TransformPoint(v)).y);
            Assert.That(lowest,Is.InRange(-.025f,.025f),"Fit the bind mesh, not the FBX's selected attack pose.");
        }finally{Object.DestroyImmediate(mesh);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
    }

    [Test]
    public void SourceRigImportsAtCanonicalScaleBeforePrefabFitting()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var baked=new Mesh();
        try {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AethelWolfRigTools.Model),scene);
            var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>(); skin.BakeMesh(baked);
            var points=baked.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();
            float height=points.Max(v=>v.y)-points.Min(v=>v.y);
            Assert.That(height,Is.InRange(.85f,1f),"Do not conceal FBX skin/bone unit mismatches with prefab scale.");
        }finally{
            Object.DestroyImmediate(baked); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void LegacyStaticFitRefusesToRewriteRiggedWolf()
    {
        var before=System.IO.File.ReadAllBytes(AethelWolfRigTools.Prefab);
        Assert.Throws<System.InvalidOperationException>(()=>AethelAssetIntegration.RefineVisualFit());
        CollectionAssert.AreEqual(before,System.IO.File.ReadAllBytes(AethelWolfRigTools.Prefab));
    }

    [Test]
    public void WolfHasSkinnedGenericRigAndSixClipsWithoutDamageEvents()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AethelWolfRigTools.Prefab);
        var animator = prefab.GetComponentInChildren<Animator>();
        Assert.IsTrue(animator.avatar.isValid); Assert.IsFalse(animator.avatar.isHuman);
        Assert.IsFalse(animator.applyRootMotion);
        Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate,animator.cullingMode,"Windup pose must remain in sync even before the camera sees the wolf.");
        var skin = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.AreEqual(24, skin.bones.Length);
        Assert.AreEqual(5164, skin.sharedMesh.triangles.Length / 3);
        Assert.IsTrue(skin.sharedMesh.boneWeights.All(w => Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1f)<.001f));
        var clips = AssetDatabase.LoadAllAssetsAtPath(AethelWolfRigTools.Model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        CollectionAssert.AreEquivalent(new[]{"Idle","Run","Windup","Bite","Recovery","Death"}, clips.Select(c=>c.name));
        foreach(var clip in clips) {
            Assert.IsEmpty(AnimationUtility.GetAnimationEvents(clip),clip.name);
            Assert.Greater(clip.length,.15f);
            Assert.IsTrue(AnimationUtility.GetCurveBindings(clip).Any(b=>b.path.Contains("Root")),clip.name+" must animate the skeleton.");
        }
        var controller = (AnimatorController)animator.runtimeAnimatorController;
        Assert.AreEqual(6,controller.layers[0].stateMachine.states.Length);
        Assert.IsTrue(controller.layers[0].stateMachine.states.All(s=>s.state.motion!=null && s.state.transitions.Length==0));
    }

    [Test]
    public void TelegraphAndRigKeepDamageOriginAndPhysicsOnStableShell()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AethelWolfRigTools.Prefab);
        var visual = prefab.transform.Find("VisualRoot");
        Assert.IsNotNull(visual.GetComponent<AethelWolfVisual>(),"Presentation must survive model-child replacement.");
        Assert.AreSame(visual.Find("BiteOrigin"),prefab.GetComponent<AethelWolfAI>().BiteOrigin);
        Assert.AreEqual(new Vector3(0,.75f,1),visual.Find("BiteOrigin").localPosition);
        Assert.IsEmpty(visual.GetComponentsInChildren<Collider>(true));
        Assert.AreEqual(1,prefab.GetComponentsInChildren<AethelWolfAI>(true).Length);
        Assert.AreEqual(1,prefab.GetComponentsInChildren<AethelHealth>(true).Length);
        Assert.AreEqual(1,prefab.GetComponentsInChildren<Collider>(true).Length);
        var lines=visual.GetComponentsInChildren<LineRenderer>(true);
        Assert.AreEqual(2,lines.Length);
        foreach(var line in lines) {
            Assert.IsFalse(line.enabled,"Cue must be hidden before play.");
            Assert.AreEqual("Aethel/Telegraph URP",line.sharedMaterial.shader.name);
        }
    }
}
