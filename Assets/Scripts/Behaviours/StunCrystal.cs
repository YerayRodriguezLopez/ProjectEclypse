using UnityEngine;

public class StunCrystal : Crystal
{
    public override float damage { get; set; } = 10;

    protected void OnTriggerEnter(Collider other)
    {
        // object is in layer
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
            Hit(other);
    }

    protected override void Hit(Collider other)
    {
        if (thrown)
        {
         
            IStunnable stunnable = null;

            if (other.transform.parent != null)
                other.transform.parent.TryGetComponent(out stunnable);

            if (stunnable == null)
                other.TryGetComponent(out stunnable);

            if (stunnable != null)
            {
                stunnable.Stun();
                Debug.Log("stun");
            }
        }

        base.Hit(other);
    }
}

