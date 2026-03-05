Using your exact `HealthUIBase` as the example throughout.

---

## `abstract` — *must* override, no default exists
The base has **no idea** how to implement it. Every subclass is forced to provide their own version. Compile error if they don't.

```csharp
// HealthUIBase
protected abstract void OnDeath();

// StackableHealthBarUI — MUST implement or won't compile
protected override void OnDeath() => _targetHealth = 0f;

// A future WorldSpaceHealthBarUI — also MUST implement
protected override void OnDeath() => gameObject.SetActive(false);
```

> Use when: the behaviour is **guaranteed to vary** per subclass and there is no sensible default.

---

## `virtual` — *can* override, has a default
The base provides a working fallback. Subclasses may override but don't have to.

```csharp
// HealthUIBase
protected virtual void OnBind() { } // default does nothing, that's fine

// StackableHealthBarUI — CHOOSES to override to seed initial values
protected override void OnBind()
{
    _targetHealth = System.HealthPercent;
    _targetArmour = System.ArmourPercent;
}

// A future SimpleHealthBarUI — doesn't override, gets empty OnBind(), works fine
```

> Use when: there's a **reasonable default** but subclasses might want to extend it.

---

## non-virtual / non-abstract — *cannot* override, shared by all
Sealed behaviour. Every subclass gets exactly this, no exceptions. This is where you put logic that must **never** differ.

```csharp
// HealthUIBase
public void Bind(HealthSystemBase healthSystem)
{
    Unbind();
    System = healthSystem;
    System.OnHealthChanged += OnHealthChanged;
    System.OnArmourChanged += OnArmourChanged;
    System.OnDeath         += OnDeath;
    OnBind();
}

// StackableHealthBarUI — cannot override Bind, doesn't need to
// WorldSpaceHealthBarUI — cannot override Bind, doesn't need to
// ALL subclasses get identical wiring behaviour
```

> Use when: the behaviour must be **identical everywhere** and you never want a subclass breaking it.

---

## Side by side in your base class

```csharp
public abstract class HealthUIBase : MonoBehaviour
{
    //  non-virtual  → shared plumbing, no one overrides this
    public void Bind(HealthSystemBase healthSystem) { ... }
    public void Unbind() { ... }

    //  virtual      → default is empty, subclass may seed initial values
    protected virtual void OnBind() { }

    //  abstract     → no default possible, every UI handles these differently
    protected abstract void OnHealthChanged(float current, float max);
    protected abstract void OnArmourChanged(float current, float max);
    protected abstract void OnDeath();
}
```

----

## Quick rule of thumb

Keyword   | Override required?  | Has default?    | Use for
------------------------------------------------------------------------------------
abstract  | ✅Yes               | ❌ No          | behaviour that *must* differ
virtual   | ❌ optional         | ✅Yes          | behaviour that *might* differ
neither   | ❌ blocked          | ✅Yes          | behaviour that *must not* differ