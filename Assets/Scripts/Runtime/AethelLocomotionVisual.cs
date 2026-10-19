using UnityEngine;

namespace EchoesOfAethel.V2
{
    // Presentation only: reads the existing motor/health and never moves or damages objects.
    public sealed class AethelLocomotionVisual : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController body;
        [SerializeField] private AethelPlayerController motor;
        [SerializeField] private AethelHealth health;
        [SerializeField, Min(.1f)] private float referenceWalkSpeed = 3.5f;
        private bool walking;
        private bool initialized;
        private static readonly int Walking = Animator.StringToHash("Walking");

        public bool IsLocomotionPlaying => walking;

        public void Configure(Animator rig, CharacterController collider, AethelPlayerController player, AethelHealth life)
        {
            animator = rig; body = collider; motor = player; health = life;
        }

        private void LateUpdate()
        {
            if (animator == null || body == null || motor == null || health == null) return;
            Vector3 velocity = body.velocity; velocity.y = 0f;
            bool moving = velocity.sqrMagnitude > .01f && !motor.IsDashing && !health.IsDead;
            if (!moving && (!initialized || walking))
                animator.Play(Walking, 0, 0f); // Hold a supplied walk frame; this is not an authored Idle clip.
            animator.speed = moving ? Mathf.Clamp(velocity.magnitude / referenceWalkSpeed, .5f, 1.6f) : 0f;
            walking = moving;
            initialized = true;
        }
    }
}
