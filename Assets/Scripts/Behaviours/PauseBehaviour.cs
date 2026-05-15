using UnityEngine;
using UnityEngine.InputSystem;

public class PauseBehaviour : MonoBehaviour
{
    [SerializeField] private Key pauseKey = Key.Escape;

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            InputSystem.Update();
        }

        if (Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            Debug.Log("Escape pressed!");
            //GameManager.Instance?.TogglePause();
        }
    }
}
