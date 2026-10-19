using System.IO;
using EchoesOfAethel.V2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates a DEMO scene once. Never regenerates it during normal builds.
public static class AethelFirstArenaCreator
{
    private const string ScenePath = "Assets/Scenes/Aethel_FirstArena.unity";

    [MenuItem("Echoes of Aethel/Create First Arena (one time)")]
    public static void Create()
    {
        if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling)
            throw new System.InvalidOperationException("Arena creation is forbidden during compilation/build.");
        if (File.Exists(ScenePath))
        {
            Debug.Log("AETHEL_SCENE_PROTECTED: existing arena was not overwritten.");
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists("Assets/Prefabs/Characters/PF_Bastiao.prefab") || File.Exists("Assets/Prefabs/Characters/PF_LoboEtereo.prefab"))
            throw new System.InvalidOperationException("Character prefabs already exist. Assemble a new scene manually; existing prefabs are protected.");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material floor = MakeMaterial("AethelFloor", new Color(.16f, .22f, .22f));
        Material stone = MakeMaterial("AethelStone", new Color(.28f, .31f, .40f));
        Material hero = MakeMaterial("AethelHeroPrototype", new Color(.55f, .57f, .70f));
        Material wolf = MakeMaterial("AethelWolfPrototype", new Color(.30f, .59f, .75f));
        Material amber = MakeMaterial("AethelAmber", new Color(.90f, .49f, .18f));

        // Integration fixtures, explicitly NOT finished environment art.
        EnvironmentPrefab("PF_BosquePilar_01", PrimitiveType.Cylinder, new Vector3(1f, 2f, 1f), stone, new Vector3(-7f, 2f, 5f));
        EnvironmentPrefab("PF_BosqueRocha_01", PrimitiveType.Sphere, new Vector3(2f, 1.4f, 1.5f), stone, new Vector3(6f, .7f, 6f));
        EnvironmentPrefab("PF_BosqueArvore_01", PrimitiveType.Cylinder, new Vector3(.8f, 3f, .8f), floor, new Vector3(-7f, 3f, -6f));

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "ArenaFloor";
        ground.transform.position = new Vector3(0f, -.25f, 0f);
        ground.transform.localScale = new Vector3(20f, .5f, 20f);
        ground.GetComponent<Renderer>().sharedMaterial = floor;

        // Art boundaries are visual placeholders. Ground and collision objects
        // are independent, so later art imports cannot silently alter physics.
        for (int i = 0; i < 4; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "ArenaBound_" + i;
            wall.transform.position = i < 2
                ? new Vector3(i == 0 ? -10f : 10f, 1f, 0f)
                : new Vector3(0f, 1f, i == 2 ? -10f : 10f);
            wall.transform.localScale = i < 2 ? new Vector3(.7f, 2f, 20f)
                : new Vector3(20f, 2f, .7f);
            wall.GetComponent<Renderer>().sharedMaterial = stone;
        }

        GameObject player = new GameObject("Bastiao_Player");
        player.transform.position = new Vector3(0f, 1.1f, -4f);
        CharacterController pc = player.AddComponent<CharacterController>();
        pc.height = 2f;
        pc.radius = .38f;
        pc.center = Vector3.up;
        player.AddComponent<AethelHealth>();
        AethelPlayerController motor = player.AddComponent<AethelPlayerController>();
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "Visual_PLACEHOLDER_replace_with_rigged_Bastiao";
        Transform playerVisual = Child(player.transform, "VisualRoot", Vector3.zero);
        model.transform.SetParent(playerVisual, false);
        model.transform.localPosition = Vector3.up;
        motor.ConfigureVisual(playerVisual);
        Transform hitOrigin = Child(playerVisual, "HitOrigin", new Vector3(0f, 1f, .99f));
        motor.ConfigureHitOrigin(hitOrigin);
        Animator playerAnimator = playerVisual.gameObject.AddComponent<Animator>();
        playerAnimator.applyRootMotion = false;
        player.AddComponent<AethelArtBindings>().Configure(playerAnimator, playerVisual,
            Child(player.transform, "GroundCheck", Vector3.zero), hitOrigin, pc,
            Child(playerVisual, "WeaponSocket", new Vector3(.5f, 1f, .3f)),
            Child(playerVisual, "ShieldSocket", new Vector3(-.5f, 1f, .2f)));
        model.GetComponent<Renderer>().sharedMaterial = hero;
        Object.DestroyImmediate(model.GetComponent<Collider>());

