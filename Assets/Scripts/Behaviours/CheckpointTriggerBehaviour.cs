using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTriggerBehaviour : MonoBehaviour
{
    [SerializeField] private int checkpointNumber = 0;
    private bool _triggered = false;
    
    private Collider _collider;

    void Start()
    {
        _collider = GetComponent<BoxCollider>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_triggered)
        {
            Debug.Log(this.name + " is triggering " + checkpointNumber);
            if (other.gameObject.layer == 6)
            {
                _triggered = true;
                //GameManager.Instance.CheckpointReached(checkpointNumber, this.transform.position);
                _collider.enabled = false;
            }
        }
    }
}