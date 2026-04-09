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
    private void CreateExplosionArea()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, ExplosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.TryGetComponent(out IHurtable damageable))
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float damage = Mathf.Lerp(MaxExplosionDamage, MinExplosionDamage, distance / ExplosionRadius);
                damageable.TakeDamage(damage);
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (ToBeThrown)
        {
            if (collision.gameObject.TryGetComponent(out IHurtable damageable))
            {
                damageable.TakeDamage(Damage);
            }
            CreateExplosionArea();
            Destroy(gameObject);
        }
    }
}
