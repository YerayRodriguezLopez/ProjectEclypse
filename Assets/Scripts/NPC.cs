using System.Collections;
using UnityEngine;

public abstract class NPC : MonoBehaviour, IHealthable
{
    public abstract float Health { get; set; }
    public abstract float MaxHealth { get; set; }

    //[SerializeField] private float _health;

    public abstract float Damage { get; set; }

    //[SerializeField] private float _damage;
    public abstract float AttackCooldown { get; set; }

    //[SerializeField] private float _attackCooldown = 1;
    public abstract float AttackSpeed{ get; set; }

    //[SerializeField] private float _attackSpeed = 1;
    public abstract float AttackRange{ get; set; }

    public  float ITime { get; set; } = 0.5f;

    public bool CanBeHurt { get; set; } = true;

    //[SerializeField] private float _attackRange = 2;
    public abstract void Die();
    public virtual void TakeDamage(float damage)
    {
        if (CanBeHurt)
        {
            Health -= damage;
            if (Health <= 0) Die();
            else StartCoroutine(InvulnerabilityCD());
        }
    }
    public abstract void Attack();

    public virtual void Heal(float heal)
    {
        Health += heal;
    }

    public virtual IEnumerator InvulnerabilityCD()
    {
        CanBeHurt = false;
        yield return new WaitForSeconds(ITime);
        CanBeHurt = true;
    }
}
