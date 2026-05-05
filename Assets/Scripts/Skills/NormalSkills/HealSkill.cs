using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Normal/Heal", fileName = "Skill_Heal")]
public class HealSkill : NormalSkill
{
    [SerializeField] private float _healAmount = 20f;

    public override void Execute(NPC caster, GameObject target = null)
    {
        // Find the most injured party member (companions + caster).
        // Falls back to healing the caster if GameManager is unavailable.
        NPC recipient = FindMostInjured(caster);
        recipient.Heal(_healAmount);
        Debug.Log($"[HealSkill] {caster.name} heals {recipient.name} for {_healAmount}. " +
                  $"{recipient.name} HP: {recipient.Health}");
    }

    private NPC FindMostInjured(NPC caster)
    {
        if (GameManager.Instance == null) return caster;

        NPC  mostInjured = caster;
        float lowestRatio = caster is CompanionAI c
            ? c.Health / c.MaxHealth
            : 1f;

        foreach (CompanionAI companion in GameManager.Instance.Companions)
        {
            float ratio = companion.Health / companion.MaxHealth;
            if (ratio < lowestRatio)
            {
                lowestRatio  = ratio;
                mostInjured  = companion;
            }
        }

        return mostInjured;
    }
}
