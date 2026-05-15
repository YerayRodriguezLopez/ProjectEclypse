using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StunCrystal : Crystal
{
    public override float damage { get; set; } = 10;
    private XRGrabInteractable _grabInteractable;
    void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
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
        Debug.Log("El objeto ha sido soltado por: " + args.interactorObject.transform.name);
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

