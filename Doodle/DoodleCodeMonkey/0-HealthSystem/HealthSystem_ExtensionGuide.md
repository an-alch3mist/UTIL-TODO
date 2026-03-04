# Health System — Architecture & Extension Guide

---

## Overview

The system is split into two independent base classes that communicate exclusively through C# events. Neither side holds a concrete reference to the other.

```
HealthSystemBase          fires events          HealthReactionBase
  (state / logic)   ──────────────────────►   (reactions / feedback)
                       OnHealthChanged
                       OnArmourChanged
                       OnDeath
```

This means **any** `HealthSystemBase` subclass pairs with **any** `HealthReactionBase` subclass — the demo, AI, and all reaction code never need to change.

```csharp
ui.Bind(system); // that's the entire contract
```

---

## HealthSystemBase — Implementations

### EntityHealthSystem ✅ (implemented)
Standard enemy or player. Armour absorbs damage first, then health takes the remainder. Supports heal, armour repair, kill, and revive.

**Crucial extension notes:**
- Add an `invincibilityFrames` timer here if you need hit-stun (block `TakeDamage` while timer > 0)
- `Revive` intentionally leaves armour at whatever it was — is that right for your design?

---

### BarrelHealthSystem
Destructible prop. No armour, no revive — just a health pool that deletes or disables the object on death.

```csharp
public class BarrelHealthSystem : HealthSystemBase
{
    [SerializeField] private float maxHealth = 30f;
    private float currentHealth;

    private void Awake() => currentHealth = maxHealth;

    public override float getHealthPercent => currentHealth / maxHealth;
    public override float getArmourPercent => 0f;

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f || !getIsAlive) return;
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        NotifyHealthChanged(currentHealth, maxHealth);
        if (currentHealth <= 0f) NotifyDeath();
    }

    public override void Heal(float amount)        { } // props don't heal
    public override void RepairArmour(float amount){ } // no armour
    public override void Kill()
    {
        currentHealth = 0f;
        NotifyHealthChanged(currentHealth, maxHealth);
        NotifyDeath();
    }
    public override void Revive(float healthAmount){ } // props don't revive
}
```

**Why this matters:** Every prop, crate, door, or wall in your game can drop in a `BarrelHealthSystem` and immediately work with explosions, bullets, and any reaction layer — no extra wiring.

---

### BossHealthSystem
Multi-phase boss. Damage resistance and behaviour change at a health threshold.

```csharp
public class BossHealthSystem : HealthSystemBase
{
    [SerializeField] private float maxHealth  = 500f;
    [SerializeField] private float phaseThreshold = 0.5f; // 50 %
    private float currentHealth;
    private int   phase = 1;

    public event Action OnPhaseChanged; // wire to cutscene / music system

    private void Awake() => currentHealth = maxHealth;

    public override float getHealthPercent => currentHealth / maxHealth;
    public override float getArmourPercent => 0f;

    public override void TakeDamage(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;

        if (phase == 1) amount *= 0.5f; // phase 1 — damage resistance

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        NotifyHealthChanged(currentHealth, maxHealth);

        if (phase == 1 && getHealthPercent < phaseThreshold)
        {
            phase = 2;
            OnPhaseChanged?.Invoke(); // trigger rage cutscene
        }

        if (currentHealth <= 0f) NotifyDeath();
    }

    // Heal / Repair / Kill / Revive implemented as needed per boss design
}
```

**Crucial extension note:** `OnPhaseChanged` is a custom event on top of the base contract. Any system (music, AI, VFX, camera shake) can subscribe without touching the health logic.

---

### ShieldHealthSystem
Regenerating shield replaces permanent armour. Shield recharges automatically after a delay when not taking damage.

