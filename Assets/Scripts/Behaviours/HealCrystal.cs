using UnityEngine;

public class HealCrystal : Crystal
{
    public override float damage { get; set; } = 10;
    private float HealRadius=5;
    private float MinHeal = 20;
    private float MaxHeal=110;

    override protected void Hit(Collider collision)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, HealRadius, layer);
        foreach (Collider other in colliders)
        {

            //object is in layer
            bool directLayerMatch = (layer.value & (1 << other.gameObject.layer)) != 0;

            // object parent is in layer
            bool parentLayerMatch = other.transform.parent != null &&
                                    (layer.value & (1 << other.transform.parent.gameObject.layer)) != 0;

            if (!directLayerMatch && !parentLayerMatch) return;

            // IHealthable
            IHealthable healthable = null;

            if (other.transform.parent != null)
                other.transform.parent.TryGetComponent(out healthable);

            if (healthable == null)
                other.TryGetComponent(out healthable);

            if (healthable != null && healthable.CanBeHurt)
            {
                float distance = Vector3.Distance(transform.position, other.transform.position);
                float healAmount = Mathf.Lerp(MaxHeal, MinHeal, distance / HealRadius);
            
               
              
                healthable.Heal(healAmount);
               

            }



        }
        Destroy(gameObject);
    }
}
