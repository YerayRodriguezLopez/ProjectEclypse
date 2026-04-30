using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DissolveVoronoiController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Range(0f, 1f)] public float dissolveSpeed = 0.08f;   // Much slower default
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

        // Voronoi fully covers the mesh around 0.75, not 1.0
        dissolveAmount = Mathf.MoveTowards(dissolveAmount, 0.75f, dissolveSpeed * Time.deltaTime);
        _material.SetFloat(DissolveAmountID, dissolveAmount);

        if (dissolveAmount >= 0.75f)
        {
            _isDissolving = false;
            gameObject.SetActive(false);
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