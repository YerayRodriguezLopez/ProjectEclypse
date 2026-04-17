using System.Collections;
using UnityEngine;

public interface IHurtable
{
    float ITime { get; }
    bool CanBeHurt { get; }
    float Health { get; }
    void TakeDamage(float damage);
    void Heal(float heal);
    void Die();
    IEnumerator InvulnerabilityCD();
}
