using UnityEngine;

namespace EchoesOfAethel.V2
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AethelHealth))]
    public sealed class AethelWolfAI : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float awarenessRadius = 12f;
        [SerializeField, Min(0f)] private float movementSpeed = 3f;
        [SerializeField, Min(.1f)] private float attackRange = 1.65f;
        [SerializeField, Min(0f)] private float attackDamage = 15f;
        [SerializeField, Min(.05f)] private float windupSeconds = .45f;
        [SerializeField, Min(.05f)] private float recoverySeconds = .85f;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform biteOrigin;
        [SerializeField, Min(.1f), Tooltip("Contact radius around BiteOrigin; one damage application per windup.")] private float biteRadius = 1.1f;
        [SerializeField] private LayerMask attackLayers = ~0;

        public Transform BiteOrigin => biteOrigin;
        public LayerMask AttackLayers => attackLayers;
        public void ConfigureVisual(Transform visual, Transform bite) { visualRoot = visual; biteOrigin = bite; }

        private CharacterController controller;
        private AethelHealth health;
        private AethelHealth playerHealth;
        private float nextAttackAt;
        private float windupEndsAt;
        private bool windingUp;
        private Vector3 lastFacing = Vector3.forward;
        private float verticalVelocity;

        public bool IsWindingUp => windingUp;
        // Read-only presentation data. Animation/telegraph never drive these timers or damage.
        public float WindupProgress => windingUp ? Mathf.Clamp01(1f - (windupEndsAt - Time.time) / windupSeconds) : 0f;
        public float RecoveryRemaining => Mathf.Max(0f, nextAttackAt - Time.time);
        public float RecoveryProgress => Mathf.Clamp01(1f - RecoveryRemaining / recoverySeconds);
        public float BiteRadius => biteRadius;
        public float MovementSpeed => movementSpeed;
        public bool HasLivingTarget => target != null && playerHealth != null && !playerHealth.IsDead;

        public void Configure(Transform player)
        {
            target = player;
            playerHealth = target != null ? target.GetComponent<AethelHealth>() : null;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<AethelHealth>();
        }

        private void Update()
        {
            // Gravity is independent of chase/attack state, including recovery and death.
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += -25f * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            if (health.IsDead || target == null) return;
            if (playerHealth == null) playerHealth = target.GetComponent<AethelHealth>();
            if (playerHealth == null || playerHealth.IsDead) return;

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (windingUp)
            {
                if (Time.time >= windupEndsAt)
                {
                    windingUp = false;
                    bool allowedLayer = (attackLayers.value & (1 << target.gameObject.layer)) != 0;
                    bool inReach = distance <= attackRange + .35f;
                    if (biteOrigin != null)
                    {
                        var body = target.GetComponent<CharacterController>();
                        Vector3 point = body != null ? body.bounds.ClosestPoint(biteOrigin.position) : target.position;
                        inReach = inReach && Vector3.Distance(point, biteOrigin.position) <= biteRadius;
                    }
                    if (allowedLayer && inReach) playerHealth.TakeDamage(attackDamage);
                    nextAttackAt = Time.time + recoverySeconds;
                }
                return;
            }
            if (distance > awarenessRadius) return;
            if (distance > attackRange && distance > .01f)
            {
                lastFacing = delta / distance;
                Transform turning = visualRoot != null ? visualRoot : transform;
                turning.rotation = Quaternion.Slerp(turning.rotation,
                    Quaternion.LookRotation(lastFacing), Mathf.Clamp01(12f * Time.deltaTime));
                controller.Move(lastFacing * movementSpeed * Time.deltaTime);
            }
            else if (Time.time >= nextAttackAt)
            {
                windingUp = true;
                windupEndsAt = Time.time + windupSeconds;
            }
        }
    }
}
