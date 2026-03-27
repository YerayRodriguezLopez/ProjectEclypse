using UnityEngine;
using UnityEngine.Events;

public class TriggerBehaviour : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private LayerMask layer;
    [SerializeField] private bool selfDisable = true;
    
    [Header("OnTriggerEnter")]
    [SerializeField] private UnityEvent triggerAction;
    
    [Header("OnTriggerExit")]
    [SerializeField] private UnityEvent triggerExitAction;
    
    private void OnTriggerEnter(Collider other)
    {
        /* Uses a bitshift AND operation (&).
         * If player is Layer 6, 1 << other.gameObject.layer gets the bit 00000001 and moves it to 01000000 (6 spaces)
         * Then it does a && comparison on the bit from LayerMask and the one from the bitshift. */
        if ((layer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (triggerAction != null)
            {
                triggerAction.Invoke();
                
                if(selfDisable) gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((layer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (triggerExitAction != null)
            {
                triggerExitAction.Invoke();
                
                if(selfDisable) gameObject.SetActive(false);
            }
        }
    }
}