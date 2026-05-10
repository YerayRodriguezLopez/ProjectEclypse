using UnityEngine;
using UnityEngine.Events;

public class GateLockBehaviour : MonoBehaviour
{
    [SerializeField] private int keysRequired = 1;
    [SerializeField] private UnityEvent triggerAction;
    
    private int keysObtained = 0;

    public void ReceiveKey()
    {
        Debug.Log(this.transform.name + " - Keys required: " + keysRequired + ", KeysObtained: " + keysObtained);
        if (keysObtained < keysRequired) keysObtained++;
        else triggerAction.Invoke();
    }
}
