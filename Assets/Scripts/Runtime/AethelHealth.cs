using System;
using UnityEngine;

namespace EchoesOfAethel.V2
{
    public sealed class AethelHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        private float currentHealth;
        private float invulnerableUntil;

        public float Current => currentHealth;
        public float Maximum => maximumHealth;
        public bool IsDead => currentHealth <= 0f;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public event Action<float, float> Changed;
        public event Action Died;

        private void Awake() { currentHealth = maximumHealth; }

        // An independent source can only extend the existing protection window.
        public void GrantInvulnerability(float durationSeconds)
        {
            if (durationSeconds > 0f)
                invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + durationSeconds);
        }

        public bool TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead || IsInvulnerable) return false;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Changed?.Invoke(currentHealth, maximumHealth);
            if (IsDead) Died?.Invoke();
            return true;
        }

        public void Restore()
        {
            invulnerableUntil = 0f;
            currentHealth = maximumHealth;
            Changed?.Invoke(currentHealth, maximumHealth);
        }

        private void OnValidate() { maximumHealth = Mathf.Max(1f, maximumHealth); }
    }
}
