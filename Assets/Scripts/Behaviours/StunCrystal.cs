using UnityEngine;

public class StunCrystal : Crystal
{
    [SerializeField]
    private float StunTime;

    protected override void Hit(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<IStunnable>(out var stunnable))
        {
            stunnable.Stun(StunTime);
        }
        Destroy(gameObject);
    }
}
