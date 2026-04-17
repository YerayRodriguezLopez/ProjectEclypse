using UnityEngine;

public class StunCrystal : Crystal
{
    protected override void Hit(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<IStunnable>(out var stunnable))
        {
            stunnable.Stun();
        }
        Destroy(gameObject);
    }
}