```csharp
public class ShieldHealthSystem : HealthSystemBase
{
    [SerializeField] private float maxHealth      = 100f;
    [SerializeField] private float maxArmour      = 60f;
    [SerializeField] private float rechargeDelay  = 3f;
    [SerializeField] private float rechargeRate   = 15f; // per second
    private float currentHealth;
    private float currentArmour;
    private float rechargeTimer;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentArmour = maxArmour;
    }

    public override float getHealthPercent => currentHealth / maxHealth;
    public override float getArmourPercent => currentArmour / maxArmour;

    public override void TakeDamage(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;

        rechargeTimer = rechargeDelay; // reset recharge on any hit

        if (currentArmour > 0f)
        {
            float absorbed = Mathf.Min(currentArmour, amount);
            currentArmour -= absorbed;
            amount        -= absorbed;
            NotifyArmourChanged(currentArmour, maxArmour);
        }

        if (amount > 0f)
        {
            currentHealth = Mathf.Max(currentHealth - amount, 0f);
            NotifyHealthChanged(currentHealth, maxHealth);
            if (currentHealth <= 0f) NotifyDeath();
        }
    }

    private void Update()
    {
        if (!getIsAlive) return;

        if (rechargeTimer > 0f) { rechargeTimer -= Time.deltaTime; return; }

        if (currentArmour < maxArmour)
        {
            currentArmour = Mathf.Min(currentArmour + rechargeRate * Time.deltaTime, maxArmour);
            NotifyArmourChanged(currentArmour, maxArmour);
        }
    }

    public override void Heal(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        NotifyHealthChanged(currentHealth, maxHealth);
    }

    public override void RepairArmour(float amount) { } // shield auto-repairs
    public override void Kill()
    {
        currentHealth = 0f; currentArmour = 0f;
        NotifyHealthChanged(currentHealth, maxHealth);
        NotifyArmourChanged(currentArmour, maxArmour);
        NotifyDeath();
    }
    public override void Revive(float healthAmount)
    {
        if (getIsAlive) return;
        currentHealth = Mathf.Clamp(healthAmount, 1f, maxHealth);
        NotifyHealthChanged(currentHealth, maxHealth);
    }
}
```

**Crucial extension note:** `rechargeDelay` and `rechargeRate` are serialized — a faster recharge rate makes for a tankier feel without changing any other system. The reaction layer sees `OnArmourChanged` fire continuously during recharge and can animate a "charging" glow with zero extra code.

---

### VehicleHealthSystem
Separate hull and engine health pools. The vehicle is dead when **either** reaches zero.

```csharp
public class VehicleHealthSystem : HealthSystemBase
{
    [SerializeField] private float maxHullHealth   = 200f;
    [SerializeField] private float maxEngineHealth = 80f;
    private float hullHealth;
    private float engineHealth;

    public event Action<float, float> OnEngineChanged; // current, max

    private void Awake()
    {
        hullHealth   = maxHullHealth;
        engineHealth = maxEngineHealth;
    }

    public override float getHealthPercent => hullHealth / maxHullHealth;
    public override float getArmourPercent => engineHealth / maxEngineHealth; // repurposed slot
    public override bool  getIsAlive       => hullHealth > 0f && engineHealth > 0f;

    public override void TakeDamage(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;
        hullHealth = Mathf.Max(hullHealth - amount, 0f);
        NotifyHealthChanged(hullHealth, maxHullHealth);
        if (!getIsAlive) NotifyDeath();
    }

    public void TakeEngineDamage(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;
        engineHealth = Mathf.Max(engineHealth - amount, 0f);
        OnEngineChanged?.Invoke(engineHealth, maxEngineHealth);
        NotifyArmourChanged(engineHealth, maxEngineHealth); // drives the armour bar as engine bar
        if (!getIsAlive) NotifyDeath();
    }

    public override void Heal(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;
        hullHealth = Mathf.Min(hullHealth + amount, maxHullHealth);
        NotifyHealthChanged(hullHealth, maxHullHealth);
    }

    public override void RepairArmour(float amount) // repurposed as engine repair
    {
        if (!getIsAlive || amount <= 0f) return;
        engineHealth = Mathf.Min(engineHealth + amount, maxEngineHealth);
        NotifyArmourChanged(engineHealth, maxEngineHealth);
    }

    public override void Kill()
    {
        hullHealth = 0f; engineHealth = 0f;
        NotifyHealthChanged(hullHealth, maxHullHealth);
        NotifyArmourChanged(engineHealth, maxEngineHealth);
        NotifyDeath();
    }
    public override void Revive(float healthAmount)
    {
        if (getIsAlive) return;
        hullHealth   = Mathf.Clamp(healthAmount, 1f, maxHullHealth);
        engineHealth = maxEngineHealth * 0.25f; // revived engine at 25 %
        NotifyHealthChanged(hullHealth, maxHullHealth);
        NotifyArmourChanged(engineHealth, maxEngineHealth);
    }
}
```

