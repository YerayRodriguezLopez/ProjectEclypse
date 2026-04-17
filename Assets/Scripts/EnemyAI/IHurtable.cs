using System.Collections;
using UnityEngine;

public interface IHurtable
{
    float iTime { get; }
    bool canBeHurt { get; }
    float Health { get; }
    void TakeDamage(float damage);
    void Heal(float heal);
    void Die();
    IEnumerable InvulnerabilityCD();
}
