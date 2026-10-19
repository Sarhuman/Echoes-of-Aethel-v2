using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SavedArenaTests
{
    [Test]
    public void SavedArenaOpensWithValidReferences() { AethelProjectTools.Validate(); }

    [Test]
    public void InitialCreatorRefusesToChangeExistingSceneAndPrefabs()
    {
        Assert.IsTrue(File.Exists(AethelProjectTools.Arena));
        var files = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".meta")).ToArray();
        var before = files.ToDictionary(p => p, Hash);
        AethelFirstArenaCreator.Create();
        foreach (var entry in before) Assert.AreEqual(entry.Value, Hash(entry.Key), entry.Key);
    }

    [TestCase("PF_Bastiao", true)]
    [TestCase("PF_LoboEtereo", false)]
    public void CharacterPrefabsPreserveIntegrationContract(string name, bool player)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/" + name + ".prefab");
        Assert.IsNotNull(prefab);
        Assert.IsNotNull(prefab.GetComponent<CharacterController>());
        Assert.IsNotNull(prefab.GetComponent<AethelHealth>());
        Assert.IsNotNull(prefab.GetComponent<AethelArtBindings>());
        var visual = prefab.transform.Find("VisualRoot"); Assert.IsNotNull(visual);
        Assert.IsNotNull(visual.GetComponentInChildren<Animator>());
        Assert.IsFalse(visual.GetComponentInChildren<Animator>().applyRootMotion);
        Assert.IsEmpty(visual.GetComponentsInChildren<Collider>(true));
        Assert.IsNotNull(prefab.transform.Find("GroundCheck"));
        if (player)
        {
            Assert.IsNotNull(prefab.GetComponent<AethelPlayerController>());
            var names = visual.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
            CollectionAssert.Contains(names, "WeaponSocket"); CollectionAssert.Contains(names, "ShieldSocket");
            Assert.IsNotNull(visual.Find("HitOrigin"));
        }
        else { Assert.IsNotNull(prefab.GetComponent<AethelWolfAI>()); Assert.IsNotNull(visual.Find("BiteOrigin")); }
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
}
