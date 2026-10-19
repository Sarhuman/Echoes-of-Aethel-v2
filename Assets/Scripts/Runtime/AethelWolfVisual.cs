using UnityEngine;

namespace EchoesOfAethel.V2
{
    // Presentation only. The existing AI owns targeting, movement, attack timing and damage.
    [DisallowMultipleComponent]
    public sealed class AethelWolfVisual : MonoBehaviour
    {
        [SerializeField] private AethelWolfAI ai;
        [SerializeField] private AethelHealth health;
        [SerializeField] private CharacterController body;
        [SerializeField] private Animator animator;
        [SerializeField] private LineRenderer warningRing;
        [SerializeField] private LineRenderer progressArc;
        [SerializeField, Min(.01f)] private float biteClipDuration = .2f;
        private readonly Vector3[] ringPoints = new Vector3[49];
        private readonly Vector3[] arcPoints = new Vector3[49];
        private bool wasWinding;
        private float biteStartedAt = float.NegativeInfinity;
        private string state;

        public string CurrentState => state;
        public bool TelegraphVisible => warningRing != null && warningRing.enabled;
        public float DisplayedWindupProgress { get; private set; }

        public void Configure(AethelWolfAI source, AethelHealth life, CharacterController collider,
            Animator rig, LineRenderer outline, LineRenderer progress, float biteDuration)
        {
            ai = source; health = life; body = collider; animator = rig;
            warningRing = outline; progressArc = progress; biteClipDuration = biteDuration;
            HideTelegraph();
        }

        private void OnDisable()
        {
            HideTelegraph(); wasWinding = false; biteStartedAt = float.NegativeInfinity;
        }

        private void LateUpdate()
        {
            if (ai == null || health == null || body == null || animator == null) { HideTelegraph(); return; }
            if (health.IsDead)
            {
                HideTelegraph(); wasWinding = false; Play("Death", 1f); return;
            }
            if (!ai.isActiveAndEnabled)
            {
                HideTelegraph(); wasWinding = false; biteStartedAt = float.NegativeInfinity;
                Play("Idle", 1f); return;
            }
            if (ai.IsWindingUp)
            {
                if (!ai.HasLivingTarget)
                {
                    HideTelegraph(); wasWinding = false; Play("Idle", 1f); return;
                }
                biteStartedAt = float.NegativeInfinity;
                wasWinding = true;
                Sample("Windup", ai.WindupProgress);
                DrawTelegraph(ai.WindupProgress); return;
            }

            HideTelegraph();
            if (wasWinding && ai.RecoveryRemaining > 0f)
            {
                wasWinding = false; biteStartedAt = Time.time;
                Play("Bite", 1f, true);
            }
            if (Time.time - biteStartedAt < biteClipDuration) return;
            wasWinding = false;
            if (!ai.HasLivingTarget) { Play("Idle", 1f); return; }
            Vector3 velocity = body.velocity; velocity.y = 0f;
            if (velocity.sqrMagnitude > .04f)
                Play("Run", Mathf.Clamp(velocity.magnitude / Mathf.Max(.1f, ai.MovementSpeed), .5f, 1.8f));
            else if (ai.RecoveryRemaining > 0f) Sample("Recovery", ai.RecoveryProgress);
            else Play("Idle", 1f);
        }

        private void Play(string next, float speed, bool restart = false)
        {
            animator.speed = speed;
            if (state != next || restart)
            {
                // Contact begins on the AI completion frame; no transition delay or animation event.
                if (next == "Bite") { animator.Play(next, 0, 0f); animator.Update(0f); }
                else animator.CrossFadeInFixedTime(next, .06f, 0, 0f);
                state = next;
            }
        }

        private void Sample(string next, float normalizedTime)
        {
            animator.speed = 0f;
            animator.Play(next, 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0f); state = next;
        }

        private void HideTelegraph()
        {
            if (warningRing != null) warningRing.enabled = false;
            if (progressArc != null) progressArc.enabled = false;
            DisplayedWindupProgress = 0f;
        }

        private void DrawTelegraph(float progress)
        {
            if (warningRing == null || progressArc == null || ai.BiteOrigin == null) { HideTelegraph(); return; }
            // Ground projection of BiteOrigin contact radius; a cue, never a damage collider.
            Vector3 center = ai.BiteOrigin.position;
            center.y = body.transform.position.y - body.skinWidth + .025f;
            for (int i = 0; i < ringPoints.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / (ringPoints.Length - 1);
                ringPoints[i] = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * ai.BiteRadius;
                float fillAngle = angle * progress;
                arcPoints[i] = center + new Vector3(Mathf.Sin(fillAngle), 0f, Mathf.Cos(fillAngle)) * ai.BiteRadius + Vector3.up * .004f;
            }
            warningRing.positionCount = ringPoints.Length; warningRing.SetPositions(ringPoints);
            progressArc.positionCount = arcPoints.Length; progressArc.SetPositions(arcPoints);
            warningRing.startColor = warningRing.endColor = new Color(1f, .16f, .04f, .3f);
            Color warning = Color.Lerp(new Color(1f, .8f, .16f), new Color(1f, .96f, .7f), progress);
            progressArc.startColor = progressArc.endColor = warning;
            progressArc.widthMultiplier = Mathf.Lerp(.04f, .075f, progress);
            warningRing.enabled = progressArc.enabled = true;
            DisplayedWindupProgress = progress;
        }
    }
}
