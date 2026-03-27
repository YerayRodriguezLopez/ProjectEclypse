using System.Collections.Generic;
using UnityEngine;

//
public enum AudioClips
{
    Jump,
    NormalShot,
    TankShot,
    FootSteps,
    Sprint,
    GameOver,
    ThankYoy,
    Laugh,
    NoSound,
    EnemyShot
}
//

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    //
    [SerializeField] private List<AudioClip> _clips = new List<AudioClip>();
    private Dictionary<AudioClips, AudioClip> clipList = new Dictionary<AudioClips, AudioClip>();
    //

    [SerializeField] private AudioSource sfxAudioSourcePrefab; // Prefab for 3D event sounds

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeClipDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeClipDictionary()
    {
        clipList.Clear();
        if (_clips.Count < System.Enum.GetValues(typeof(AudioClips)).Length)
        {
            Debug.LogError("There aren't enough audio clips asigned on AudioManager!");
        }
        else
        {
            int i = 0;
            foreach (AudioClips clipName in System.Enum.GetValues(typeof(AudioClips)))
            {
                clipList[clipName] = _clips[i];
                i++;
            }
        }
    }

    public AudioClip GetClip(AudioClips clipName)
    {
        if (clipList.ContainsKey(clipName))
            return clipList[clipName];

        Debug.LogError("Audio clip not found on AudioManager: " + clipName);
        return _clips.Count > 0 ? _clips[0] : null;
    }

    // Play non-spatialized event SFX globally (e.g., UI clicks)
    public void Play(AudioClips clipName)
    {
        AudioClip clip = GetClip(clipName);

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 1f);
    }

    // Override to play spatialized event SFX at a position in world
    public void Play(AudioClips clipName, Vector3 position)
    {
        AudioClip clip = GetClip(clipName);

        if (clip != null)
        {
            AudioSource newSource = Instantiate(sfxAudioSourcePrefab, position, Quaternion.identity);
            newSource.clip = clip;
            newSource.spatialBlend = 1f;
            newSource.minDistance = 5f; // max volume radius
            newSource.maxDistance = 20f; // distance where volume fades to 0

            newSource.Play();
            Destroy(newSource.gameObject, clip.length);
        }
    }
    
    // Override to play spatialized event SFX at a position in world with a custom radius
    public void Play(AudioClips clipName, Vector3 position, float minDistance, float maxDistance)
    {
        AudioClip clip = GetClip(clipName);

        if (clip != null)
        {
            AudioSource newSource = Instantiate(sfxAudioSourcePrefab, position, Quaternion.identity);
            newSource.clip = clip;
            newSource.spatialBlend = 1f;
            newSource.minDistance = minDistance; // max volume radius
            newSource.maxDistance = maxDistance; // distance where volume fades to 0

            newSource.Play();
            Destroy(newSource.gameObject, clip.length);
        }
    }
}