using UnityEngine;

public class ExplosiveCrystal : Crystal
{
    public override float damage { get; set; } = 10;


    private float MaxExplosionDamage = 110;

    private float MinExplosionDamage = 20;

    private float ExplosionRadius = 5;

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("hola");
       
        Hit(other);
       
    }
    override protected void Hit(Collider collision)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, ExplosionRadius, layer);
        audioManager.Play(AudioClips.Explosion);
        foreach (Collider other in colliders)
        {

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
                float distance = Vector3.Distance(transform.position, other.transform.position);
                float damageAmount = Mathf.Lerp(MaxExplosionDamage, MinExplosionDamage, distance / ExplosionRadius);
                Debug.Log(distance);
                //float damageAmount = (ExplosionRadius/distance) * 100;
                Debug.Log(damageAmount);
                healthable.TakeDamage(damageAmount);
                //Hit(other);

            }

           

        }
        Destroy(gameObject);
    }
}
