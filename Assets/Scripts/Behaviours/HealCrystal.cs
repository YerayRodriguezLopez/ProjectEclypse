using UnityEngine;

public class HealCrystal : Crystal
{
    [SerializeField]
    private float HealRadius;
    [SerializeField]
    private float MinHeal;
    [SerializeField]
    private float MaxHeal;

    protected override void Hit(Collision collision)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, HealRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.CompareTag("Player") || collider.gameObject.CompareTag("Companion"))
            {
                if (collider.gameObject.TryGetComponent<IHurtable>(out var hurtable))
                {
                    float healAmount = Random.Range(MinHeal, MaxHeal);
                    hurtable.Heal(healAmount);
                }
            }
        }
        Destroy(gameObject);
    }
}
