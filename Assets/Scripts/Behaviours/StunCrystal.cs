using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StunCrystal : Crystal
{
    public override float damage { get; set; } = 10;

    [Header("Configuración de Lanzamiento")]
    [SerializeField] private float forceSpeed = 20f; 

    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rigidbody;

    void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        _grabInteractable.selectExited.AddListener(AlSoltarObjeto);
    }

    void OnDisable()
    {
        _grabInteractable.selectExited.RemoveListener(AlSoltarObjeto);
    }

    private void AlSoltarObjeto(SelectExitEventArgs args)
    {
        Debug.Log("El objeto ha sido soltado");

        var interactorTransform = args.interactorObject.transform;

        Vector3 shootDirection = interactorTransform.forward;

        thrown = true;
        onGround = false;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            _rigidbody.linearVelocity = shootDirection * forceSpeed;
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        bool directLayerMatch = (layer.value & (1 << other.gameObject.layer)) != 0;

        bool parentLayerMatch = other.transform.parent != null &&
                                (layer.value & (1 << other.transform.parent.gameObject.layer)) != 0;

        if (!directLayerMatch && !parentLayerMatch) return;

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