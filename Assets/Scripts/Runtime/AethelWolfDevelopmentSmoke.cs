#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoesOfAethel.V2
{
    // Opt-in build diagnostic only. Normal game launches never reposition or disable actors.
    public sealed class AethelWolfDevelopmentSmoke : MonoBehaviour
    {
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-aethel-wolf-smoke")>=0)
                new GameObject("Wolf development smoke").AddComponent<AethelWolfDevelopmentSmoke>();
        }
        void OnEnable(){Application.logMessageReceived+=WatchErrors;}
        void OnDisable(){Application.logMessageReceived-=WatchErrors;}
        void WatchErrors(string message,string trace,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)failed=true;
        }
        void Check(bool condition,string message)
        {
            if(!condition){failed=true;Debug.LogError("AETHEL_WOLF_SMOKE_FAILURE: "+message);}
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            var player=FindFirstObjectByType<AethelPlayerController>();
            var ai=FindFirstObjectByType<AethelWolfAI>();
            Check(player!=null && ai!=null && Camera.main!=null,"Saved arena references");
            if(failed){Application.Quit(1);yield break;}
            player.enabled=false;ai.enabled=false;
            while(!SplashScreen.isFinished)yield return null;
            var visual=ai.GetComponentInChildren<AethelWolfVisual>();
            var animator=ai.GetComponentInChildren<Animator>();
            var skin=ai.GetComponentInChildren<SkinnedMeshRenderer>();
            Check(visual!=null && animator!=null && skin!=null,"Rig/presentation references");
            if(failed){Application.Quit(1);yield break;}
            ai.transform.position=new Vector3(0,.08f,0);
            ai.transform.Find("VisualRoot").rotation=Quaternion.identity;
            player.transform.position=new Vector3(0,.08f,1.45f);
            ai.Configure(player.transform);Physics.SyncTransforms();
            var camera=Camera.main;camera.GetComponent<AethelIsometricCamera>().enabled=false;
            camera.orthographicSize=2.1f;
            camera.transform.position=new Vector3(4,3.08f,-2.5f);
            camera.transform.LookAt(new Vector3(0,.48f,.5f));
            var jaw=Array.Find(skin.bones,b=>b.name=="Jaw");
            Check(jaw!=null,"Jaw bone");
            if(failed){Application.Quit(1);yield break;}
            var initial=jaw.localRotation;
            float health=player.GetComponent<AethelHealth>().Current;
            ai.enabled=true;
            float limit=Time.time+1.2f;
            while((!ai.IsWindingUp || ai.WindupProgress<.6f) && Time.time<limit)yield return null;
            yield return null;
            Check(ai.IsWindingUp && visual.TelegraphVisible,"Synchronized windup cue");
            Check(Quaternion.Angle(initial,jaw.localRotation)>3f,"Skinned jaw animation");
            Check(player.GetComponent<AethelHealth>().Current==health,"No early animation damage");
            Capture("wolf-windup-runtime.png");
            while(ai.IsWindingUp && Time.time<limit)yield return null;
            yield return null;
            Check(!ai.IsWindingUp && !visual.TelegraphVisible,"Cue ends at contact");
            Check(visual.CurrentState=="Bite","Bite state");
            Check(player.GetComponent<AethelHealth>().Current==health-15f,"Existing single damage application");
            Capture("wolf-bite-runtime.png");
            yield return new WaitForSeconds(.25f);
            Check(player.GetComponent<AethelHealth>().Current==health-15f,"No duplicate animation damage");
            Debug.Log("AETHEL_WOLF_SMOKE_HARDWARE: "+SystemInfo.processorType+"; "+SystemInfo.graphicsDeviceName+"; RAM="+SystemInfo.systemMemorySize+"MB");
            Debug.Log(failed?"AETHEL_WOLF_BUILD_SMOKE_FAILED":"AETHEL_WOLF_BUILD_SMOKE_PASSED");
            Application.Quit(failed?1:0);
        }
        void Capture(string name)
        {
            var target=new RenderTexture(1920,1080,24);target.Create();
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"..",name)),image.EncodeToPNG());
            RenderTexture.active=previous;target.Release();Destroy(target);Destroy(image);
        }
    }
}
#endif
