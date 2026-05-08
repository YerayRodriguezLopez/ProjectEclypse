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
    public override float AttackCooldown { get; set; } = 3f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;
    public override float Speed { get; set; } = 2f;
    public override float MaxHealth { get; set; } = 100;


    //[SerializeField] private List<AnimationClip> Attacks;

    public Animator animator;


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
                animator.SetBool("IsAt2", false);
                animator.SetBool("IsAt1", false);
                animator.SetBool("IsAtCh", false);
                animator.SetBool("IsMoving", false);
                Attack();
            }
            else if (distance <= VisionDistance)
            {
                animator.SetBool("IsAt2", false);
                animator.SetBool("IsAt1", false);
                animator.SetBool("IsAtCh", false);
                animator.SetBool("IsMoving", true);

                Chase();
            }
            else
            {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAt2", false);
                animator.SetBool("IsAt1", false);
                animator.SetBool("IsAtCh", false);
                Target = null;
                
            }

        }

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.gameObject.layer == 3 || other.transform.gameObject.layer == 6)
        {
            if (other.TryGetComponent<IHealthable>(out IHealthable newHurtable))
            {
                if (Target == null)
                {
                    Target = other.gameObject;
                    ChooseState();
                }
                else
                {
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
    //private bool isDealingDamage = false; 

    public override void Attack()
    {
        if (!canAttack) return;

        canAttack = false;
        if (attackCoroutine == null)
        {
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
       
    }

    private IEnumerator AttackRoutine()
    {
        int rand = Random.Range(1, 100);

        animator.SetBool("IsAt2", false);
        animator.SetBool("IsAt1", false);
        animator.SetBool("IsAtCh", false);

        if (rand < 60)
            animator.SetBool("IsAt1", true);
        else if (rand < 90)
            animator.SetBool("IsAt2", true);
        else
            animator.SetBool("IsAtCh", true);


        yield return new WaitForSeconds(AttackCooldown);

        canAttack = true;
        attackCoroutine = null;
        ChooseState();
    }

    public void DealDamageBasic()
    {
        if (Target == null) return; 
        Debug.Log("atac basic");
        if (Target.TryGetComponent<IHealthable>(out IHealthable hurtable))
        {
            Debug.Log("pego 1,2");

            hurtable.TakeDamage(Damage);
        }
    }

    public void DealDamageCharged()
    {
        if (Target == null) return; 
        Debug.Log("atac charged");
        if (Target.TryGetComponent<IHealthable>(out IHealthable hurtable))
        {
            Debug.Log("pego cargado");

            hurtable.TakeDamage(Damage * 1.5f);
        }
    }
    //private bool canAttack = true;

    //public override void Attack()
    //{
    //    if (!canAttack) return;

    //    canAttack = false;
    //    if (attackCoroutine == null)
    //    {
    //        attackCoroutine = StartCoroutine(AttackRoutine());
    //    }
    //    ChooseState();
    //}
    //public void DealDamageBasic()
    //{
    //    Debug.Log("atac basic");
    //    if (Target.TryGetComponent<IHealthable>(out IHealthable hurtable))
    //    {
    //        hurtable.TakeDamage(Damage);     
    //    }
    //}
    //public void DealDamageCharged()
    //{
    //    Debug.Log("atac charged");

    //    if (Target.TryGetComponent<IHealthable>(out IHealthable hurtable))
    //    {
    //        hurtable.TakeDamage(Damage*1.5f);
    //    }
    //}
    //private IEnumerator AttackRoutine()
    //{
    //    Debug.Log("AttackRoutine");



    //    yield return new WaitForSeconds(AttackCooldown);

    //    int rand = Random.Range(1, 100);
    //    switch (rand)
    //    {
    //        case int when rand > 0 && rand < 60:
    //            //attack 1
    //            animator.SetBool("IsAt2", false);
    //            animator.SetBool("IsAtCh", false);
    //            animator.SetBool("IsAt1", true);
    //            break;
    //        case int when rand >= 60 && rand < 90:
    //            //attack 2
    //            animator.SetBool("IsAtCh", false);
    //            animator.SetBool("IsAt1", false);
    //            animator.SetBool("IsAt2", true);

    //            break;
    //        case int when rand >= 90:
    //            //attack charged
    //            animator.SetBool("IsAt2", false);
    //            animator.SetBool("IsAt1", false);
    //            animator.SetBool("IsAtCh", true);
    //            break;
    //        default:
    //            //attack1
    //            animator.SetBool("IsAt2", false);
    //            animator.SetBool("IsAtCh", false);
    //            animator.SetBool("IsAt1", true);
    //            break;
    //    }
    //    canAttack = true;
    //    attackCoroutine = null;
    //    ChooseState();
    //}





    public override void Die()
    {
        animator.SetTrigger("Dead");
        return;
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
        if (damage >= this.Health)
        {
            animator.SetTrigger("Dead");
        }
        base.TakeDamage(damage);
        animator.SetBool("IsAt2", false);
        animator.SetBool("IsAt1", false);
        animator.SetBool("IsAtCh", false);
        animator.SetBool("IsMoving", false);
        animator.SetTrigger("Hit");
    }

    public override void Chase()
    {
        if (chaseCoroutine != null) return;

        chaseCoroutine = StartCoroutine(ChaseRoutine());
    }

    public override IEnumerator ChaseRoutine()
    {

        agent.isStopped = false;

        while (Target != null)
        {
            float distance = Vector3.Distance(Target.transform.position, transform.position);

            if (distance <= AttackRange)
            {
                agent.isStopped = true;
                chaseCoroutine = null;
                animator.SetBool("IsMoving", false);
                ChooseState();
                yield break;
            }
            else if (distance > VisionDistance)
            {
                animator.SetBool("IsMoving", false);

                agent.isStopped = true;
                Target = null;
                chaseCoroutine = null;
                ChooseState();

                yield break;
            }
            else
            {
                animator.SetBool("IsMoving", true);

                agent.speed = Speed;
                agent.SetDestination(Target.transform.position);


                yield return new WaitForSeconds(0.2f);
            }
        }
      

    }
}
