using UnityEngine;

public interface IHurtable
{
    float Health { get; }
    void TakeDamage(float damage);
    void Die();
}
