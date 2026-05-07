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

    public virtual float ITime { get; set; } = 1f;

    public virtual bool CanBeHurt { get; set; } = true;

    public  Coroutine InvulnerableCorutine = null;
    //[SerializeField] private float _attackRange = 2;
    public abstract void Die();
    private void Update()
    {
        //Debug.Log("can be hurt: " + this.CanBeHurt);
    }
    public virtual void TakeDamage(float damage)
    {
        if (this.CanBeHurt)
        {
            Debug.Log("ouch " + damage);

            Health -= damage;
            this.CanBeHurt = false;
            if (Health <= 0)
            {
                Die();
            }
            else
            {
                if (InvulnerableCorutine == null)
                    InvulnerableCorutine = StartCoroutine(InvulnerabilityCD());
            }

        }
    }
    public abstract void Attack();

    public virtual void Heal(float heal)
    {
        Health += heal;
    }

    public virtual IEnumerator InvulnerabilityCD()
    {
        Debug.Log("Empiezo");
        
        yield return new WaitForSeconds(ITime);
        this.CanBeHurt = true;
        Debug.Log("acabo");
        InvulnerableCorutine = null;
    }
}
