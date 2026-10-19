using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoesOfAethel.V2
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AethelHealth))]
    public sealed class AethelPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5.5f;
        [SerializeField, Min(0f)] private float acceleration = 38f;
        [SerializeField, Min(0f)] private float rotationSpeed = 16f;
        [SerializeField] private float gravity = -25f;
        [Header("Dash")]
        [SerializeField, Min(.01f)] private float dashDuration = .20f;
        [SerializeField, Min(0f)] private float dashSpeed = 17f;
        [SerializeField, Range(0f, .2f)] private float dashInvulnerability = .12f;
        [SerializeField, Min(0f)] private float dashCooldown = 1.2f;
        [SerializeField, Min(0f)] private float dashStaminaCost = 25f;
        [SerializeField, Min(1f)] private float maximumStamina = 100f;
        [SerializeField, Min(0f)] private float staminaRegenPerSecond = 16f;
        [Header("Attack")]
        [SerializeField, Min(.1f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackDamage = 25f;
        [SerializeField, Min(.05f)] private float attackCooldown = .45f;
        [SerializeField] private LayerMask attackLayers = ~0;
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Tooltip("Damage query center; gameplay owns damage, never animation events.")] private Transform hitOrigin;

        public Transform HitOrigin => hitOrigin;
        public LayerMask AttackLayers => attackLayers;
        public Transform CameraTransform => cameraTransform;

        private CharacterController controller;
        private AethelHealth health;
        private Vector3 horizontalVelocity;
        private Vector3 facing = Vector3.forward;
        private float verticalVelocity;
        private float dashEndsAt;
        private float dashReadyAt;
        private float attackReadyAt;
        private float stamina;
        private readonly Collider[] overlaps = new Collider[24];
        private readonly HashSet<AethelHealth> attackedInSwing = new HashSet<AethelHealth>();
        private InputAction move;
        private InputAction dash;
        private InputAction attack;

        public float Stamina => stamina;
        public bool IsDashing => Time.time < dashEndsAt;

        public void ConfigureVisual(Transform model) { visualRoot = model; }
        public void ConfigureHitOrigin(Transform origin) { hitOrigin = origin; }
        public void ConfigureCamera(Transform view) { cameraTransform = view; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<AethelHealth>();
            stamina = maximumStamina;
            if (visualRoot == null) visualRoot = transform;
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            dash = new InputAction("Dash", InputActionType.Button, "<Keyboard>/space");
            dash.AddBinding("<Keyboard>/leftShift");
            dash.AddBinding("<Gamepad>/buttonSouth");
            attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            attack.AddBinding("<Keyboard>/j");
            attack.AddBinding("<Gamepad>/buttonWest");
        }

        private void OnEnable() { move?.Enable(); dash?.Enable(); attack?.Enable(); }
        private void OnDisable() { move?.Disable(); dash?.Disable(); attack?.Disable(); }
        private void OnDestroy() { move?.Dispose(); dash?.Dispose(); attack?.Dispose(); }

        private void Update()
        {
            if (health.IsDead) return;
            float dt = Time.deltaTime;
            stamina = Mathf.Min(maximumStamina, stamina + staminaRegenPerSecond * dt);
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            Vector2 axes = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            Vector3 input = CameraRelative(axes);
            if (dash.WasPressedThisFrame() && !IsDashing &&
                Time.time >= dashReadyAt && stamina >= dashStaminaCost)
            {
                if (input.sqrMagnitude > .01f) facing = input.normalized;
                dashEndsAt = Time.time + dashDuration;
                dashReadyAt = Time.time + dashCooldown;
                stamina -= dashStaminaCost;
                health.GrantInvulnerability(Mathf.Min(dashDuration, dashInvulnerability));
            }

            if (IsDashing)
            {
                horizontalVelocity = facing * dashSpeed;
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity, input * moveSpeed, acceleration * dt);
                if (input.sqrMagnitude > .01f) facing = input.normalized;
            }

            if (visualRoot != null && facing.sqrMagnitude > .01f)
            {
                Quaternion desired = Quaternion.LookRotation(facing);
                visualRoot.rotation = Quaternion.Slerp(
                    visualRoot.rotation, desired, Mathf.Clamp01(rotationSpeed * dt));
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * dt;
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            if (attack.WasPressedThisFrame() && Time.time >= attackReadyAt && !IsDashing)
            {
                attackReadyAt = Time.time + attackCooldown;
                Strike();
            }
        }

        private Vector3 CameraRelative(Vector2 axes)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            Vector3 vector = forward * axes.y + right * axes.x;
            return Vector3.ClampMagnitude(vector, 1f);
        }

        private void Strike()
        {
            Vector3 center = hitOrigin != null ? hitOrigin.position : controller.bounds.center + facing * (attackRange * .55f);
            int count = Physics.OverlapSphereNonAlloc(center, attackRange * .65f,
                overlaps, attackLayers, QueryTriggerInteraction.Ignore);
            attackedInSwing.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider target = overlaps[i];
                if (target == null || target.transform.root == transform.root) continue;
                AethelHealth other = target.GetComponentInParent<AethelHealth>();
                if (other != null && other != health && attackedInSwing.Add(other))
                    other.TakeDamage(attackDamage);
            }
        }

        private void OnValidate()
        {
            dashDuration = Mathf.Max(.01f, dashDuration);
            dashInvulnerability = Mathf.Clamp(dashInvulnerability, 0f, dashDuration);
            maximumStamina = Mathf.Max(1f, maximumStamina);
        }
    }
}
