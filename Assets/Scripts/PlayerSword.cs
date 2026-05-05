using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSword : MonoBehaviour
{

    private Player player;
    [SerializeField] private LayerMask layer;


    private void Start()
    {
        player = transform.parent.GetComponent<Player>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((layer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
            {
                if (hurtableTarget.CanBeHurt)
                {
                    Debug.Log("espada pega");
                    hurtableTarget.TakeDamage(player.Damage);
                }
            }
        }
    }
}
