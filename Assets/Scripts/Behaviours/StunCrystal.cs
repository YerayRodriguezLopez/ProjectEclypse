using UnityEngine;

public class StunCrystal : Crystal
{
    [SerializeField] private float Damage;
    protected override void Hit(Collider other)
    {
        if (other.transform.parent.TryGetComponent<IStunnable>(out var stunnable))
        {
            stunnable.Stun();
        }
        if (other.transform.parent.TryGetComponent<IHealthable>(out var damageable))
        {
            damageable.TakeDamage(Damage);
        }
        Destroy(gameObject);
    }
}