        GameObject enemy = new GameObject("Lobo_Etereo");
        enemy.transform.position = new Vector3(1.5f, 1.1f, 4f);
        CharacterController wc = enemy.AddComponent<CharacterController>();
        wc.height = 1.5f;
        wc.radius = .45f;
        wc.center = Vector3.up * .75f;
        enemy.AddComponent<AethelHealth>();
        AethelWolfAI ai = enemy.AddComponent<AethelWolfAI>();
        ai.Configure(player.transform);
        GameObject wolfVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        wolfVisual.name = "Visual_PLACEHOLDER_replace_with_rigged_Wolf";
        Transform wolfRoot = Child(enemy.transform, "VisualRoot", Vector3.zero);
        wolfVisual.transform.SetParent(wolfRoot, false);
        wolfVisual.transform.localPosition = Vector3.up * .75f;
        wolfVisual.transform.localScale = new Vector3(1.2f, .7f, 1.6f);
        wolfVisual.GetComponent<Renderer>().sharedMaterial = wolf;
        Object.DestroyImmediate(wolfVisual.GetComponent<Collider>());
        Transform bite = Child(wolfRoot, "BiteOrigin", new Vector3(0f, .75f, 1f));
        ai.ConfigureVisual(wolfRoot, bite);
        Animator wolfAnimator = wolfRoot.gameObject.AddComponent<Animator>();
        wolfAnimator.applyRootMotion = false;
        enemy.AddComponent<AethelArtBindings>().Configure(wolfAnimator, wolfRoot,
            Child(enemy.transform, "GroundCheck", Vector3.zero), bite, wc);

        // Physics and gameplay remain on roots. Replace only visual children with authored rigs.
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Characters")) AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");
        PrefabUtility.SaveAsPrefabAssetAndConnect(player, "Assets/Prefabs/Characters/PF_Bastiao.prefab", InteractionMode.AutomatedAction);
        // Scene target references belong to the instance, never the prefab asset.
        ai.Configure(null);
        PrefabUtility.SaveAsPrefabAssetAndConnect(enemy, "Assets/Prefabs/Characters/PF_LoboEtereo.prefab", InteractionMode.AutomatedAction);
        ai.Configure(player.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ai);

        GameObject camObject = new GameObject("IsometricCamera");
        camObject.tag = "MainCamera";
        Camera camera = camObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.085f, .10f, .16f);
        camObject.AddComponent<AudioListener>();
        camObject.AddComponent<AethelIsometricCamera>().Configure(player.transform);
        motor.ConfigureCamera(camObject.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(motor);

        GameObject sunObject = new GameObject("CoolKeyLight");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(.68f, .76f, .96f);
        sun.intensity = 1.15f;
        sunObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torch.name = "WarmAccent_" + side;
            torch.transform.position = new Vector3(side * 7.5f, 1f, 3f);
            torch.transform.localScale = new Vector3(.25f, 1f, .25f);
            torch.GetComponent<Renderer>().sharedMaterial = amber;
            Light accent = torch.AddComponent<Light>();
            accent.type = LightType.Point;
            accent.range = 6f;
            accent.intensity = 1.5f;
            accent.color = new Color(1f, .55f, .22f);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("AETHEL_V2_FIRST_ARENA_CREATED " + ScenePath
            + " (no automatic regeneration; prototype meshes are NOT final art)");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        const string root = "Assets/Art/Materials";
        if (!AssetDatabase.IsValidFolder(root))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        string path = root + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Aethel/Painted URP");
        if (shader == null)
            throw new System.InvalidOperationException(
                "Aethel/Painted URP shader unavailable. Import Assets/Art/Shaders and configure URP first.");
        Material material = new Material(shader);
        material.name = name;
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnvironmentPrefab(string name, PrimitiveType shape, Vector3 size, Material material, Vector3 position)
    {
        const string folder = "Assets/Prefabs/Environment";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Environment");
        string path = folder + "/" + name + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject root;
        if (prefab != null) root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
        {
            root = new GameObject(name);
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = "Visual_PLACEHOLDER"; visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = size;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var physics = new GameObject("Collision"); physics.transform.SetParent(root.transform, false);
            physics.AddComponent<BoxCollider>().size = new Vector3(size.x, shape == PrimitiveType.Cylinder ? size.y * 2 : size.y, size.z);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
        }
        root.transform.position = position;
    }

    private static Transform Child(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.localPosition = position;
        return child;
    }
}
