using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the player prefab.
/// On Awake it registers the player and its ordered follow anchors with
/// <see cref="GameManager"/> so each companion receives its own dedicated slot.
///
/// Setup:
///   Add one child Transform per companion slot under the player (e.g.
///   CompanionAnchor0, CompanionAnchor1, CompanionAnchor2) and assign them
///   to _followAnchors in order. Index here must match each companion's
///   serialized SlotIndex.
/// </summary>
public class PlayerRegistrar : MonoBehaviour
{
    [Tooltip("One anchor per companion slot, ordered by SlotIndex. " +
             "Index 0 → companion with SlotIndex 0, index 1 → SlotIndex 1, etc.")]
    [SerializeField] private List<Transform> _followAnchors = new();

    private void Awake()
    {
        if (!GameManager.Instance)
        {
            Debug.LogWarning("[PlayerRegistrar] GameManager not found.");
            return;
        }

        GameManager.Instance.RegisterPlayer(gameObject, _followAnchors);
    }
}