**Crucial extension note:** `getArmourPercent` is reused as the engine bar slot — your existing `StackableHealthBarReaction` immediately becomes a hull/engine display. No new reaction class needed.

---

### NetworkHealthSystem
Server-authoritative health. The local client never calculates damage — it only applies state sent from the server.

```csharp
public class NetworkHealthSystem : HealthSystemBase
{
    private float currentHealth;
    private float maxHealth = 100f;
    private float currentArmour;
    private float maxArmour  = 50f;

    public override float getHealthPercent => currentHealth / maxHealth;
    public override float getArmourPercent => currentArmour / maxArmour;

    /// <summary>Called by your Netcode / Mirror / Photon layer on state sync.</summary>
    public void ApplyServerState(float health, float armour, float hMax, float aMax)
    {
        maxHealth     = hMax;
        maxArmour     = aMax;
        currentHealth = health;
        currentArmour = armour;

        NotifyHealthChanged(currentHealth, maxHealth);
        NotifyArmourChanged(currentArmour, maxArmour);

        if (currentHealth <= 0f) NotifyDeath();
    }

    // Local calls are intentionally no-ops — server is authoritative
    public override void TakeDamage(float amount) { }
    public override void Heal(float amount)        { }
    public override void RepairArmour(float amount){ }
    public override void Kill()                    { }
    public override void Revive(float healthAmount){ }
}
```

**Crucial extension note:** The entire reaction layer (health bars, vignette, sounds) works identically for networked entities. The server pushes state, `ApplyServerState` fires the events, and every subscriber updates — no special network-aware UI code needed anywhere.

---

### GodModeHealthSystem
Invincible entity for cutscenes, debug sessions, or tutorial sequences.

```csharp
public class GodModeHealthSystem : HealthSystemBase
{
    public override float getHealthPercent => 1f;
    public override float getArmourPercent => 1f;
    public override bool  getIsAlive       => true;

    public override void TakeDamage(float amount) { } // no-op
    public override void Kill()                   { } // no-op
    public override void Heal(float amount)        { }
    public override void RepairArmour(float amount){ }
    public override void Revive(float healthAmount){ }
}
```

**Crucial extension note:** Swap `EntityHealthSystem` for `GodModeHealthSystem` in the Inspector at runtime for instant invincibility — zero code changes needed anywhere else.

---

### ZombieHealthSystem
Regenerates health over time. Stays down briefly on "death", then revives automatically.

```csharp
public class ZombieHealthSystem : HealthSystemBase
{
    [SerializeField] private float maxHealth      = 100f;
    [SerializeField] private float regenRate      = 5f;   // hp per second
    [SerializeField] private float reviveDelay    = 4f;   // seconds before auto-revive
    private float currentHealth;
    private float deadTimer;

    private void Awake() => currentHealth = maxHealth;

    public override float getHealthPercent => currentHealth / maxHealth;
    public override float getArmourPercent => 0f;

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        NotifyHealthChanged(currentHealth, maxHealth);
        if (currentHealth <= 0f) { deadTimer = reviveDelay; NotifyDeath(); }
    }

    private void Update()
    {
        if (!getIsAlive)
        {
            deadTimer -= Time.deltaTime;
            if (deadTimer <= 0f) Revive(1f); // auto-revive at 1 hp
            return;
        }

        if (currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(currentHealth + regenRate * Time.deltaTime, maxHealth);
            NotifyHealthChanged(currentHealth, maxHealth);
        }
    }

    public override void Heal(float amount)
    {
        if (!getIsAlive || amount <= 0f) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        NotifyHealthChanged(currentHealth, maxHealth);
    }
    public override void Revive(float healthAmount)
    {
        currentHealth = Mathf.Clamp(healthAmount, 1f, maxHealth);
        NotifyHealthChanged(currentHealth, maxHealth);
    }
    public override void Kill()
    {
        currentHealth = 0f;
        NotifyHealthChanged(currentHealth, maxHealth);
        NotifyDeath();
    }
    public override void RepairArmour(float amount){ }
}
```

---

---

## HealthReactionBase — Implementations

