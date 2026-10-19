using System.Collections;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ImportedArtRuntimeTests
{
    [UnityTest]
    public IEnumerator SuppliedWalkFollowsExistingMotorAndPausesDuringDash()
    {
        var original=InputSystem.settings;
        var settings=ScriptableObject.CreateInstance<InputSettings>();
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings=settings;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try {
            yield return SceneManager.LoadSceneAsync("Aethel_FirstArena");
            yield return new WaitForSeconds(.4f);
            var player=Object.FindFirstObjectByType<AethelPlayerController>();
            var presentation=player.GetComponentInChildren<AethelLocomotionVisual>();
            var animator=player.GetComponentInChildren<Animator>();
            Assert.IsNotNull(presentation); Assert.IsFalse(animator.applyRootMotion);
            Vector3 modelPosition=animator.transform.localPosition;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            yield return new WaitForSeconds(.25f);
            Assert.IsTrue(presentation.IsLocomotionPlaying);
            Assert.Greater(animator.speed,0);
            Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,0);
            Assert.AreEqual(modelPosition,animator.transform.localPosition,"Imported root motion must not displace the visual.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Space));
            yield return new WaitForSeconds(.05f);
            yield return null; // Observe presentation after the motor frame's LateUpdate.
            Assert.IsTrue(player.IsDashing); Assert.IsFalse(presentation.IsLocomotionPlaying);
            Assert.AreEqual(0,animator.speed);
            Assert.IsEmpty(player.transform.Find("VisualRoot").GetComponentsInChildren<Collider>());
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings=original!=null?original:ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(settings);
        }
    }

    [UnityTest]
    public IEnumerator SavedCharacterPrefabsStillApplyOneHitAndWolfBite()
    {
        var original=InputSystem.settings;
        var settings=ScriptableObject.CreateInstance<InputSettings>();
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings=settings;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try {
            yield return SceneManager.LoadSceneAsync("Aethel_FirstArena");
            var wolf=Object.FindFirstObjectByType<AethelWolfAI>(); wolf.enabled=false;
            yield return new WaitForSeconds(.4f);
            var player=Object.FindFirstObjectByType<AethelPlayerController>();
            player.transform.Find("VisualRoot").rotation=Quaternion.identity;
            wolf.transform.position=player.transform.position+Vector3.forward*1.3f;
            wolf.transform.Find("VisualRoot").rotation=Quaternion.LookRotation(Vector3.back);
            Physics.SyncTransforms();
            var wolfLife=wolf.GetComponent<AethelHealth>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(75,wolfLife.Current,"One damage application despite rig and equipped meshes.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(75,wolfLife.Current);
            wolf.enabled=true;
            yield return new WaitForSeconds(.65f);
            Assert.AreEqual(85,player.GetComponent<AethelHealth>().Current,"Existing wolf windup still applies bite damage.");
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings=original!=null?original:ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(settings);
        }
    }
}
