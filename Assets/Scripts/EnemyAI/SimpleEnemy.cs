
using UnityEngine;
using UnityEngine.AI;

public abstract class SimpleEnemy : NPC, IParryable, IPullable, IStunnable
{

    public abstract Vector3 AttackLocalDirection { get; set; }

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

    //[SerializeField] private float _visionDistance;




    public abstract void Parry();

    public abstract void OpenParryWindo();

    public abstract void CloseParryWindow();

    public abstract void Pull();

    public abstract void Stun();

    public abstract void ClearStun();
    public abstract void Chase();
}
