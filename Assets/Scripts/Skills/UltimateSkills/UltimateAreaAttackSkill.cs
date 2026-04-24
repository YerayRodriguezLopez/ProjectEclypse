using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ULTIMATE SKILLS
// ─────────────────────────────────────────────────────────────────────────────

[CreateAssetMenu(menuName = "Skills/Ultimate/UltimateAreaAttack", fileName = "Skill_UltimateAreaAttack")]
public class UltimateAreaAttackSkill : UltimateSkill
{
    [SerializeField] private float     _radius          = 5f;
    [SerializeField] private float     _damageMultiplier = 3f;
    [SerializeField] private LayerMask _enemyLayer;

    public override void Execute(NPC caster, GameObject target = null)
    {
        float      damage = caster.Damage * _damageMultiplier;
        Collider[] hits   = Physics.OverlapSphere(caster.transform.position, _radius, _enemyLayer);

        Debug.Log($"[UltimateAreaAttackSkill] {caster.name}: area hit for {damage} " +
                  $"in radius {_radius}. Targets hit: {hits.Length}");

        foreach (Collider col in hits)
        {
            if (col.gameObject == caster.gameObject) continue;

            // Each target has its own CanBeHurt / InvulnerabilityCD, so a single
            // area hit landing on each one at the same frame is fine — the CD is
            // per-target, not global.
            if (col.TryGetComponent(out IHurtable hurtable))
                hurtable.TakeDamage(damage);
        }
    }
}
