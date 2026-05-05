using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public abstract class Crystal : MonoBehaviour
{
 
    public abstract float damage { get; set; }
    protected bool thrown = true;
    protected bool onGround = false;
    //protected bool thrown = true;
    //protected bool onGround = false;
    [SerializeField] protected LayerMask layer;

    //protected virtual void OnTriggerEnter(Collider other)
    //{
    //    if ((layer.value & (1 << other.gameObject.layer)) != 0)
    //    {
    //        if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTargetParent))
    //        {
    //            if (hurtableTargetParent.CanBeHurt)
    //            {
    //                //Debug.Log("espada pega");
    //                //hurtableTarget.TakeDamage(damage);
                    
    //                    Hit(other);
                    
                    

    //            }
    //        }
    //        else if (other.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
    //        {
    //            if (hurtableTarget.CanBeHurt)
    //            {
    //                //Debug.Log("espada pega");
    //                //hurtableTarget.TakeDamage(damage);

    //                Hit(other);



    //            }
    //        }
    //    }
    //    //else
    //    //{
    //    //    Destroy(gameObject);
    //    //}
    //}

    virtual protected void Hit(Collider other)
    {
        if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTarget) && thrown)
        {
            hurtableTarget.TakeDamage(damage);
            Debug.Log("damage");

        }
       
    }
}
