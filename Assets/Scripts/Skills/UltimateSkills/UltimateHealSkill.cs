using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Ultimate/UltimateHeal", fileName = "Skill_UltimateHeal")]
public class UltimateHealSkill : UltimateSkill
{
    [SerializeField] private float _healAmount = 100f;

    public override void Execute(NPC caster, GameObject target = null)
    {
        if (GameManager.Instance == null)
        {
            // Fallback: heal caster only.
            caster.Heal(_healAmount);
            Debug.Log($"[UltimateHealSkill] {caster.name}: healed self (no GameManager).");
            return;
        }

        // Heal all registered companions.
        foreach (CompanionAI companion in GameManager.Instance.Companions)
        {
            companion.Heal(_healAmount);
            Debug.Log($"[UltimateHealSkill] Healed {companion.name} for {_healAmount}. " +
                      $"HP: {companion.Health}");
        }

        // Heal the player if it implements IHurtable.
        if (GameManager.Instance.Player != null &&
            GameManager.Instance.Player.TryGetComponent(out IHurtable playerHurtable))
        {
            playerHurtable.Heal(_healAmount);
            Debug.Log($"[UltimateHealSkill] Healed player for {_healAmount}.");
        }
    }
}
