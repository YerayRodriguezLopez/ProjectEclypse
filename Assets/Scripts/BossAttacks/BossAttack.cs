using UnityEngine;

public abstract class BossAttack : ScriptableObject
{
    public string attackName;
    public float damage = 10f;

    [Range(0, 100)]
    public int weight = 50;

    public abstract void Execute(Boss boss, Transform target);
}