### StackableHealthBarReaction ✅ (implemented)
Standard stacked screen-space bar with smooth lerp fill and colour gradient. Armour bar outline hides when armour is depleted.

---

### WorldSpaceHealthBarReaction
Overhead bar that floats above an enemy in 3D world space. Billboards to always face the camera.

```csharp
public class WorldSpaceHealthBarReaction : HealthReactionBase
{
    [SerializeField] private Transform barRoot;
    [SerializeField] private Transform healthBar3D;
    [SerializeField] private float     lerpSpeed = 8f;

    private void Update()
    {
        if (hSystem == null) return;

        // billboard — always face main camera
        barRoot.LookAt(Camera.main.transform);

        // scale bar on X axis to represent health
        float current = Mathf.Lerp(healthBar3D.localScale.x, targetHealthAmount, Time.deltaTime * lerpSpeed);
        healthBar3D.localScale = new Vector3(current, 1f, 1f);
    }
}
```

**Why it matters:** Works for any enemy type including networked ones. Subscribes to the same events as screen-space bars — zero duplication.

---

### MinimapHealthReaction
Minimap icon that pulses red when the entity is critically low. Turns grey on death.

```csharp
public class MinimapHealthReaction : HealthReactionBase
{
    [SerializeField] private SpriteRenderer minimapIcon;
    [SerializeField] private float          criticalThreshold = 0.25f;

    protected override void OnBind()
    {
        base.OnBind();
        minimapIcon.color = Color.white;
    }

    protected override void OnDeath() => minimapIcon.color = Color.grey;

    private void Update()
    {
        if (hSystem == null) return;

        bool  critical = hSystem.getHealthPercent < criticalThreshold;
        float pulse    = critical ? Mathf.PingPong(Time.time * 4f, 1f) : 1f;
        minimapIcon.color = Color.Lerp(Color.red, Color.white, pulse);
    }
}
```

---

### ScreenVignetteReaction
Full-screen red vignette overlay. Intensifies as player health drops — a staple of modern shooters.

```csharp
public class ScreenVignetteReaction : HealthReactionBase
{
    [SerializeField] private Image vignetteOverlay;
    [SerializeField] private float maxAlpha = 0.65f;

    private void Update()
    {
        if (hSystem == null) return;
        float danger = 1f - hSystem.getHealthPercent;
        vignetteOverlay.color = new Color(1f, 0f, 0f, danger * maxAlpha);
    }
}
```

---

### AudioHealthReaction
Plays audio cues on damage, low HP, and death. No UI at all — pure audio feedback.

```csharp
public class AudioHealthReaction : HealthReactionBase
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   damageClip;
    [SerializeField] private AudioClip   deathClip;
    [SerializeField] private AudioClip   lowHealthLoop;
    [SerializeField] private float       lowHealthThreshold = 0.25f;
    private bool playingLowHealth;

    protected override void OnHealthChanged(float current, float max)
    {
        base.OnHealthChanged(current, max);
        audioSource.PlayOneShot(damageClip);

        bool isLow = hSystem.getHealthPercent < lowHealthThreshold;
        if (isLow && !playingLowHealth)
        {
            audioSource.clip = lowHealthLoop;
            audioSource.loop = true;
            audioSource.Play();
            playingLowHealth = true;
        }
        else if (!isLow && playingLowHealth)
        {
            audioSource.Stop();
            playingLowHealth = false;
        }
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        audioSource.Stop();
        audioSource.PlayOneShot(deathClip);
        playingLowHealth = false;
    }
}
```

**Crucial extension note:** Bind multiple reactions to one system simultaneously. An enemy can have a `WorldSpaceHealthBarReaction` + `AudioHealthReaction` + `MinimapHealthReaction` all from the same `EntityHealthSystem` with no extra wiring.

---

### AnimatorHealthReaction
Drives an Animator parameter so a character limps, staggers, or enters a rage animation at low HP.

```csharp
public class AnimatorHealthReaction : HealthReactionBase
{
    [SerializeField] private Animator animator;
    private static readonly int HealthParam = Animator.StringToHash("HealthPercent");
    private static readonly int DeadParam   = Animator.StringToHash("IsDead");

    private void Update()
    {
        if (hSystem == null) return;
        animator.SetFloat(HealthParam, hSystem.getHealthPercent);
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        animator.SetBool(DeadParam, true);
    }
}
```

