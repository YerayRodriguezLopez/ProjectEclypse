using UnityEngine;

public class HealCrystal : Crystal
{
    [SerializeField]
    private float HealRadius;
    [SerializeField]
    private float MinHeal;
    [SerializeField]
    private float MaxHeal;

    private void CreateHealArea()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, HealRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.TryGetComponent(out IHurtable healable))
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float healAmount = Mathf.Lerp(MaxHeal, MinHeal, distance / HealRadius);
                healable.Heal(healAmount);
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (ToBeThrown)
        {
            if (collision.gameObject.TryGetComponent(out IHurtable healable))
            {
                CreateHealArea();
                Destroy(gameObject);
            }
        }
    }
}
