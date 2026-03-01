# HealthSystem Base Classes — Extension Guide

---

## `HealthSystemBase` Implementations

### EntityHealthSystem
Standard enemy / player. Armour absorbs first, then health.
```csharp
public class EntityHealthSystem : HealthSystemBase
{
    // full armour → health → death flow
}
```

---

### BarrelHealthSystem
Destructible prop. No armour, no revive.
```csharp
public class BarrelHealthSystem : HealthSystemBase
{
    public override float getArmourPercent => 0f;

    public override void TakeDamage(float amount)
    {
        currentHealth -= amount;
        NotifyHealthChanged();
        if (currentHealth <= 0f) NotifyDeath();
    }

    public override void Revive(float h)       { } // props don't revive
    public override void RepairArmour(float a) { } // no armour
}
```

---

### BossHealthSystem
Multi-phase boss. Different behaviour per phase.
```csharp
public class BossHealthSystem : HealthSystemBase
{
    private int phase = 1;

    public override void TakeDamage(float amount)
    {
        if (phase == 1) amount *= 0.5f;   // phase 1 — damage resistance

        currentHealth -= amount;
        NotifyHealthChanged();

        if (currentHealth < maxHealth * 0.5f && phase == 1)
        {
            phase = 2;                    // enter rage phase
            NotifyHealthChanged();
        }

        if (currentHealth <= 0f) NotifyDeath();
    }
}
```

---

### ShieldHealthSystem
Regenerating shield. No permanent armour — shield recharges after delay.
```csharp
public class ShieldHealthSystem : HealthSystemBase
{
    [SerializeField] private float rechargeDelay  = 3f;
    [SerializeField] private float rechargeRate   = 10f;
    private float rechargeTimer;

    public override void TakeDamage(float amount)
    {
        rechargeTimer = rechargeDelay;    // reset recharge on hit

        if (currentArmour > 0f)
        {
            float absorbed = Mathf.Min(currentArmour, amount);
            currentArmour -= absorbed;
            amount        -= absorbed;
            NotifyArmourChanged();
        }

        if (amount > 0f)
        {
            currentHealth -= amount;
            NotifyHealthChanged();
            if (currentHealth <= 0f) NotifyDeath();
        }
    }

    private void Update()
    {
        if (rechargeTimer > 0f)
        {
            rechargeTimer -= Time.deltaTime;
            return;
        }

        if (currentArmour < maxArmour)
        {
            currentArmour = Mathf.Min(currentArmour + rechargeRate * Time.deltaTime, maxArmour);
            NotifyArmourChanged();
        }
    }
}
```

---

### VehicleHealthSystem
Separate hull and engine health. Dead when either reaches zero.
```csharp
public class VehicleHealthSystem : HealthSystemBase
{
    [SerializeField] private float maxEngineHealth = 50f;
    private float engineHealth;

    public override bool getIsAlive => currentHealth > 0f && engineHealth > 0f;

    public void TakeEngineDamage(float amount)
    {
        engineHealth = Mathf.Max(engineHealth - amount, 0f);
        NotifyHealthChanged();
        if (engineHealth <= 0f) NotifyDeath();
    }
}
```

---

### NetworkHealthSystem
Receives health state from server. Local client never calculates damage.
```csharp
public class NetworkHealthSystem : HealthSystemBase
{
    // Called by your network layer when server sends a health update
    public void ApplyServerState(float health, float armour)
    {
        currentHealth = health;
        currentArmour = armour;
        NotifyHealthChanged();
        NotifyArmourChanged();
        if (currentHealth <= 0f) NotifyDeath();
    }

    // Damage is server-authoritative — local calls are blocked
    public override void TakeDamage(float amount) { }
    public override void Kill()                   { }
}
```

---

### GodModeHealthSystem
Invincible entity for cutscenes, tutorials, or debug purposes.
```csharp
public class GodModeHealthSystem : HealthSystemBase
{
    public override bool  getIsAlive       => true;
    public override float getHealthPercent => 1f;
    public override float getArmourPercent => 1f;

    public override void TakeDamage(float amount) { }  // no-op
    public override void Kill()                   { }  // no-op
    public override void Revive(float h)          { }
    public override void RepairArmour(float a)    { }
    public override void Heal(float a)            { }
}
```

