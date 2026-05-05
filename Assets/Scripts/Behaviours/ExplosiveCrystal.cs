using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ExplosiveCrystal : Crystal
{
    public override float damage { get; set; } = 10;


    private float MaxExplosionDamage = 100;

    private float MinExplosionDamage = 20;

    private float ExplosionRadius = 5;

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("hola");
        //if ((layer.value & (1 << other.gameObject.layer)) != 0)
        //{
        //    if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTargetParent))
        //    {
        //        if (hurtableTargetParent.CanBeHurt)
        //        {
        //            //Debug.Log("espada pega");
        //            //hurtableTarget.TakeDamage(damage);

        //            Hit(other);



        //        }
        //    }
        //    else if (other.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
        //    {
        //        if (hurtableTarget.CanBeHurt)
        //        {
        //            //Debug.Log("espada pega");
        //            //hurtableTarget.TakeDamage(damage);

        //            Hit(other);



        //        }
        //    }
        //}
        Hit(other);
       
    }
    override protected void Hit(Collider collision)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, ExplosionRadius, layer);
        foreach (Collider collider in colliders)
        {
            if (collider.transform.parent.TryGetComponent<IHealthable>(out var hurtableTargetParent) && thrown)
            {
                Debug.Log("daño explosion");
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                float damageAmount = Mathf.Lerp(MaxExplosionDamage, MinExplosionDamage, distance / ExplosionRadius);
                Debug.Log(distance);
                //float damageAmount = (ExplosionRadius/distance) * 100;
                Debug.Log(damageAmount);
                hurtableTargetParent.TakeDamage(damageAmount);
               
            }
            //else if (collision.TryGetComponent<IHealthable>(out IHealthable hurtableTarget) && thrown)
            //{
            //    Debug.Log("daño explosion otro");
            //    //float distance = Vector3.Distance(transform.position, collider.transform.position);
            //    ////float damageAmount = Mathf.Lerp(MaxExplosionDamage, MinExplosionDamage, distance / ExplosionRadius);
            //    //float damageAmount = (distance / ExplosionRadius) * 100;
            //    //Debug.Log(damageAmount);
            //    //hurtableTarget.TakeDamage(damageAmount);
            //}
           
        }
        Destroy(gameObject);
    }
}
