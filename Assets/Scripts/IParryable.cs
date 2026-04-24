using UnityEngine;

public interface IParryable
{
    Vector3 AttackLocalDirection { get; }

    void Parry();

    void OpenParryWindo();
    void CloseParryWindow();
}