---

---

## `HealthUIBase` Implementations

### StackableHealthBarUI
Standard stacked screen-space bar. Armour on top, health below.
```csharp
public class StackableHealthBarUI : HealthUIBase
{
    [SerializeField] Image healthFillImage;
    [SerializeField] Image armourFillImage;

    private void Update()
    {
        healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetHealthAmount, Time.deltaTime * lerpSpeed);
        armourFillImage.fillAmount = Mathf.Lerp(armourFillImage.fillAmount, targetArmourAmount, Time.deltaTime * lerpSpeed);
    }
}
```

---

### WorldSpaceHealthBarUI
Overhead bar that floats above an enemy in 3D world space.
```csharp
public class WorldSpaceHealthBarUI : HealthUIBase
{
    [SerializeField] private Transform barRoot;
    [SerializeField] private Transform healthBar3D;

    private void Update()
    {
        // billboard — always faces the camera
        barRoot.LookAt(Camera.main.transform);

        // scale bar on X axis to represent health
        healthBar3D.localScale = new Vector3(targetHealthAmount, 1f, 1f);
    }
}
```

---

### MinimapHealthUI
Minimap icon that pulses red when the entity is critically low.
```csharp
public class MinimapHealthUI : HealthUIBase
{
    [SerializeField] private SpriteRenderer minimapIcon;

    protected override void OnBind()
    {
        base.OnBind();
        minimapIcon.color = Color.white;
    }

    protected override void OnDeath() => minimapIcon.color = Color.grey;

    private void Update()
    {
        if (hSystem == null) return;

        bool critical = hSystem.getHealthPercent < 0.25f;
        float pulse   = critical ? Mathf.PingPong(Time.time * 4f, 1f) : 1f;
        minimapIcon.color = Color.Lerp(Color.red, Color.white, pulse);
    }
}
```

---

### ScreenVignetteHealthUI
Full-screen red vignette overlay. Intensifies as player health drops.
```csharp
public class ScreenVignetteHealthUI : HealthUIBase
{
    [SerializeField] private Image vignetteOverlay;

    private void Update()
    {
        if (hSystem == null) return;

        float danger = 1f - hSystem.getHealthPercent;
        vignetteOverlay.color = new Color(1f, 0f, 0f, danger * 0.65f);
    }
}
```

---

### TextHealthUI
Simple TMP label. Useful for debug HUDs or retro-style games.
```csharp
public class TextHealthUI : HealthUIBase
{
    [SerializeField] private TMP_Text label;

    protected override void OnBind() => Refresh();

    protected override void OnDeath() => label.text = "DEAD";

    private void Refresh()
    {
        int hp = Mathf.CeilToInt(hSystem.getHealthPercent * 100f);
        int ar = Mathf.CeilToInt(hSystem.getArmourPercent * 100f);
        label.text = $"HP {hp}  |  AR {ar}";
    }

    // re-use base target floats as the trigger to refresh label
    private void Update()
    {
        if (hSystem != null) Refresh();
    }
}
```

---

### AnimatedCharacterHealthUI
Drives an Animator parameter so a character limps or staggers at low HP.
```csharp
public class AnimatedCharacterHealthUI : HealthUIBase
{
    [SerializeField] private Animator animator;
    private static readonly int HealthParam = Animator.StringToHash("HealthPercent");

    private void Update()
    {
        if (hSystem == null) return;
        animator.SetFloat(HealthParam, hSystem.getHealthPercent);
    }
}
```

---

## Bind once — works with everything

```csharp
// Any HealthSystemBase subclass pairs with any HealthUIBase subclass
// The demo, AI, and all UI code never change

HealthSystemBase system = barrel;          // or boss, vehicle, shield, network...
HealthUIBase     ui     = worldSpaceBar;   // or vignette, minimap, text, animator...

ui.Bind(system); // that's it
```