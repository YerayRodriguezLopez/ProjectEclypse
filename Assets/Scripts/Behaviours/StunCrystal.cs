using UnityEngine;

public class StunCrystal : Crystal
{
    [SerializeField]
    private float StunTime;

    private void OnCollisionEnter(Collision collision)
    {
        if (ToBeThrown)
        {
            if (collision.gameObject.TryGetComponent(out IHurtable damageable))
            {
                damageable.TakeDamage(Damage);
            }
            if (collision.gameObject.TryGetComponent(out IStunnable stunnable))
            {
                stunnable.Stun();
            }
            Destroy(gameObject);
        }
    }
}
