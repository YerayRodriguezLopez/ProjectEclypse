using UnityEngine;

public abstract class NPC : MonoBehaviour, IHurtable
{
    public abstract float Health { get; set; }

    //[SerializeField] private float _health;

    public abstract float Damage { get; set; }

    //[SerializeField] private float _damage;
    public abstract float AttackCooldown { get; set; }

    //[SerializeField] private float _attackCooldown = 1;
    public abstract float AttackSpeed{ get; set; }

    //[SerializeField] private float _attackSpeed = 1;
    public abstract float AttackRange{ get; set; }

    //[SerializeField] private float _attackRange = 2;
    public abstract void Die();
    public abstract void TakeDamage(float damage);
    public abstract void Attack(GameObject Target);
}
