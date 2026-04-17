using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a world-space checkpoint. When the player (layer 6) enters the trigger,
/// registers this checkpoint with the GameManager as the current respawn point.
///
/// Design notes:
///   • checkpointNumber is still manually set — this is intentional on non-linear
///     maps where spatial sorting would produce incorrect ordering.
///   • Duplicate ID detection runs in OnEnable so errors surface at edit-time
///     via PlayMode, before a bug can silently overwrite checkpoint data.
///   • All checkpoints self-register into a static dictionary so the GameManager
///     (and any debug tooling) can always query which IDs are live in the scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTriggerBehaviour : MonoBehaviour
{
    /// <summary>
    /// Scene-wide registry of active checkpoints keyed by their ID.
    /// Cleared automatically when the scene unloads.
    /// </summary>
    private static readonly Dictionary<int, CheckpointTriggerBehaviour> _registry
        = new Dictionary<int, CheckpointTriggerBehaviour>();

    /// <summary>Returns the registered checkpoint for a given ID, or null.</summary>
    public static CheckpointTriggerBehaviour GetById(int id)
        => _registry.TryGetValue(id, out var cp) ? cp : null;

    [Header("Checkpoint Settings")]
    [Tooltip("Unique ID for this checkpoint within the scene. " +
             "Manually assigned to support non-linear level layouts. " +
             "Duplicate IDs will log an error at runtime.")]
    [SerializeField] private int _checkpointNumber = 0;

    [Tooltip("Exact world-space position the player will respawn at. " +
             "Defaults to this transform's position if left at zero.")]
    [SerializeField] private Vector3 _respawnOffset = Vector3.zero;

    private bool _hasBeenTriggered;
    private Collider _collider;

    public int CheckpointNumber => _checkpointNumber;

    /// <summary>World-space respawn position (transform + optional offset).</summary>
    public Vector3 RespawnPosition => transform.position + _respawnOffset;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    private void OnEnable()
    {
        RegisterSelf();
    }

    private void OnDisable()
    {
        UnregisterSelf();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenTriggered || other.gameObject.layer == 6)
        {
            _hasBeenTriggered = true;
            _collider.enabled = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(_checkpointNumber, RespawnPosition);
            }
            else
            {
                Debug.LogWarning($"[CheckpointTrigger] GameManager not found. " +
                                 $"Checkpoint {_checkpointNumber} could not be registered.");
            }

            Debug.Log($"[CheckpointTrigger] '{name}' triggered checkpoint #{_checkpointNumber} " +
                      $"at respawn position {RespawnPosition}");
        }
    }

    private void RegisterSelf()
    {
        if (_registry.TryGetValue(_checkpointNumber, out var existing) && existing != this)
        {
            Debug.LogError($"[CheckpointTrigger] Duplicate checkpoint ID {_checkpointNumber} " +
                           $"detected on '{name}'. Already registered by '{existing.name}'. " +
                           $"Each checkpoint must have a unique ID.");
        }
        else _registry[_checkpointNumber] = this;
    }

    private void UnregisterSelf()
    {
        if (_registry.TryGetValue(_checkpointNumber, out var registered) && registered == this)
            _registry.Remove(_checkpointNumber);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw the respawn target position so designers can verify placement
        Vector3 respawn = transform.position + _respawnOffset;
        Gizmos.color = _hasBeenTriggered ? Color.gray : Color.green;
        Gizmos.DrawWireSphere(respawn, 0.2f);
        Gizmos.DrawLine(transform.position, respawn);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"CP #{_checkpointNumber}",
            new GUIStyle { normal = { textColor = Color.white }, fontSize = 11 }
        );
    }
#endif
}