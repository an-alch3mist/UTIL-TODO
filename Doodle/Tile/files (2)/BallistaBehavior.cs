using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Ballista tower behavior — attached directly to the B-slab part prefab
/// (the "main body" part, not the entrance or block parts).
///
/// This demonstrates building-LEVEL behavior that lives on ONE part:
/// the big body prefab is the natural home for the attack logic.
///
/// It doesn't care about individual tile offsets — it just shoots things.
/// </summary>
public class BallistaBehavior : MonoBehaviour, IPartBehavior
{
    [Header("Combat")]
    public float range        = 5f;
    public float attackRate   = 1f;    // shots per second
    public int   damage       = 10;
    public Transform muzzlePoint;      // assign in prefab

    [Header("Projectile")]
    public GameObject projectilePrefab;

    private BuildingInstance _owner;
    private float            _cooldown;
    private Transform        _target;

    // ═════════════════════════════════════════════════════════════════════════
    //  IPartBehavior
    // ═════════════════════════════════════════════════════════════════════════

    public void OnPlaced(BuildingInstance owner, int partIndex)
    {
        _owner = owner;
        // Could register with a TowerManager here
    }

    public void OnRemoved()
    {
        _target = null;
    }

    public void OnMoved()
    {
        _target = null; // Re-acquire after move
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Update
    // ═════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        _cooldown -= Time.deltaTime;

        if (_target == null || !TargetInRange(_target))
            _target = AcquireTarget();

        if (_target != null && _cooldown <= 0f)
        {
            Shoot(_target);
            _cooldown = 1f / attackRate;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Private
    // ═════════════════════════════════════════════════════════════════════════

    private Transform AcquireTarget()
    {
        // Simple sphere overlap — swap with your enemy manager query
        var cols = Physics.OverlapSphere(transform.position, range);
        foreach (var col in cols)
        {
            if (col.CompareTag("Enemy"))
                return col.transform;
        }
        return null;
    }

    private bool TargetInRange(Transform t) =>
        Vector3.Distance(transform.position, t.position) <= range;

    private void Shoot(Transform target)
    {
        if (projectilePrefab == null) return;
        var origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
        var proj   = Instantiate(projectilePrefab, origin, Quaternion.identity);
        // Hand off target to projectile — your projectile script handles the rest
        // proj.GetComponent<Projectile>().Init(target, damage);
        Debug.Log($"[Ballista] Fired at {target.name} from {_owner.data.buildingName}@{_owner.pivotCoord}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
