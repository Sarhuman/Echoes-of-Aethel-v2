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

        private CharacterController controller;
        private AethelHealth health;
        private AethelHealth playerHealth;
        private float nextAttackAt;
        private float windupEndsAt;
        private bool windingUp;
        private Vector3 lastFacing = Vector3.forward;

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
                    if (distance <= attackRange + .35f) playerHealth.TakeDamage(attackDamage);
                    nextAttackAt = Time.time + recoverySeconds;
                }
                return;
            }
            if (distance > awarenessRadius) return;
            if (distance > attackRange && distance > .01f)
            {
                lastFacing = delta / distance;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(lastFacing), Mathf.Clamp01(12f * Time.deltaTime));
                controller.Move((lastFacing * movementSpeed + Vector3.down * 2f) * Time.deltaTime);
            }
            else if (Time.time >= nextAttackAt)
            {
                windingUp = true;
                windupEndsAt = Time.time + windupSeconds;
            }
        }
    }
}
