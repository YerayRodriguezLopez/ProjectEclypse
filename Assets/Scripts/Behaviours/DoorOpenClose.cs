using UnityEngine;

public class DoorOpenClose : MonoBehaviour
{
    [SerializeField]
    Collider doorCollider;
    [SerializeField]
    Transform doorMesh;
    [SerializeField]
    int heightToMove = 2; // The height the door will move when opening
    AudioManager audioManager;

    public void Awake()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
    }

    public void Open() //Open the door moving it up smoothly with an animation and disable the collider without usin Leantoward
    {
        doorMesh = transform.GetChild(0);
        doorCollider = GetComponent<Collider>();
        StartCoroutine(MoveDoorUp());
        audioManager.Play(AudioClips.Door);
    }
    public void Close() //Close the door moving it down smoothly with an animation and enable the collider without usin Leantoward
    {

        StartCoroutine(MoveDoorDown());
        audioManager.Play(AudioClips.Door);
    }

    private System.Collections.IEnumerator MoveDoorUp()
    {
        float elapsedTime = 0f;
        Vector3 startingPos = doorMesh.localPosition;
        Vector3 targetPos = startingPos + new Vector3(0, heightToMove, 0); // Move the door up by 2 units
        while (elapsedTime < 1f) // Move the door over 1 second
        {
            doorMesh.localPosition = Vector3.Lerp(startingPos, targetPos, elapsedTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        doorMesh.localPosition = targetPos; // Ensure the door reaches the target position
        doorCollider.enabled = false; // Disable the collider after opening
    }
    private System.Collections.IEnumerator MoveDoorDown()
    {
        float elapsedTime = 0f;
        Vector3 startingPos = doorMesh.localPosition;
        Vector3 targetPos = startingPos - new Vector3(0, heightToMove, 0); // Move the door down by 2 units
        while (elapsedTime < 1f) // Move the door over 1 second
        {
            doorMesh.localPosition = Vector3.Lerp(startingPos, targetPos, elapsedTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        doorMesh.localPosition = targetPos; // Ensure the door reaches the target position
        doorCollider.enabled = true; // Enable the collider after closing
    }
}
