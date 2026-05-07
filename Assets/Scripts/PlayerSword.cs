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
        //if ((layer.value & (1 << other.gameObject.layer)) != 0)
        //{
        //    if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
        //    {
        //        if (hurtableTarget.CanBeHurt)
        //        {
        //            Debug.Log("espada pega");
        //            hurtableTarget.TakeDamage(player.Damage);
        //        }
        //    }
        //    else if (other.transform.parent.TryGetComponent<IHealthable>(out IHealthable hurtableTarget))
        //    {
        //        if (hurtableTarget.CanBeHurt)
        //        {
        //            Debug.Log("espada pega");
        //            hurtableTarget.TakeDamage(player.Damage);
        //        }
        //    }

        //}
        //object is in layer
        bool directLayerMatch = (layer.value & (1 << other.gameObject.layer)) != 0;

        // object parent is in layer
        bool parentLayerMatch = other.transform.parent != null &&
                                (layer.value & (1 << other.transform.parent.gameObject.layer)) != 0;

        if (!directLayerMatch && !parentLayerMatch) return;

        // IHealthable
        IHealthable healthable = null;

        if (other.transform.parent != null)
            other.transform.parent.TryGetComponent(out healthable);

        if (healthable == null)
            other.TryGetComponent(out healthable);

        if (healthable != null && healthable.CanBeHurt)
        {
            Debug.Log("espada pega");
            healthable.TakeDamage(player.Damage);

        }
    }
}
