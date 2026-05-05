using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Crystal : MonoBehaviour
{
    [SerializeField]
    protected float damage;
    protected bool thrown = false;
    protected bool onGround = true;
    //protected bool thrown = true;
    //protected bool onGround = false;
    [SerializeField] private LayerMask layer;

    private void OnTriggerEnter(Collider other)
    {
        if ((layer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
            {
                if (hurtableTarget.CanBeHurt)
                {
                    //Debug.Log("espada pega");
                    //hurtableTarget.TakeDamage(damage);
                    Hit(other);
                    

                }
            }
        }
    }

    virtual protected void Hit(Collider other)
    {
        if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
        {
            hurtableTarget.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
