using UnityEngine;

public class Crystal : MonoBehaviour
{
    [SerializeField]
    protected float Damage;
    protected bool ToBeThrown = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (ToBeThrown)
        {
            if (collision.gameObject.TryGetComponent(out IHurtable damageable))
            {
                damageable.TakeDamage(Damage);
            }
            Destroy(gameObject);
        }
    }
}
