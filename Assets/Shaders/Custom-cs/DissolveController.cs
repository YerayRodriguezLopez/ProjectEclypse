using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DissolveController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Range(0f, 5f)] public float dissolveSpeed = 1f;
    [Range(0f, 1f)] public float dissolveAmount = 0f;
    public bool playOnStart = true;

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveSpeedID  = Shader.PropertyToID("_DissolveSpeed");

    private Material _material;
    private bool     _isDissolving;

    private void Awake()
    {
        // Use a per-instance copy so we never mutate the shared asset
        _material = GetComponent<Renderer>().material;
    }

    private void Start()
    {
        if (playOnStart)
            BeginDissolve();
    }

    private void Update()
    {
        if (!_isDissolving) return;

        dissolveAmount = Mathf.MoveTowards(dissolveAmount, 1f, dissolveSpeed * Time.deltaTime);
        _material.SetFloat(DissolveAmountID, dissolveAmount);

        if (Mathf.Approximately(dissolveAmount, 1f))
        {
            _isDissolving = false;
            gameObject.SetActive(false); // Hide once fully dissolved
        }
    }

    /// <summary>Starts the dissolve animation from the current amount.</summary>
    public void BeginDissolve()
    {
        _isDissolving = true;
        _material.SetFloat(DissolveSpeedID, dissolveSpeed);
    }

    /// <summary>Resets the object to fully visible.</summary>
    public void ResetDissolve()
    {
        dissolveAmount = 0f;
        _isDissolving  = false;
        gameObject.SetActive(true);
        _material.SetFloat(DissolveAmountID, dissolveAmount);
    }
}