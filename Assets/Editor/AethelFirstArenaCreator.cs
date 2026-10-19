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
        if (File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Scene protected",
                "The first arena already exists. It was NOT overwritten. Edit the existing scene or create a separate scene manually.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material floor = MakeMaterial("AethelFloor", new Color(.16f, .22f, .22f));
        Material stone = MakeMaterial("AethelStone", new Color(.28f, .31f, .40f));
        Material hero = MakeMaterial("AethelHeroPrototype", new Color(.55f, .57f, .70f));
        Material wolf = MakeMaterial("AethelWolfPrototype", new Color(.30f, .59f, .75f));
        Material amber = MakeMaterial("AethelAmber", new Color(.90f, .49f, .18f));

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
        pc.center = Vector3.zero;
        player.AddComponent<AethelHealth>();
        AethelPlayerController motor = player.AddComponent<AethelPlayerController>();
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "Visual_PLACEHOLDER_replace_with_rigged_Bastiao";
        model.transform.SetParent(player.transform, false);
        model.GetComponent<Renderer>().sharedMaterial = hero;
        Object.DestroyImmediate(model.GetComponent<Collider>());

        GameObject enemy = new GameObject("Lobo_Etereo");
        enemy.transform.position = new Vector3(1.5f, 1.1f, 4f);
        CharacterController wc = enemy.AddComponent<CharacterController>();
        wc.height = 1.5f;
        wc.radius = .45f;
        enemy.AddComponent<AethelHealth>();
        AethelWolfAI ai = enemy.AddComponent<AethelWolfAI>();
        ai.Configure(player.transform);
        GameObject wolfVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        wolfVisual.name = "Visual_PLACEHOLDER_replace_with_rigged_Wolf";
        wolfVisual.transform.SetParent(enemy.transform, false);
        wolfVisual.transform.localScale = new Vector3(1.2f, .7f, 1.6f);
        wolfVisual.GetComponent<Renderer>().sharedMaterial = wolf;
        Object.DestroyImmediate(wolfVisual.GetComponent<Collider>());

        GameObject camObject = new GameObject("IsometricCamera");
        camObject.tag = "MainCamera";
        Camera camera = camObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.085f, .10f, .16f);
        camObject.AddComponent<AethelIsometricCamera>().Configure(player.transform);

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
        const string root = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(root))
            AssetDatabase.CreateFolder("Assets", "Materials");
        string path = root + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new System.InvalidOperationException(
                "Universal Render Pipeline/Lit shader unavailable. Create this project using Unity Hub > Universal 3D (URP).");
        Material material = new Material(shader);
        material.name = name;
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
