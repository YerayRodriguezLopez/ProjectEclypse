using UnityEngine;

public class PlayerSword : MonoBehaviour
{

    private Player player;
    [SerializeField] private LayerMask layer;


    private void Start()
    {
        player = transform.parent.GetComponent<Player>();
        //Debug.Log(player.Damage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((layer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (other.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
            {
               if(hurtableTarget.CanBeHurt)
                    hurtableTarget.TakeDamage(player.Damage);
            }
        }
    }
}
