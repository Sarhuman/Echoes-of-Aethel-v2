using System.Collections;
using System.Reflection;
using EchoesOfAethel.V2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed class FoundationTests
{
    private GameObject floor, player, wolf, camera;
    private Keyboard keyboard;
    private AethelPlayerController motor;
    private InputSettings originalSettings;
    private InputSettings testSettings;
    private bool originalBackground;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        originalSettings = InputSystem.settings;
        testSettings = ScriptableObject.CreateInstance<InputSettings>();
        testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings = testSettings;
        originalBackground = Application.runInBackground;
        Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>();
        floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0, -.25f, 0);
        floor.transform.localScale = new Vector3(40, .5f, 40);
        player = new GameObject("TestPlayer");
        motor = player.AddComponent<AethelPlayerController>();
        player.GetComponent<CharacterController>().center = Vector3.up;
        camera = new GameObject("TestCamera", typeof(Camera), typeof(AethelIsometricCamera));
        camera.tag = "MainCamera";
        camera.GetComponent<AethelIsometricCamera>().Configure(player.transform);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings = originalSettings != null ? originalSettings : ScriptableObject.CreateInstance<InputSettings>();
        Object.Destroy(testSettings);
        Application.runInBackground = originalBackground;
        Object.Destroy(player); Object.Destroy(wolf); Object.Destroy(camera); Object.Destroy(floor);
        yield return null;
    }

    private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);

    [UnityTest]
    public IEnumerator MovementRotationAndFixedCamera()
    {
        Vector3 start = player.transform.position;
        Quaternion view = camera.transform.rotation;
        Keys(Key.W);
        yield return new WaitForSeconds(.35f);
        Keys();
        Vector3 travel = player.transform.position - start; travel.y = 0;
        Vector3 forward = camera.transform.forward; forward.y = 0;
        Assert.Greater(travel.magnitude, .5f);
        Assert.Greater(Vector3.Dot(travel.normalized, forward.normalized), .98f);
        Assert.Less(Quaternion.Angle(view, camera.transform.rotation), .01f);
        Assert.Greater(Vector3.Dot(player.transform.forward, forward.normalized), .9f);
        var directions = new[] { Key.S, Key.A, Key.D };
        Vector3 right = camera.transform.right; right.y = 0;
        var expected = new[] { -forward.normalized, -right.normalized, right.normalized };
        for (int i = 0; i < directions.Length; i++)
        {
            Keys(); yield return new WaitForSeconds(.2f);
            start = player.transform.position; Keys(directions[i]);
            yield return new WaitForSeconds(.25f); Keys();
            travel = player.transform.position - start; travel.y = 0;
            Assert.Greater(Vector3.Dot(travel.normalized, expected[i]), .98f);
        }
        yield return new WaitForSeconds(.5f);
        Assert.Less(Vector3.Distance(camera.transform.position, player.transform.position + new Vector3(8.5f,12,-9)), .1f);
    }

    [UnityTest]
    public IEnumerator DashCostCooldownInvulnerabilityAndInsufficientStamina()
    {
        Set(motor, "staminaRegenPerSecond", 0f);
        var health = player.GetComponent<AethelHealth>();
        Keys(Key.Space); yield return null; yield return null;
        Assert.IsTrue(motor.IsDashing); Assert.AreEqual(75, motor.Stamina, .01);
        Assert.IsFalse(health.TakeDamage(15));
        Keys(); yield return new WaitForSeconds(.15f);
        Assert.IsTrue(health.TakeDamage(15));
        yield return new WaitForSeconds(.1f);
        Assert.IsFalse(motor.IsDashing);
        Keys(Key.Space); yield return null; yield return null;
        Assert.AreEqual(75, motor.Stamina, .01);
        Keys(); yield return new WaitForSeconds(1.25f);
        Set(motor, "stamina", 24f);
        Keys(Key.Space); yield return null; yield return null;
        Assert.IsFalse(motor.IsDashing); Assert.AreEqual(24, motor.Stamina, .01);
        Keys(); Set(motor, "staminaRegenPerSecond", 16f);
        yield return new WaitForSeconds(.2f); Assert.Greater(motor.Stamina, 25f);
        Keys(Key.Space); yield return null; yield return null; Assert.IsTrue(motor.IsDashing);
    }

    [UnityTest]
    public IEnumerator BasicAttackDeduplicatesMultipleCollidersAndRespectsCooldown()
    {
        wolf = new GameObject("DamageTarget"); wolf.transform.position = new Vector3(0, 0, 1.5f);
        var hp = wolf.AddComponent<AethelHealth>();
        var body = wolf.AddComponent<BoxCollider>(); body.center = Vector3.up;
        var child = new GameObject("SecondCollider"); child.transform.SetParent(wolf.transform, false);
        child.AddComponent<SphereCollider>().center = Vector3.up;
        yield return null;
        Keys(Key.J); yield return null; yield return null;
        Assert.AreEqual(75, hp.Current);
        Keys(); yield return null; Keys(Key.J); yield return null; yield return null;
        Assert.AreEqual(75, hp.Current);
        Keys(); yield return new WaitForSeconds(.5f);
        Keys(Key.J); yield return null; yield return null;
        Assert.AreEqual(50, hp.Current);
        Keys(); Set(motor, "attackLayers", (LayerMask)0);
        yield return new WaitForSeconds(.5f); Keys(Key.J); yield return null; yield return null;
        Assert.AreEqual(50, hp.Current, "Excluded attack layers must not receive damage.");
    }

    [UnityTest]
    public IEnumerator WolfChasesSettlesTelegraphsAndDamagesOnce()
    {
        wolf = new GameObject("TestWolf"); wolf.transform.position = new Vector3(0, 1, 4);
        var ai = wolf.AddComponent<AethelWolfAI>();
        wolf.GetComponent<CharacterController>().center = Vector3.up;
        ai.Configure(player.transform);
        var bite = new GameObject("BiteOrigin").transform; bite.SetParent(wolf.transform, false);
        bite.localPosition = new Vector3(0, 1, 1); ai.ConfigureVisual(wolf.transform, bite);
        yield return new WaitForSeconds(.3f);
        Assert.Less(wolf.transform.position.z, 3.5f);
        float deadline = Time.time + 2;
        while (!ai.IsWindingUp && Time.time < deadline) yield return null;
        Assert.IsTrue(ai.IsWindingUp);
        Assert.Less(Mathf.Abs(wolf.transform.position.y), .15f);
        var hp = player.GetComponent<AethelHealth>(); Assert.AreEqual(100, hp.Current);
        yield return new WaitForSeconds(.5f); Assert.AreEqual(85, hp.Current);
        yield return new WaitForSeconds(.3f); Assert.AreEqual(85, hp.Current);
    }

    [UnityTest]
    public IEnumerator DashCannotCrossWall()
    {
        wolf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wolf.transform.position = new Vector3(0, 1, 1.3f); wolf.transform.localScale = new Vector3(10, 2, .5f);
        Keys(Key.Space); yield return new WaitForSeconds(.3f);
        Assert.Less(player.transform.position.z, 1f);
    }

    [UnityTest]
    public IEnumerator InvulnerabilityOnlyExtendsAndDeathOccursOnce()
    {
        var hp = player.GetComponent<AethelHealth>(); int deaths = 0; hp.Died += () => deaths++;
        hp.GrantInvulnerability(.2f); hp.GrantInvulnerability(.01f);
        yield return new WaitForSeconds(.05f); Assert.IsFalse(hp.TakeDamage(100));
        yield return new WaitForSeconds(.2f); Assert.IsTrue(hp.TakeDamage(100));
        Assert.IsFalse(hp.TakeDamage(100)); Assert.AreEqual(1, deaths);
    }
}
