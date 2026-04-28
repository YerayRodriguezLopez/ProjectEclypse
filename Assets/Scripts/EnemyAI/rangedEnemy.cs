using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class rangedEnemy : SimpleEnemy
{
    private string idleName = "FlyingEnemyIdle";
    private string MoveForwardName = "FlyingEnemyMoveForward";
    //private string attackChargeName = "";
    private string AttackName = "FlyingEnemyAttack";
    public override float StunDuration { get; set; } = 0.5f;
    public override bool IsStunned { get; set; } = false;
    public override float VisionDistance { get; set; } = 17;
    public override float Health { get; set; } = 100;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 1.5f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 10f;
    public override float Speed { get; set; } = 3f;
    public override float MaxHealth { get; set; } = 100;
    public GameObject tt;
    public Animator animator;

    private Coroutine attackCoroutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ChooseState();
    }
    public void Awake()
    {
        //animator.Play
    }
    private void Update()
    {
        //Debug.Log(Target);
    }

    public override void ChooseState()
    {
        
        if (Health <= 0) Die();
        else if (IsStunned) return;
        else if (Target != null && !IsStunned)
        {
            float distance = Vector3.Distance(Target.transform.position, this.transform.position);
            //Debug.Log(distance);
            //Debug.Log(VisionDistance);
            if (distance <= AttackRange)
            {
                animator.Play(AttackName);
                Attack();
            }
            else if (distance <= VisionDistance)
            {
                //chase
                animator.Play(MoveForwardName);
                Chase();
            }
            else
            {
                Target = null;
                animator.Play(idleName);
            }

        }
        animator.Play(idleName);
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
                        if (newHurtable.Health < currentHurtable.Health)
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

        //if (Target.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
        //{
        //    hurtableTarget.TakeDamage(this.Damage);
        //}

        yield return new WaitForSeconds(AttackCooldown);

        canAttack = true;
        ChooseState();
    }



  
    public override void Die()
    {
        Debug.Log("muero");
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
        //this.Health -= damage;
        //ChooseState();

        base.TakeDamage(damage);
    }

    public override void Chase()
    {
        if (chaseCoroutine != null) return;

        chaseCoroutine = StartCoroutine(ChaseRoutine());
    }

    public override IEnumerator ChaseRoutine()
    {
        //    agent.isStopped = false;

        //    while (Target != null)
        //    {
        //        float distance = Vector3.Distance(Target.transform.position, transform.position);

        //        if (distance <= AttackRange)
        //        {
        //            agent.isStopped = true;
        //            chaseCoroutine = null;
        //            ChooseState();
        //            yield break;
        //        }
        //        else if (distance > VisionDistance)
        //        {
        //            agent.isStopped = true;
        //            Target = null;
        //            chaseCoroutine = null;
        //            ChooseState();

        //            yield break;
        //        }
        //        else
        //        {
        //            agent.SetDestination(Target.transform.position);

        //            yield return new WaitForSeconds(0.2f);
        //        }
        //    }


        //    agent.isStopped = true;
        //    this.Target = null;
        //    chaseCoroutine = null;
        yield return base.ChaseRoutine();

    }
}
