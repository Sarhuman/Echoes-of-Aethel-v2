using System.Linq;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ImportedArtTests
{
    [Test]
    public void BastiaoHasValidRigAndSocketsWithoutMovingDamageOriginOntoBones()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AethelBastiaoImport.PrefabPath);
        var animator=prefab.GetComponentInChildren<Animator>();
        Assert.IsTrue(animator.avatar.isValid && animator.avatar.isHuman);
        Assert.IsNotNull(animator.runtimeAnimatorController);
        Assert.IsFalse(animator.applyRootMotion);
        var nodes=prefab.GetComponentsInChildren<Transform>(true);
        Assert.AreEqual("RightHand",nodes.Single(t=>t.name=="WeaponSocket").parent.name);
        Assert.AreEqual("LeftHand",nodes.Single(t=>t.name=="ShieldSocket").parent.name);
        Assert.AreSame(prefab.transform.Find("VisualRoot/HitOrigin"),prefab.GetComponent<AethelPlayerController>().HitOrigin);
        Assert.AreEqual(new Vector3(0,1,.99f),prefab.GetComponent<AethelPlayerController>().HitOrigin.localPosition);
        Assert.AreEqual(1,prefab.GetComponentsInChildren<AethelPlayerController>(true).Length);
        Assert.AreEqual(1,prefab.GetComponentsInChildren<AethelHealth>(true).Length);
        Assert.AreEqual(1,prefab.GetComponentsInChildren<Collider>(true).Length);
        Assert.IsNotNull(prefab.GetComponentInChildren<AethelLocomotionVisual>());
        var clips=AssetDatabase.LoadAllAssetsAtPath(AethelBastiaoImport.ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        Assert.AreEqual(1,clips.Length); Assert.AreEqual("Walking",clips[0].name);
        Assert.IsEmpty(AnimationUtility.GetAnimationEvents(clips[0]),"Animation must not apply duplicate combat damage.");
    }

    [TestCase("Escudo",5100)] [TestCase("Arvore",15100)] [TestCase("Rocha",6100)]
    [TestCase("Pilar",8100)] [TestCase("Piso",4100)] [TestCase("LoboEtereo",5200)] [TestCase("Machado",3200)]
    public void RuntimeModelsRespectTriangleBudgetsAndIntroduceNoPhysics(string asset,int budget)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Exported/MD_"+asset+".fbx");
        Assert.IsNotNull(model);
        Assert.LessOrEqual(model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3),budget);
        Assert.IsEmpty(model.GetComponentsInChildren<Collider>(true));
        Assert.IsEmpty(model.GetComponentsInChildren<AethelHealth>(true));
    }

    [TestCase("PF_BosqueArvore_01")] [TestCase("PF_BosqueRocha_01")] [TestCase("PF_BosquePilar_01")] [TestCase("PF_BosquePiso_01")]
    public void EnvironmentHasSeparateCollisionAndTexturedVisual(string name)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/"+name+".prefab");
        Assert.IsNotNull(prefab.transform.Find("Collision").GetComponent<BoxCollider>());
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>()) {
            Assert.IsNull(renderer.GetComponent<Collider>());
            Assert.AreEqual("Aethel/Painted URP",renderer.sharedMaterial.shader.name);
            Assert.IsNotNull(renderer.sharedMaterial.mainTexture);
        }
    }
}
