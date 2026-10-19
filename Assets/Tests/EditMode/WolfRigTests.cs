using System.Linq;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class WolfRigTests
{
    [Test]
    public void LocalFurEditsPreserveImportedUvRigAndEveryAnimationCurve()
    {
        var before=JsonUtility.FromJson<AethelWolfRigTools.FurContract>(System.IO.File.ReadAllText(AethelWolfRigTools.FurContractPath));
        var after=AethelWolfRigTools.ImportedFurContract();
        Assert.AreEqual(before.uvTriangles,after.uvTriangles,"Imported triangle UV mapping changed.");
        Assert.AreEqual(before.bindRig,after.bindRig,"Imported bones, hierarchy or bind poses changed.");
        Assert.AreEqual(before.animationCurves,after.animationCurves,"Geometry-only edits must not alter clips.");
    }

    [Test]
    public void UpperFurHasNoPawOrLowerLegInfluence()
    {
        WithSkin((animator,skin,visual,baked)=> {
            var points=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
            for(int i=0;i<points.Length;i++) {
                var p=visual.InverseTransformPoint(skin.transform.TransformPoint(points[i]));
                if(p.y<=.31f)continue;
                var w=weights[i];
                int[] bones={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};
                float[] values={w.weight0,w.weight1,w.weight2,w.weight3};
                for(int j=0;j<4;j++)if(values[j]>.00001f) {
                    string name=skin.bones[bones[j]].name;
                    Assert.IsFalse(name.Contains("Paw") || name.Contains("Lower"),"Upper fur vertex "+i+" follows "+name);
                }
            }
        });
    }

    [Test]
    public void ImportedHaunchAndManeStayInsideReviewedStretchLimits()
    {
        WithSkin((animator,skin,visual,baked)=> {
            var rest=skin.sharedMesh.vertices.Select(p=>visual.InverseTransformPoint(skin.transform.TransformPoint(p))).ToArray();
            var triangles=skin.sharedMesh.triangles;
            var edges=new System.Collections.Generic.HashSet<(int,int)>();
            for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                int a=triangles[i+j],b=triangles[i+(j+1)%3];edges.Add((Mathf.Min(a,b),Mathf.Max(a,b)));
            }
            var lines=new System.Collections.Generic.List<string>();
            foreach(string name in new[]{"Run","Windup","Bite","Death"}) {
                float haunch=0,mane=0;var clip=Clip(name);
                for(int step=0;step<=8;step++) {
                    clip.SampleAnimation(animator.gameObject,clip.length*step/8);skin.BakeMesh(baked);
                    var points=baked.vertices.Select(p=>visual.InverseTransformPoint(skin.transform.TransformPoint(p))).ToArray();
                    foreach(var edge in edges) {
                        int a=edge.Item1,b=edge.Item2;float length=(rest[a]-rest[b]).magnitude;if(length<.001f)continue;
                        var center=(rest[a]+rest[b])*.5f;float ratio=(points[a]-points[b]).magnitude/length;
                        if(center.z>-.8f && center.z<-.15f && center.y>.1f)haunch=Mathf.Max(haunch,ratio);
                        if(center.z>.35f && center.y>.18f)mane=Mathf.Max(mane,ratio);
                    }
                }
                lines.Add(name+" haunchMax="+haunch.ToString("F6")+" maneMax="+mane.ToString("F6"));
                Assert.Less(haunch,name=="Run"?3.8f:name=="Death"?2.7f:1.8f,name+" haunch");
                Assert.Less(mane,name=="Run"?4.9f:2.5f,name+" mane");
            }
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllLines("TestResults/wolf-surface-stretch.txt",lines);
        });
    }

    [Test]
    public void RunningSolesTrackThreeMetresPerSecondDuringDiagonalSupport()
    {
        WithSkin((animator,skin,visual,baked)=> {
            var run=Clip("Run");
            foreach(string foot in new[]{"ForePaw.L","HindPaw.R","ForePaw.R","HindPaw.L"}) {
                Clip("Idle").SampleAnimation(animator.gameObject,0);
                var ids=SolePatch(skin,visual,baked,foot);
                float start=run.length*(foot=="ForePaw.L" || foot=="HindPaw.R" ? .08f : .58f);
                run.SampleAnimation(animator.gameObject,start);var a=Centroid(skin,visual,baked,ids);
                run.SampleAnimation(animator.gameObject,start+1f/30f);var b=Centroid(skin,visual,baked,ids);
                Assert.Less((b-a+Vector3.forward*.1f).magnitude,.012f,foot+": support must compensate actual AI travel, not slide with the shell.");
                Assert.Less(Mathf.Abs(b.y-a.y),.006f,foot+": no vertical bounce while supporting.");
            }
        });
    }

    [Test]
    public void AttackSolesStayPlantedAndContactReturnHaveContinuousSkin()
    {
        WithSkin((animator,skin,visual,baked)=> {
            Clip("Idle").SampleAnimation(animator.gameObject,0);
            var patches=new[]{"ForePaw.L","HindPaw.R","ForePaw.R","HindPaw.L"}.Select(n=>SolePatch(skin,visual,baked,n)).ToArray();
            var rest=patches.Select(ids=>Centroid(skin,visual,baked,ids)).ToArray();
            float maximum=0;var lines=new System.Collections.Generic.List<string>();
            foreach(string name in new[]{"Windup","Bite","Recovery"}) {
                var clip=Clip(name);
                for(int step=0;step<=16;step++) {
                    clip.SampleAnimation(animator.gameObject,clip.length*step/16f);
                    for(int i=0;i<patches.Length;i++) {
                        var delta=Centroid(skin,visual,baked,patches[i])-rest[i];
                        maximum=Mathf.Max(maximum,delta.magnitude);
                        lines.Add(name+" sample="+step+" sole="+i+" delta="+delta.ToString("F6"));
                    }
                }
            }
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllLines("TestResults/wolf-polish-contact.txt",lines);
            Assert.Less(maximum,.008f,"Attack maximum sole displacement; see TestResults/wolf-polish-contact.txt");
            AssertSeam(animator,skin,baked,Clip("Windup"),Clip("Windup").length,Clip("Bite"),0,.002f);
            AssertSeam(animator,skin,baked,Clip("Bite"),Clip("Bite").length,Clip("Recovery"),.25f,.002f);
            AssertSeam(animator,skin,baked,Clip("Run"),0,Clip("Run"),Clip("Run").length,.002f);
        });
    }

    static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(AethelWolfRigTools.Model).OfType<AnimationClip>().Single(c=>c.name==name);

    static void WithSkin(System.Action<Animator,SkinnedMeshRenderer,Transform,Mesh> check)
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var baked=new Mesh();
        try {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AethelWolfRigTools.Prefab),scene);
            check(instance.GetComponentInChildren<Animator>(),instance.GetComponentInChildren<SkinnedMeshRenderer>(),instance.transform.Find("VisualRoot"),baked);
        }finally{Object.DestroyImmediate(baked);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
    }

    static int[] SolePatch(SkinnedMeshRenderer skin,Transform visual,Mesh baked,string bone)
    {
        int index=System.Array.FindIndex(skin.bones,b=>b.name==bone);
        var weights=skin.sharedMesh.boneWeights;
        var ids=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==index && weights[i].weight0>.999f).ToArray();
        Assert.Greater(ids.Length,4,bone);
        skin.BakeMesh(baked);var points=baked.vertices;
        float min=ids.Min(i=>visual.InverseTransformPoint(skin.transform.TransformPoint(points[i])).y);
        return ids.Where(i=>visual.InverseTransformPoint(skin.transform.TransformPoint(points[i])).y<min+.015f).ToArray();
    }

    static Vector3 Centroid(SkinnedMeshRenderer skin,Transform visual,Mesh baked,int[] ids)
    {
        skin.BakeMesh(baked);var points=baked.vertices;
        return ids.Aggregate(Vector3.zero,(sum,i)=>sum+visual.InverseTransformPoint(skin.transform.TransformPoint(points[i])))/ids.Length;
    }

    static void AssertSeam(Animator animator,SkinnedMeshRenderer skin,Mesh baked,AnimationClip a,float at,AnimationClip b,float bt,float tolerance)
    {
        a.SampleAnimation(animator.gameObject,at);skin.BakeMesh(baked);var first=baked.vertices;
        b.SampleAnimation(animator.gameObject,bt);skin.BakeMesh(baked);var second=baked.vertices;
        float delta=first.Zip(second,(x,y)=>skin.transform.TransformVector(x-y).magnitude).Max();
        Assert.Less(delta,tolerance,a.name+" -> "+b.name+" seam (metres)");
    }

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
            Clip("Idle").SampleAnimation(instance.GetComponentInChildren<Animator>().gameObject,0);
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
