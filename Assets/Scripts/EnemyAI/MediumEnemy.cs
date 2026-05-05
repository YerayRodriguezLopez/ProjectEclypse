using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MediumEnemy : SimpleEnemy
{
    public override float StunDuration { get; set; } = 0.5f;
    public override bool IsStunned { get; set; } = false;
    public override float VisionDistance { get; set; } = 15;
    public override float Health { get; set; } = 100;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 1.5f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;
    public override float Speed { get; set; } = 2f;
    public override float MaxHealth { get; set; } = 100;


    [SerializeField] private List<AnimationClip> Attacks;



    private Coroutine attackCoroutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChooseState();
    }

    private void Update()
    {
        
    }

    public override void ChooseState()
    {
        if (Health <= 0) Die();
        else if (IsStunned) return;
        else if (Target != null && !IsStunned)
        {
            float distance = Vector3.Distance(Target.transform.position, this.transform.position);
        
            if (distance <= AttackRange)
            {
                Attack();
            }
            else if (distance <= VisionDistance)
            {
           
                Chase();
            }
            else
            {
                Target = null;
                //idle?
            }

        }

        //idle?
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.gameObject.layer == 3 || other.transform.gameObject.layer == 6)
        {
            // Intentamos obtener IHurtable del objeto detectado
            if (other.TryGetComponent<IHealthable>(out IHealthable newHurtable))
            {
                if (Target == null)
                {
                    // No había target, asignamos directamente
                    Target = other.gameObject;
                    ChooseState();
                }
                else
                {
                    // Comparamos vida con el target actual
                    if (Target.TryGetComponent<IHealthable>(out IHealthable currentHurtable))
                    {
                        if (newHurtable.Health > currentHurtable.Health)
                        {
                            Target = other.gameObject;
                            ChooseState();
                        }
                    }
                }
            }
        }
    }


    private bool canAttack = true;

    public override void Attack()
    {
        if (!canAttack) return;

        canAttack = false;
        attackCoroutine = StartCoroutine(AttackRoutine());
        ChooseState();
    }

    private IEnumerator AttackRoutine()
    {
        Debug.Log("AttackRoutine");

        if (Target.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
        {
            //metodo propio de ataque de cada enemigo

            int attackIndex = Random.Range(0, Attacks.Count);
            //AttackAnimation(attackIndex)
            Debug.Log("te pego");
            hurtableTarget.TakeDamage(this.Damage);
        }

        yield return new WaitForSeconds(AttackCooldown);

        canAttack = true;
        ChooseState();
    }





    public override void Die()
    {

    }

    public override void Pull()
    {

    }

    public override void Stun()
    {
        base.Stun();
    }


    public override void ClearStun()
    {
       base.ClearStun();
    }

    public override IEnumerator ClearStunRutine()
    {
        yield return base.ClearStunRutine();
    }

    public override void TakeDamage(float damage)
    {
        //this.Health -= damage;
        //ChooseState();
        base.TakeDamage(damage);
    }

    public override void Chase()
    {
        base.Chase();
    }

    public override IEnumerator ChaseRoutine( )
    {
        yield return base.ChaseRoutine();

    }
}
