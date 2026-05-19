using UnityEngine;

public abstract class Crystal : MonoBehaviour
{
    public abstract float damage { get; set; }
    protected bool thrown = false;
    protected bool onGround = true;
    protected AudioManager audioManager;

    [SerializeField] protected LayerMask layer;

    public void Awake()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
    }

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

        audioManager.Play(AudioClips.GlassBreak);
        Destroy(gameObject);
    }
}