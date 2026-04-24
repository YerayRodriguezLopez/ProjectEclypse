
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class SimpleEnemy : NPC, IPullable, IStunnable
{

   

    //[SerializeField] private Vector3 _attackLocalDirection;

    public abstract bool IsStunned { get; set; }

    //[SerializeField] private bool _isStunned;

    public abstract float StunDuration { get; set; }
    public abstract float Speed { get; set; }

    //[SerializeField] private float _stunDuration;

    //[SerializeField] private NodeSO Root, CurrentState;

    //public abstract NodeSO _root { get; set; }
    //public abstract NodeSO _currentState { get; set; }

    ////[HideInInspector] public NavMeshAgent agent;
    //public abstract Transform target { get; set; }

    public abstract float VisionDistance { get; set; }
    public Coroutine chaseCoroutine;
    public GameObject Target = null;

    public NavMeshAgent agent;


    //ChooseState sera igual para melee y ranged
    public abstract void ChooseState();

    //pull sera igual para melee y ranged
    public abstract void Pull();

    //deberia ser igual para todos
    public virtual void Stun()
    {
        //stun anim
        IsStunned = true;
        ChooseState();
        ClearStun();

    }


    public virtual void ClearStun()
    {
        StartCoroutine(ClearStunRutine());
    }

    public virtual IEnumerator ClearStunRutine()
    {
        yield return new WaitForSeconds(StunDuration);
        IsStunned = false;
    }
    public virtual void Chase()
    {
        if (chaseCoroutine != null) return;

        chaseCoroutine = StartCoroutine(ChaseRoutine());
    }

    public virtual IEnumerator ChaseRoutine()
    {
        agent.isStopped = false;

        while (Target != null)
        {
            float distance = Vector3.Distance(Target.transform.position, transform.position);

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
                agent.speed = Speed;
                agent.SetDestination(Target.transform.position);
                

                yield return new WaitForSeconds(0.2f);
            }
        }


        agent.isStopped = true;
        this.Target = null;
        chaseCoroutine = null;
    }
}
