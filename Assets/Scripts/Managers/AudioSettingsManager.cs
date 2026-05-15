using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private Slider sliderSFX;
    
    /* Required scene layout:
     * UI Canvas
     * |- Settings container
     *   |- Sliders
     */

    private float _volumeVal;

    private void Start()
    {
        // Debug.Log("Start AudioSettingsManager LoadVolumeLevel");
        LoadVolumeLevel(1);
        LoadVolumeLevel(2);
        LoadVolumeLevel(3);
        // Debug.Log("Ending AudioSettingsManager LoadVolumeLevel");
    }

    public void LoadVolumeLevel(int sliderNum)
    {
        switch (sliderNum)
        {
            case 1:
                ProcessVolumeLoad(sliderMaster.transform.name, sliderMaster);
                break;
                
            case 2:
                ProcessVolumeLoad(sliderMusic.transform.name, sliderMusic);
                break;

            case 3:
                ProcessVolumeLoad(sliderSFX.transform.name, sliderSFX);
                break;

            default:
                break;
        }
    }

    private void ProcessVolumeLoad(string sliderName, Slider slider)
    {
        // Debug.Log(sliderName + "'s PlayerPrefs loading returns: "+ PlayerPrefs.GetFloat(sliderName, 0.5f));

        _volumeVal = PlayerPrefs.GetFloat(sliderName, 0.5f);
        slider.value = _volumeVal;
        mixer.SetFloat(sliderName, Mathf.Log10(_volumeVal) * 20);
    }

    public void SaveVolumeLevel(int sliderNum, float sliderVal)
    {
        switch (sliderNum)
        {
            case 1:
                // Debug.Log("Saving MasterVol");
                SaveSlider(sliderVal, sliderMaster.transform.name);
                break;
                
            case 2:
                // Debug.Log("Saving MusicVol");
                SaveSlider(sliderVal, sliderMusic.transform.name);
                break;

            case 3:
                // Debug.Log("Saving SFXVol");
                SaveSlider(sliderVal, sliderSFX.transform.name);
                break;

            default:
                break;
        }
        // Debug.Log("AudioSettingsManager.cs, searching for slider num " + sliderNum);
    }

    private void SaveSlider(float sliderVal, string sliderName)
    {
        // Debug.Log(sliderName + ": Saving value " + sliderVal + " to PlayerPrefs");
        _volumeVal = sliderVal;
        mixer.SetFloat(sliderName, Mathf.Log10(_volumeVal) * 20); 
        PlayerPrefs.SetFloat(sliderName, _volumeVal);
        // Debug.Log(sliderName + ": Saved" + PlayerPrefs.GetFloat(sliderName, 0.5f));
    }
    
    public void SetMasterVolume(float vol)
    {
        SaveVolumeLevel(1, vol);
    }

    public void SetMusicVolume(float vol)
    {
        SaveVolumeLevel(2, vol);
    }

    public void SetSFXVolume(float vol)
    {
        SaveVolumeLevel(3, vol);
    }
}