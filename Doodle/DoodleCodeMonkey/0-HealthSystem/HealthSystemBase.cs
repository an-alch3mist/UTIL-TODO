using System;
using UnityEngine;

namespace SPACE_DOODLE_CODEMONKEY
{
    public abstract class HealthSystemBase : MonoBehaviour
    {
        public abstract bool getIsAlive { get; }
		public virtual float getHealthPercent { get; }
		public virtual float getArmourPercent { get; }

        public abstract void TakeDamage(float amount);
        public abstract void Heal(float amount);
        public abstract void RepairArmour(float amount);
        public abstract void Kill();
        public abstract void Revive(float healthAmount);

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnArmourChanged;
        public event Action               OnDeath;

        protected void NotifyHealthChanged(float c, float m) => OnHealthChanged?.Invoke(c, m);
        protected void NotifyArmourChanged(float c, float m) => OnArmourChanged?.Invoke(c, m);
        protected void NotifyDeath()                         => OnDeath?.Invoke();
    }
}
