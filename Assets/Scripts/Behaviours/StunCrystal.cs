using UnityEngine;

public class StunCrystal : Crystal
{
    public override float damage { get; set; } = 10;
    protected void OnTriggerEnter(Collider other)
    {
        if ((layer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTargetParent))
            {
                if (hurtableTargetParent.CanBeHurt)
                {
                    //Debug.Log("espada pega");
                    //hurtableTarget.TakeDamage(damage);

                    Hit(other);



                }
            }
            else if (other.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
            {
                if (hurtableTarget.CanBeHurt)
                {
                    //Debug.Log("espada pega");
                    //hurtableTarget.TakeDamage(damage);

                    Hit(other);



                }
            }
        }
        //else
        //{
        //    Destroy(gameObject);
        //}
    }
    protected override void Hit(Collider other)
    {
        if (other.transform.parent.TryGetComponent<IStunnable>(out var stunnable) && thrown)
        {
            stunnable.Stun();
            Debug.Log("stun");
        }
        base.Hit(other);
        
    }
}
