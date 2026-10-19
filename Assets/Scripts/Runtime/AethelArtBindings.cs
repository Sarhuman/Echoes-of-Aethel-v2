using UnityEngine;

namespace EchoesOfAethel.V2
{
    // Integration references only. Combat/input/health remain in the existing components.
    public sealed class AethelArtBindings : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private Transform shieldSocket;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Transform damageOrigin;
        [SerializeField] private CharacterController body;

        public void Configure(Animator rig, Transform visual, Transform ground, Transform origin,
            CharacterController collider, Transform weapon = null, Transform shield = null)
        {
            animator = rig; visualRoot = visual; groundCheck = ground; damageOrigin = origin;
            body = collider; weaponSocket = weapon; shieldSocket = shield;
        }

        public string ValidateBindings()
        {
            if (animator == null || visualRoot == null || groundCheck == null || damageOrigin == null || body == null)
                return "Assign Animator, VisualRoot, GroundCheck, damage origin and CharacterController.";
            if (GetComponent<AethelPlayerController>() != null && (weaponSocket == null || shieldSocket == null))
                return "Bastiao requires WeaponSocket and ShieldSocket.";
            if (animator.applyRootMotion) return "Disable root motion: CharacterController owns movement.";
            if (visualRoot.GetComponentsInChildren<Collider>(true).Length != 0)
                return "Visual children must not introduce colliders. Keep physics on the gameplay root.";
            if (animator.runtimeAnimatorController == null) return "Technical placeholder: Animator controller/rig still pending.";
            return "References assigned. Playtest animation, sockets, collisions and damage before accepting art.";
        }
    }
}
