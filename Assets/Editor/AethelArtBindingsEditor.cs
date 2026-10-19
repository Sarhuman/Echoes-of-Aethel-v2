using EchoesOfAethel.V2;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AethelArtBindings))]
public sealed class AethelArtBindingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var bindings = (AethelArtBindings)target;
        EditorGUILayout.HelpBox(bindings.ValidateBindings(), MessageType.Info);
        EditorGUILayout.HelpBox("Damage authority: AethelPlayerController / AethelWolfAI → AethelHealth.TakeDamage. Do not add damage Animation Events. Inspect attack layers and camera on the existing controllers.", MessageType.Info);
        if (Application.isPlaying)
        {
            var health = bindings.GetComponent<AethelHealth>();
            if (health != null) EditorGUILayout.LabelField("Health / invulnerable", $"{health.Current:0.0}/{health.Maximum:0.0} / {health.IsInvulnerable}");
            var player = bindings.GetComponent<AethelPlayerController>();
            if (player != null) EditorGUILayout.LabelField("Stamina / dashing", $"{player.Stamina:0.0} / {player.IsDashing}");
            var wolf = bindings.GetComponent<AethelWolfAI>();
            if (wolf != null) EditorGUILayout.LabelField("Wolf windup", wolf.IsWindingUp.ToString());
            var wolfVisual = bindings.GetComponentInChildren<AethelWolfVisual>();
            if (wolfVisual != null)
            {
                EditorGUILayout.LabelField("Wolf animation", wolfVisual.CurrentState);
                EditorGUILayout.LabelField("Telegraph / progress", $"{wolfVisual.TelegraphVisible} / {wolfVisual.DisplayedWindupProgress:P0}");
            }
            Repaint();
        }
    }
}
