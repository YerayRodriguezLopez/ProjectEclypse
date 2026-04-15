using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class meleeEnemy : SimpleEnemy
{
    public override Vector3 AttackLocalDirection {  get; set; }
    public override float StunDuration { get; set; } = 0.5f;
    public override bool IsStunned { get; set; } = false;
    public override float VisionDistance { get; set; } = 15;
    public override float Health { get; set; } = 100;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 1.5f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;
    public override float Speed { get; set; } = 5f;

    [SerializeField] private LayerMask playerLayer;

    [SerializeField] private LayerMask companionLayer;

    [SerializeField] private GameObject Target;

    private NavMeshAgent agent;
    //private Transform TargetPosition;
    private Coroutine chaseCoroutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChooseState();
    }

    private void Update()
    {
        Debug.Log(Target);
    }
    
    public void ChooseState()
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
                Attack(Target);
            }
            else if (distance <= VisionDistance)
            {
                Debug.Log("START CHASE");
                Chase(Target);
            }
            else
            {
                //Target = null;
                //idle?
            }
            
        }
        //idle?

        
    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log(other.transform.gameObject.layer);
        //Debug.Log(" layer " + playerLayer.value);
        if((other.transform.gameObject.layer == 3|| other.transform.gameObject.layer == 6))
        {
            Debug.Log("agafo target");
            if(Target == null)
            {

                Target = other.transform.gameObject;
            }


            ChooseState();
        }        
    }


    public override void Attack(GameObject Target)
    {
        StartCoroutine(AttackRoutine(Target));
    }

    private IEnumerator AttackRoutine(GameObject target)
    {
        Debug.Log("Entro attack corutine");
        if (target.TryGetComponent<IHurtable>(out IHurtable hurtableTarget))
        //if(target is IHurtable hurtableTarget)
        //var hurtableTarget = target as IHurtable    
        {
            Debug.Log("encuentro hurtable");

            if (hurtableTarget != null)
            {
                

                //attack anim
                Debug.Log("te pego");

                hurtableTarget.TakeDamage(this.Damage);
                
            }
        }
        yield return new WaitForSeconds(AttackCooldown);

        ChooseState();
    }

    

    public override void CloseParryWindow()
    {
    }

    public override void Die()
    {

    }

    public override void OpenParryWindo()
    {
    }

    public override void Parry()
    {
    }

    public override void Pull()
    {
    }

    public override void Stun()
    {
        //stun anim
        IsStunned = true;
        ChooseState();
        ClearStun();

    }
    
    
    public override void ClearStun()
    {
        StartCoroutine(ClearStunRutine());
    }

    private IEnumerator ClearStunRutine()
    {
        yield return new WaitForSeconds(StunDuration);
        IsStunned = false;
    }

    public override void TakeDamage(float damage)
    {
        this.Health -= damage;
        ChooseState();
    }
    //public override void Chase(GameObject Target)
    //{

    //    float distance = Vector3.Distance(Target.transform.position, this.transform.position);
    //    if (distance > AttackRange)
    //    {
    //        // AND line of sight?
    //        agent.SetDestination(Target.transform.position);
    //    }
    //    else
    //    {
    //        ChooseState();
    //    }
    //}
    //public override void Chase(GameObject Target)
    //{
    //    if (chaseCoroutine == null)
    //    {
    //        Debug.Log("ENTER CORUTINE");

    //        chaseCoroutine = StartCoroutine(ChaseRoutine(Target));
    //    }
    //}
    //private IEnumerator ChaseRoutine(GameObject target)
    //{
    //    while (target != null)
    //    {

    //        Debug.Log("chasing");
    //        float distance = Vector3.Distance(target.transform.position, transform.position);
    //        Debug.Log(distance + "----------------->"+ (distance <= AttackRange || distance > VisionDistance));
    //        if (distance <= AttackRange || distance > VisionDistance)
    //        {
    //            Debug.Log("stop chase"); 
    //            chaseCoroutine = null;
    //            Target = null;
    //            agent.isStopped = true;
    //            ChooseState();
    //            yield break;
    //        }
    //        else
    //        {
    //            if (target != null)
    //                agent.SetDestination(target.transform.position);
    //            else
    //                agent.isStopped = true;

    //            chaseCoroutine = null;
    //            Target = null;
    //            ChooseState();
    //            StopCoroutine(chaseCoroutine);
    //            yield return new WaitForSeconds(0.2f);

    //        }


    //    }


    //}
    public override void Chase(GameObject Target)
    {
        if (chaseCoroutine != null)
            StopCoroutine(chaseCoroutine);

        chaseCoroutine = StartCoroutine(ChaseRoutine(Target));
    }

    private IEnumerator ChaseRoutine(GameObject target)
    {
        agent.isStopped = false;

        while (target != null)
        {
            float distance = Vector3.Distance(target.transform.position, transform.position);

            if (distance <= AttackRange)
            {
                agent.isStopped = true;
                chaseCoroutine = null;
                ChooseState(); 
                yield break;
            }
            else if (distance > VisionDistance)
            {
                agent.isStopped = true;
                Target = null;
                chaseCoroutine = null;
                ChooseState();

                yield break;
            }
            else
            {
                agent.SetDestination(target.transform.position);

                yield return new WaitForSeconds(0.2f);
            }
        }

       
        agent.isStopped = true;
        this.Target = null;
        chaseCoroutine = null;
    }
}
