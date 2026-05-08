using UnityEngine;
using UnityEngine.Serialization;

// ─────────────────────────────────────────────────────────────────────────────
// NORMAL SKILLS
// ─────────────────────────────────────────────────────────────────────────────

[CreateAssetMenu(menuName = "Skills/Normal/HeavyAttack", fileName = "Skill_HeavyAttack")]
public class HeavyAttackSkill : NormalSkill
{
    [SerializeField] private float damageMultiplier = 2.5f;

    public override void Execute(NPC caster, GameObject target = null)
    {
        if (!target || !target.TryGetComponent(out IHealthable hurtable)) return;

        float hit = caster.Damage * damageMultiplier;
        Debug.Log($"[HeavyAttackSkill] {caster.name}: heavy hit → {target.name} for {hit}.");
        hurtable.TakeDamage(hit);
    }
}
