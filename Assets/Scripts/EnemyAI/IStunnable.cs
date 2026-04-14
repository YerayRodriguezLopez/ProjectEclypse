using UnityEngine;

public interface IStunnable
{
    bool IsStunned { get; }
    float StunDuration { get; }

    void Stun();
    void ClearStun();
}
