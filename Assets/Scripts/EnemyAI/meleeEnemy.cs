using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class meleeEnemy : SimpleEnemy
{
    
    public override float StunDuration { get; set; } = 0.5f;
    public override bool IsStunned { get; set; } = false;
    public override float VisionDistance { get; set; } = 15;
    public override float Health { get; set; } = 100;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 1.5f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;
    public override float Speed { get; set; } = 3.5f;



    //[SerializeField] private GameObject Target;

    //private NavMeshAgent agent;
    //private Transform TargetPosition;
    //private Coroutine chaseCoroutine;
    private Coroutine attackCoroutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChooseState();
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
                Attack();
            }
            else if (distance <= VisionDistance)
            {
                //Debug.Log("START CHASE");
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
        //Debug.Log(other.transform.gameObject.layer);
        //Debug.Log(" layer " + playerLayer.value);
        if((other.transform.gameObject.layer == 3|| other.transform.gameObject.layer == 6))
        {
            
            if (Target == null)
            {
                Target = other.gameObject;
                ChooseState();
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
        base .ClearStun();
    }

    public override IEnumerator ClearStunRutine()
    {
        yield return base.ClearStunRutine();
    }

    public override void TakeDamage(float damage)
    {
        this.Health -= damage;
        ChooseState();
    }
    
    public override void Chase()
    {
      base.Chase();
    }

    public override IEnumerator ChaseRoutine()
    {
        yield return base.ChaseRoutine();
       
    }
}
