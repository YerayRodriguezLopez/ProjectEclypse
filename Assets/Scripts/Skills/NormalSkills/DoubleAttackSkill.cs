using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Normal/DoubleAttack", fileName = "Skill_DoubleAttack")]
public class DoubleAttackSkill : NormalSkill
{
    [SerializeField] private float _damageMultiplier = 1f;

    // Execute() is intentionally empty — this skill always runs as a coroutine.
    public override void Execute(NPC caster, GameObject target = null) { }

    /// <summary>
    /// Delivers two hits with a gap equal to the target's ITime between them,
    /// so the InvulnerabilityCD fully resets before the second hit lands.
    /// </summary>
    public override IEnumerator ExecuteCoroutine(NPC caster, GameObject target = null)
    {
        if (target == null || !target.TryGetComponent(out IHurtable hurtable))
        {
            Debug.Log($"[DoubleAttackSkill] {caster.name}: no valid target.");
            yield break;
        }

        float hit = caster.Damage * _damageMultiplier;

        Debug.Log($"[DoubleAttackSkill] {caster.name}: hit 1 → {target.name} for {hit}.");
        hurtable.TakeDamage(hit);

        // Wait for ITime so InvulnerabilityCD resets before the second hit.
        yield return new WaitForSeconds(hurtable.ITime);

        Debug.Log($"[DoubleAttackSkill] {caster.name}: hit 2 → {target.name} for {hit}.");
        hurtable.TakeDamage(hit);
    }
}