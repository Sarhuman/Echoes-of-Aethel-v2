using System.Collections;
using System.IO;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class WolfVisualTests
{
    AethelWolfAI ai;
    AethelWolfVisual visual;
    AethelPlayerController player;
    AethelHealth life;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return SceneManager.LoadSceneAsync("Aethel_FirstArena");
        ai=Object.FindFirstObjectByType<AethelWolfAI>(); ai.enabled=false;
        player=Object.FindFirstObjectByType<AethelPlayerController>(); player.enabled=false;
        visual=ai.GetComponentInChildren<AethelWolfVisual>();
        life=player.GetComponent<AethelHealth>();
        ai.transform.position=new Vector3(0,.08f,0);
        ai.transform.Find("VisualRoot").rotation=Quaternion.identity;
        player.transform.position=new Vector3(0,.08f,1.45f);
        ai.Configure(player.transform); Physics.SyncTransforms();
        yield return null;
    }

    IEnumerator UntilWindupEnds()
    {
        float limit=Time.time+1.2f;
        while(ai.IsWindingUp && Time.time<limit) yield return null;
        Assert.IsFalse(ai.IsWindingUp,"Existing AI windup deadline must complete.");
        yield return null; // Observe presentation after LateUpdate.
    }

    [UnityTest]
    public IEnumerator SkeletonRunsTelegraphsContactsRecoversAndDies()
    {
        player.transform.position=new Vector3(0,.08f,5); Physics.SyncTransforms(); ai.enabled=true;
        yield return new WaitForSeconds(.12f); yield return null;
        Assert.AreEqual("Run",visual.CurrentState);
        var skin=ai.GetComponentInChildren<SkinnedMeshRenderer>();
        var animator=ai.GetComponentInChildren<Animator>();
        var fore=System.Array.Find(skin.bones,b=>b.name=="ForeUpper.L");
        Debug.Log("AETHEL_RUN_BEFORE normalized="+animator.GetCurrentAnimatorStateInfo(0).normalizedTime+" speed="+animator.speed+" enabled="+animator.enabled+" culling="+animator.cullingMode+" bone="+fore.localRotation+" skinScale="+skin.transform.lossyScale);
        var meshA=new Mesh(); skin.BakeMesh(meshA);
        yield return new WaitForSeconds(.08f);
        Debug.Log("AETHEL_RUN_AFTER normalized="+animator.GetCurrentAnimatorStateInfo(0).normalizedTime+" speed="+animator.speed+" bone="+fore.localRotation+" stateRun="+animator.GetCurrentAnimatorStateInfo(0).IsName("Run"));
        var meshB=new Mesh(); skin.BakeMesh(meshB);
        var first=meshA.vertices; var second=meshB.vertices;
        bool deformed=false;
        for(int i=0;i<first.Length;i++) if(skin.transform.TransformVector(first[i]-second[i]).sqrMagnitude>.000001f){deformed=true;break;}
        Object.Destroy(meshA); Object.Destroy(meshB);
        ConfigureDiagnosticCamera();Capture("run-runtime.png");
        Assert.IsTrue(deformed,"Run must deform the supplied mesh, not just move a static visual.");
        ai.enabled=false; ai.transform.position=new Vector3(0,.08f,0);
        player.transform.position=new Vector3(0,.08f,1.45f);
        ai.transform.Find("VisualRoot").rotation=Quaternion.identity;
        Physics.SyncTransforms(); ai.enabled=true;
        yield return null; yield return null;
        Assert.IsTrue(ai.IsWindingUp); Assert.IsTrue(visual.TelegraphVisible);
        Assert.AreEqual("Windup",visual.CurrentState);
        Assert.AreEqual(100,life.Current,"No damage during anticipation.");
        float limit=Time.time+.4f;
        while(ai.WindupProgress<.55f && ai.IsWindingUp && Time.time<limit) yield return null;
        yield return null;
        Assert.Less(Mathf.Abs(ai.WindupProgress-visual.DisplayedWindupProgress),.16f);
        ConfigureDiagnosticCamera(); Capture("windup-runtime.png");
        yield return UntilWindupEnds();
        Assert.AreEqual("Bite",visual.CurrentState); Assert.IsFalse(visual.TelegraphVisible);
        Assert.AreEqual(85,life.Current,"Only the existing completion frame applies damage.");
        Capture("bite-runtime.png");
        yield return new WaitForSeconds(.25f); yield return null;
        Assert.AreEqual("Recovery",visual.CurrentState); Assert.AreEqual(85,life.Current);
        ai.GetComponent<AethelHealth>().TakeDamage(100);
        yield return null; yield return null;
        Assert.AreEqual("Death",visual.CurrentState); Assert.IsFalse(visual.TelegraphVisible);
        yield return new WaitForSeconds(.8f); Capture("death-runtime.png");
    }

    [UnityTest]
    public IEnumerator DisablingPresentationHidesCueWithoutCancellingDamage()
    {
        ai.enabled=true; yield return null; yield return null;
        Assert.IsTrue(visual.TelegraphVisible);
        visual.enabled=false; Assert.IsFalse(visual.TelegraphVisible);
        yield return UntilWindupEnds();
        Assert.AreEqual(85,life.Current);
        yield return new WaitForSeconds(.2f); Assert.AreEqual(85,life.Current);
    }

    [UnityTest]
    public IEnumerator InvulnerabilityBlocksDamageWhileAnimationCompletes()
    {
        life.GrantInvulnerability(.9f); ai.enabled=true;
        yield return null; yield return null;
        Assert.IsTrue(visual.TelegraphVisible);
        yield return UntilWindupEnds();
        Assert.AreEqual(100,life.Current); Assert.AreEqual("Bite",visual.CurrentState);
        Assert.IsFalse(visual.TelegraphVisible);
    }

    [UnityTest]
    public IEnumerator LeavingContactRangeDuringTelegraphAvoidsDamage()
    {
        ai.enabled=true; yield return null; yield return null;
        Assert.IsTrue(visual.TelegraphVisible);
        player.transform.position=new Vector3(0,.08f,5); Physics.SyncTransforms();
        yield return UntilWindupEnds();
        Assert.AreEqual(100,life.Current); Assert.IsFalse(visual.TelegraphVisible);
    }

    [UnityTest]
    public IEnumerator TargetDeathHidesPendingWindupPresentation()
    {
        ai.enabled=true; yield return null; yield return null;
        life.TakeDamage(100);
        yield return null; yield return null;
        Assert.IsFalse(visual.TelegraphVisible); Assert.AreEqual("Idle",visual.CurrentState);
        // The original AI retains its pending windup on a dead target. Presentation must not revive it.
        Assert.IsTrue(ai.IsWindingUp);
        life.TakeDamage(100); Assert.IsFalse(visual.TelegraphVisible);
    }

    [UnityTest]
    public IEnumerator KillingBiteStillPlaysContactBeforeReturningToIdle()
    {
        life.TakeDamage(90); ai.enabled=true;
        yield return null; yield return null;
        yield return UntilWindupEnds();
        Assert.IsTrue(life.IsDead); Assert.AreEqual("Bite",visual.CurrentState);
        Assert.IsFalse(visual.TelegraphVisible);
        yield return new WaitForSeconds(.25f); yield return null;
        Assert.AreEqual("Idle",visual.CurrentState);
    }

    void ConfigureDiagnosticCamera()
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        var camera=Camera.main;
        camera.GetComponent<AethelIsometricCamera>().enabled=false;
        camera.orthographicSize=2.1f;
        camera.transform.position=ai.transform.position+new Vector3(4,3,-2.5f);
        camera.transform.LookAt(ai.transform.position+new Vector3(0,.4f,.5f));
    }

    static void Capture(string file)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        Directory.CreateDirectory("TestResults/Wolf");
        var target=new RenderTexture(1920,1080,24); target.Create();
        RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
        File.WriteAllBytes("TestResults/Wolf/"+file,image.EncodeToPNG());
        RenderTexture.active=previous; target.Release(); Object.Destroy(target); Object.Destroy(image);
    }
}
