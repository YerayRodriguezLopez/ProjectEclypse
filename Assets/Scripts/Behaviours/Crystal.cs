using UnityEngine;

public class Crystal : MonoBehaviour
{
    [SerializeField]
    protected float damage;
    protected bool thrown = false;
    protected bool onGround = true;

    private void OnCollisionEnter(Collision collision)
    {
        if (onGround)
        {
            if(collision.gameObject.CompareTag("CompanionAttack") || collision.gameObject.CompareTag("EnemyAttack"))
            {
                Hit(collision);
            }
        }
        if (thrown)
        {
            Hit(collision);
        }
    }

    virtual protected void Hit(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<IHurtable>(out var hurtable))
        {
            hurtable.Hurt(damage);
        }
        Destroy(gameObject);
    }
}
