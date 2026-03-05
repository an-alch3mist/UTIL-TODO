using System;
using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	// ─────────────────────────────────────────────────────────────────────────
	//  Behaviour selector — choose in the Inspector dropdown
	// ─────────────────────────────────────────────────────────────────────────
	public enum HealthSystemType
	{
		Entity,     // Standard HP + permanent armour
		NoArmour,   // HP only — no armour slot
		Shield,     // Regenerating shield (replaces armour)
		Boss,       // Phase-based damage resistance + OnPhaseChanged
		Zombie,     // Auto-revive after delay + passive HP regen
		Barrel,     // Destructible prop — no heal / revive
		GodMode,    // Invincible — all damage is a no-op
		Network,    // Server-authoritative — local calls are no-ops
	}

	// ─────────────────────────────────────────────────────────────────────────
	//  Per-type config structs  (only the relevant block shows in Inspector)
	//  Tip: use a custom Editor or Odin to hide irrelevant blocks at runtime.
	// ─────────────────────────────────────────────────────────────────────────
	[Serializable]
	public struct EntityConfig
	{
		public float maxHealth;     // default 100
		public float maxArmour;     // default  50
	}
	[Serializable]
	public struct ShieldConfig
	{
		public float maxHealth;
		public float maxShield;
		public float rechargeDelay; // seconds after last hit
		public float rechargeRate;  // shield per second
	}
	[Serializable]
	public struct BossConfig
	{
		public float maxHealth;
		public float phaseThreshold;   // 0–1 fraction (e.g. 0.5 = 50 %)
		public float phase1Resistance; // 0–1 damage reduction in phase 1
	}
	[Serializable]
	public struct ZombieConfig
	{
		public float maxHealth;
		public float regenRate;   // HP per second while alive
		public float reviveDelay; // seconds before auto-revive
	}
	[Serializable]
	public struct BarrelConfig
	{
		public float maxHealth;   // default 30
	}

	// ─────────────────────────────────────────────────────────────────────────
	//  Unified component
	// ─────────────────────────────────────────────────────────────────────────
	public class UnifiedHealthSystem : MonoBehaviour
	{
		// ── Selector ──────────────────────────────────────────────────────
		[Header("Behaviour")]
		[SerializeField] private HealthSystemType systemType = HealthSystemType.Entity;

		// ── Config blocks (only relevant ones are read at runtime) ─────────
		[Header("Entity / NoArmour Config")]
		[SerializeField] private EntityConfig entity = new EntityConfig { maxHealth = 100f, maxArmour = 50f };

		[Header("Shield Config")]
		[SerializeField]
		private ShieldConfig shield = new ShieldConfig
		{ maxHealth = 100f, maxShield = 60f, rechargeDelay = 3f, rechargeRate = 15f };

		[Header("Boss Config")]
		[SerializeField]
		private BossConfig boss = new BossConfig
		{ maxHealth = 500f, phaseThreshold = 0.5f, phase1Resistance = 0.5f };

		[Header("Zombie Config")]
		[SerializeField]
		private ZombieConfig zombie = new ZombieConfig
		{ maxHealth = 100f, regenRate = 5f, reviveDelay = 4f };

		[Header("Barrel Config")]
		[SerializeField] private BarrelConfig barrel = new BarrelConfig { maxHealth = 30f };

		// ── Runtime state ──────────────────────────────────────────────────
		private float currentHealth;
		private float currentArmour;
		private float maxHealth;
		private float maxArmour;

		// type-specific runtime vars
		private float rechargeTimer;  // Shield
		private float deadTimer;      // Zombie
		private int bossPhase = 1;  // Boss

		// ── Events ─────────────────────────────────────────────────────────
		public event Action OnHealthChanged;
		public event Action OnArmourChanged;
		public event Action<float, float> OnCertainEventOccurRequired2FloatParam;
		public event Action OnDeath;
		public event Action OnPhaseChanged; // Boss only

		// ── Queries ────────────────────────────────────────────────────────
		public float getHealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
		public float getArmourPercent => maxArmour > 0f ? currentArmour / maxArmour : 0f;
		public bool IsAlive => systemType == HealthSystemType.GodMode || currentHealth > 0f;

		// ─────────────────────────────────────────────────────────────────
		//  Init
		// ─────────────────────────────────────────────────────────────────
		private void Awake()
		{
			Debug.Log(C.method(this));
			InitFromType();
		}

		private void InitFromType()
		{
			bossPhase = 1;
			switch (systemType)
			{
				case HealthSystemType.Entity:
					maxHealth = entity.maxHealth;
					maxArmour = entity.maxArmour;
					break;

				case HealthSystemType.NoArmour:
					maxHealth = entity.maxHealth; // reuses Entity health field
					maxArmour = 0f;
					break;

				case HealthSystemType.Shield:
					maxHealth = shield.maxHealth;
					maxArmour = shield.maxShield;
					break;

				case HealthSystemType.Boss:
					maxHealth = boss.maxHealth;
					maxArmour = 0f;
					break;

				case HealthSystemType.Zombie:
					maxHealth = zombie.maxHealth;
					maxArmour = 0f;
					break;

				case HealthSystemType.Barrel:
					maxHealth = barrel.maxHealth;
					maxArmour = 0f;
					break;

				case HealthSystemType.GodMode:
					maxHealth = 1f;
					maxArmour = 0f;
					break;

				case HealthSystemType.Network:
					maxHealth = 100f; // overwritten on first server sync
					maxArmour = 50f;
					break;
			}

			currentHealth = maxHealth;
			currentArmour = maxArmour;
		}

		// ─────────────────────────────────────────────────────────────────
		//  Update — ticks for Shield / Zombie
		// ─────────────────────────────────────────────────────────────────
		private void Update()
		{
			switch (systemType)
			{
				case HealthSystemType.Shield: TickShield(); break;
				case HealthSystemType.Zombie: TickZombie(); break;
			}
		}

		private void TickShield()
		{
			if (!IsAlive || currentArmour >= maxArmour) return;
			if (rechargeTimer > 0f) { rechargeTimer -= Time.deltaTime; return; }
			currentArmour = Mathf.Min(currentArmour + shield.rechargeRate * Time.deltaTime, maxArmour);
			OnArmourChanged?.Invoke();
		}

		private void TickZombie()
		{
			if (!IsAlive)
			{
				deadTimer -= Time.deltaTime;
				if (deadTimer <= 0f) Revive(50f);
				return;
			}
			// passive regen while alive
			if (currentHealth >= maxHealth) return;
			currentHealth = Mathf.Min(currentHealth + zombie.regenRate * Time.deltaTime, maxHealth);
			OnHealthChanged?.Invoke();
		}

		// ─────────────────────────────────────────────────────────────────
		//  Commands
		// ─────────────────────────────────────────────────────────────────
		public void TakeDamage(float amount)
		{
			// ── type-level early-outs ──────────────────────────────────
			if (systemType == HealthSystemType.GodMode) return;
			if (systemType == HealthSystemType.Network) return; // server is authority
			if (!IsAlive || amount <= 0f) return;

			// ── Boss phase 1 resistance ────────────────────────────────
			if (systemType == HealthSystemType.Boss && bossPhase == 1)
				amount *= (1f - boss.phase1Resistance);

			// ── Armour-first absorption ────────────────────────────────
			if (currentArmour > 0f)
			{
				float absorbed = Mathf.Min(currentArmour, amount);
				currentArmour -= absorbed;
				amount -= absorbed;
				OnArmourChanged?.Invoke();
			}

			// ── Apply remaining to health ──────────────────────────────
			if (amount > 0f)
			{
				currentHealth = Mathf.Max(currentHealth - amount, 0f);
				OnHealthChanged?.Invoke();

				if (currentHealth <= 0f)
				{
					OnDeath?.Invoke();
					if (systemType == HealthSystemType.Zombie)
						deadTimer = zombie.reviveDelay;
				}
			}

			// ── Shield hit resets recharge window ─────────────────────
			if (systemType == HealthSystemType.Shield)
				rechargeTimer = shield.rechargeDelay;

			// ── Boss phase transition ──────────────────────────────────
			if (systemType == HealthSystemType.Boss && bossPhase == 1 && getHealthPercent < boss.phaseThreshold)
			{
				bossPhase = 2;
				OnPhaseChanged?.Invoke();
			}
		}

		public void Heal(float amount)
		{
			if (systemType == HealthSystemType.GodMode) return;
			if (systemType == HealthSystemType.Network) return;
			if (systemType == HealthSystemType.Barrel) return; // props don't heal
			if (!IsAlive || amount <= 0f) return;

			currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
			OnHealthChanged?.Invoke();
		}

		public void RepairArmour(float amount)
		{
			// Shield self-repairs only; no-armour types have nothing to repair
			if (systemType == HealthSystemType.Shield) return;
			if (systemType == HealthSystemType.NoArmour) return;
			if (systemType == HealthSystemType.Boss) return;
			if (systemType == HealthSystemType.Zombie) return;
			if (systemType == HealthSystemType.Barrel) return;
			if (systemType == HealthSystemType.GodMode) return;
			if (systemType == HealthSystemType.Network) return;
			if (!IsAlive || amount <= 0f) return;

			currentArmour = Mathf.Min(currentArmour + amount, maxArmour);
			OnArmourChanged?.Invoke();
		}

		public void Revive(float healthAmount)
		{
			if (systemType == HealthSystemType.GodMode) return;
			if (systemType == HealthSystemType.Network) return;
			if (systemType == HealthSystemType.Barrel) return;
			if (IsAlive) return;

			currentHealth = Mathf.Clamp(healthAmount, 1f, maxHealth);
			OnHealthChanged?.Invoke();
			OnArmourChanged?.Invoke();
		}

		// ─────────────────────────────────────────────────────────────────
		//  Network-only API — called by your Netcode / Mirror / Photon layer
		// ─────────────────────────────────────────────────────────────────
		/// <summary>
		/// Server sends authoritative state → apply directly, skip all local logic.
		/// </summary>
		public void ApplyServerState(float health, float armour, float hMax, float aMax)
		{
			if (systemType != HealthSystemType.Network)
			{
				Debug.LogWarning("ApplyServerState called on non-Network system — ignored.");
				return;
			}
			maxHealth = hMax;
			maxArmour = aMax;
			currentHealth = health;
			currentArmour = armour;
			OnHealthChanged?.Invoke();
			OnArmourChanged?.Invoke();
			if (currentHealth <= 0f) OnDeath?.Invoke();
		}

		// ─────────────────────────────────────────────────────────────────
		//  Hot-swap at runtime (e.g. pick up a GodMode power-up)
		// ─────────────────────────────────────────────────────────────────
		/// <summary>
		/// Switches behaviour type at runtime and re-initialises state.
		/// Subscribe to events again after calling this if needed.
		/// </summary>
		public void SwitchType(HealthSystemType newType)
		{
			systemType = newType;
			InitFromType();
			OnHealthChanged?.Invoke();
			OnArmourChanged?.Invoke();
		}
	}
}