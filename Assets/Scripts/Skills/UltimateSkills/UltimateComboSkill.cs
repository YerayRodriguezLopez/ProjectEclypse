using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Ultimate/UltimateCombo", fileName = "Skill_UltimateCombo")]
public class UltimateComboSkill : UltimateSkill
{
    [SerializeField] private int   _hitCount         = 5;
    [SerializeField] private float _damageMultiplier = 0.8f;

    // Execute() is intentionally empty — this skill always runs as a coroutine.
    public override void Execute(NPC caster, GameObject target = null) { }

    /// <summary>
    /// Delivers _hitCount hits, each separated by the target's ITime so that
    /// InvulnerabilityCD fully resets between every strike.
    /// </summary>
    public override IEnumerator ExecuteCoroutine(NPC caster, GameObject target = null)
    {
        if (target == null || !target.TryGetComponent(out IHurtable hurtable))
        {
            Debug.Log($"[UltimateComboSkill] {caster.name}: no valid target.");
            yield break;
        }

        float hit = caster.Damage * _damageMultiplier;
        Debug.Log($"[UltimateComboSkill] {caster.name}: {_hitCount}-hit combo " +
                  $"({hit} per hit) on {target.name}!");

        for (int i = 0; i < _hitCount; i++)
        {
            // Check the target is still alive before each strike.
            if (hurtable.Health <= 0) yield break;

            Debug.Log($"[UltimateComboSkill] hit {i + 1}/{_hitCount} → {target.name} for {hit}.");
            hurtable.TakeDamage(hit);

            // Only wait between hits, not after the last one.
            if (i < _hitCount - 1)
                yield return new WaitForSeconds(hurtable.ITime);
        }
    }
}