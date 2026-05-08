using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DissolveVoronoiController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Range(0f, 1f)] public float dissolveSpeed  = 0.08f;
    [Range(0f, 1f)] public float dissolveAmount = 0f;
    [SerializeField] private bool playOnStart = true;

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private Material[] _materials;
    private bool        _isDissolving;

    /// <summary>Read-only. Used by DissolveParticleEmitter to track state changes.</summary>
    public bool IsDissolving => _isDissolving;

    private void Awake()
    {
        // .materials returns per-instance copies of all slots — works for 1 or many
        _materials = GetComponent<Renderer>().materials;
    }

    private void Start()
    {
        if (playOnStart)
            BeginDissolve();
    }

    private void Update()
    {
        if (_isDissolving)
        {
            // Voronoi fully covers the mesh around 0.75, not 1.0
            dissolveAmount = Mathf.MoveTowards(dissolveAmount, 0.75f, dissolveSpeed * Time.deltaTime);
            SetDissolveOnAllMaterials(dissolveAmount);

            if (dissolveAmount >= 0.75f)
            {
                _isDissolving = false;
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Starts the dissolve animation from the current amount.</summary>
    public void BeginDissolve()
    {
        _isDissolving = true;
    }

    /// <summary>Resets the object to fully visible.</summary>
    public void ResetDissolve()
    {
        dissolveAmount = 0f;
        _isDissolving  = false;
        gameObject.SetActive(true);
        SetDissolveOnAllMaterials(dissolveAmount);
    }

    private void SetDissolveOnAllMaterials(float value)
    {
        foreach (Material mat in _materials)
            mat.SetFloat(DissolveAmountID, value);
    }
}