---

### AIBehaviourHealthReaction
Changes AI state based on health thresholds. The health system tells the reaction, the reaction tells the AI — health logic never touches AI directly.

```csharp
public class AIBehaviourHealthReaction : HealthReactionBase
{
    [SerializeField] private EnemyAI   ai;
    [SerializeField] private float     retreatThreshold = 0.3f;

    protected override void OnHealthChanged(float current, float max)
    {
        base.OnHealthChanged(current, max);

        if (hSystem.getHealthPercent < retreatThreshold)
            ai.SetState(EnemyAI.State.Retreat);
        else
            ai.SetState(EnemyAI.State.Aggressive);
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        ai.SetState(EnemyAI.State.Dead);
    }
}
```

**Crucial extension note:** This is the biggest win of the reaction pattern. AI, audio, animation, and UI all respond to health without a single direct coupling. You can remove any reaction component from the Inspector and the system keeps working.

---

### TextHealthReaction
Simple TMP label. Debug HUDs, retro-style games, or damage numbers.

```csharp
public class TextHealthReaction : HealthReactionBase
{
    [SerializeField] private TMP_Text label;

    protected override void OnBind()   { base.OnBind(); Refresh(); }
    protected override void OnDeath()  { base.OnDeath(); label.text = "DEAD"; }

    protected override void OnHealthChanged(float c, float m) { base.OnHealthChanged(c, m); Refresh(); }
    protected override void OnArmourChanged(float c, float m) { base.OnArmourChanged(c, m); Refresh(); }

    private void Refresh()
    {
        int hp = Mathf.CeilToInt(hSystem.getHealthPercent * 100f);
        int ar = Mathf.CeilToInt(hSystem.getArmourPercent * 100f);
        label.text = $"HP {hp}  |  AR {ar}";
    }
}
```

---

---

## Many reactions, one system

```csharp
// Any HealthSystemBase subclass pairs with any number of HealthReactionBase subclasses.
// Bind once — the rest is automatic.

worldSpaceBar.Bind(bossSystem);
vignette.Bind(bossSystem);
audio.Bind(bossSystem);
minimap.Bind(bossSystem);
aiReaction.Bind(bossSystem);
```

---

## Crucial future extension points

| Scenario | Approach |
|---|---|
| Multiplayer / server-authoritative | `NetworkHealthSystem` — `ApplyServerState` replaces all local logic |
| Multiple enemies on screen | One `HealthSystemBase` per enemy, one `WorldSpaceHealthBarReaction` per enemy — no shared state |
| Boss phase transitions | Add a custom `OnPhaseChanged` event on top of the base contract |
| VFX on damage (sparks, blood) | New `VFXHealthReaction` — bind it alongside the bar, no existing code changes |
| Save/load | Serialize `currentHealth` and `currentArmour` from the system only — reactions reconstruct from `OnBind` |
| Damage types (fire, poison, pierce) | Extend `TakeDamage(float amount, DamageType type)` — armour can resist some types |
| On-screen damage numbers | New `DamageNumberReaction` — subscribe to `OnHealthChanged`, diff the delta, spawn floating text |
| Spectator / observer mode | Bind a reaction to any system in the scene — read-only, no game state impact |

---

## Known issues to fix before shipping

### `Kill()` missing notify calls in `EntityHealthSystem`
The UI bar does not snap to zero — it lerps there on its own after `OnDeath` fires, which can look wrong on instant-kill effects.

```csharp
public override void Kill()
{
    if (!getIsAlive) return;
    currentHealth = 0f;
    currentArmour = 0f;
    NotifyHealthChanged(currentHealth, maxHealth); // ← add
    NotifyArmourChanged(currentArmour, maxArmour); // ← add
    NotifyDeath();
}
```

### `OnHealthChanged` / `OnArmourChanged` / `OnDeath` not virtual in `HealthReactionBase`
Subclasses like `TextHealthReaction` and `AudioHealthReaction` need to override event handlers to react precisely, but the base class declares them as non-virtual `protected void`. Mark them virtual so subclasses can override without polling in `Update`.

```csharp
protected virtual void OnHealthChanged(float current, float max) { ... }
protected virtual void OnArmourChanged(float current, float max) { ... }
protected virtual void OnDeath() { ... }
```
