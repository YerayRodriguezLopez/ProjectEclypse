using UnityEngine;

public class ExplosiveCrystal : Crystal
{
    [SerializeField]
    private float MaxExplosionDamage;
    [SerializeField]
    private float MinExplosionDamage;
    [SerializeField]
    private float ExplosionRadius;
    //Use spherecast to detect all objects in the explosion radius and apply damage based on distance from explosion center
    override protected void Hit(Collision collision)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, ExplosionRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.CompareTag("Player") || collider.gameObject.CompareTag("Companion") || collider.gameObject.CompareTag("Enemy"))
            {
                if (collider.gameObject.TryGetComponent<IHealthable>(out var hurtable))
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    float damageAmount = Mathf.Lerp(MaxExplosionDamage, MinExplosionDamage, distance / ExplosionRadius);
                    hurtable.TakeDamage(damageAmount);
                }
            }
        }
        Destroy(gameObject);
    }
}
