using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIBehaviour : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    public void OnPlayButton()
    {
        SceneManager.LoadScene(1);
    }

    public void OnSettingsButton()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnBackButton()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
