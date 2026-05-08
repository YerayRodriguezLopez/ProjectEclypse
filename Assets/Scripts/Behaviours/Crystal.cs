using UnityEngine;

public abstract class Crystal : MonoBehaviour
{
    public abstract float damage { get; set; }
    protected bool thrown = true;
    protected bool onGround = false;

    [SerializeField] protected LayerMask layer;

    virtual protected void Hit(Collider other)
    {
        if (!thrown) return;

     
        IHealthable healthable = null;

        if (other.transform.parent != null)
            other.transform.parent.TryGetComponent(out healthable);

        if (healthable == null)
            other.TryGetComponent(out healthable);

        if (healthable != null)
        {
            healthable.TakeDamage(damage);
            Debug.Log("damage");
        }
    }
}