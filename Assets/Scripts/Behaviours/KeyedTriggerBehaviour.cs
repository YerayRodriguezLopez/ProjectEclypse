using UnityEngine;
using UnityEngine.Events;

public class KeyedTriggerBehaviour : MonoBehaviour
{
    [SerializeField] private int keysRequired = 1;
    [SerializeField] private UnityEvent triggerAction;
    
    private int keysObtained = 0;

    public void ReceiveKey()
    {
        keysObtained++;
        Debug.Log(this.transform.name + " - Keys required: " + keysRequired + ", KeysObtained: " + keysObtained);
        if (keysObtained >= keysRequired) triggerAction.Invoke();
    }
}
