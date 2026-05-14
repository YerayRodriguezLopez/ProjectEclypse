using UnityEngine;
using UnityEngine.Events;

public class LifeCycleTriggerBehaviour : MonoBehaviour
{
    public enum TriggerMode
    {
        OnDisable,
        OnDestroy
    }

    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnDestroy;
    [SerializeField] private UnityEvent actions;

    private void OnDisable()
    {
        if (triggerMode == TriggerMode.OnDisable)
            actions.Invoke();
    }

    private void OnDestroy()
    {
        if (triggerMode == TriggerMode.OnDestroy)
            actions.Invoke();
    }
}