using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain serializable container for a single save slot.
/// Populated by <see cref="GameManager.SaveGame"/> and consumed by
/// <see cref="GameManager.LoadGame"/>.
///
/// Scene build index mapping:
///   0  →  Main Menu        (never saved to — new game only)
///   1  →  Tutorial scene   (CheckpointId == 0, or no checkpoint yet)
///   2  →  Second scene     (CheckpointId >= 1)
/// </summary>
[Serializable]
public class SaveData
{
    // ── Player ────────────────────────────────────────────────────────────────

    /// <summary>Player's current health at the time of saving.</summary>
    public float PlayerHealth;

    // ── Companions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Health for each companion, ordered by their registration index in
    /// <see cref="GameManager.Companions"/>.
    /// Index 0 → first registered companion, index 1 → second, etc.
    /// </summary>
    public List<float> CompanionHealths = new();

    // ── Checkpoint ────────────────────────────────────────────────────────────

    /// <summary>
    /// ID of the last reached checkpoint as assigned by
    /// <see cref="GameManager.SetCheckpoint"/>.
    /// -1 means no checkpoint has been reached in this run.
    /// </summary>
    public int CheckpointId = -1;

    /// <summary>World-space respawn position of the last checkpoint.</summary>
    public SerializableVector3 CheckpointPosition;

    // ── Derived helpers (not serialized) ─────────────────────────────────────

    /// <summary>
    /// Build index of the scene to load when restoring this save.
    /// Derived from <see cref="CheckpointId"/> at runtime — not written to JSON.
    /// Call <see cref="ResolveScene"/> after deserialization before reading this.
    /// </summary>
    [NonSerialized]
    public int TargetSceneIndex;

    /// <summary>
    /// Resolves and caches <see cref="TargetSceneIndex"/> from the checkpoint ID.
    ///
    ///   CheckpointId == -1  →  1  (no checkpoint yet, start of tutorial)
    ///   CheckpointId ==  0  →  1  (first checkpoint, still in tutorial)
    ///   CheckpointId >=  1  →  2  (second checkpoint or later, main scene)
    /// </summary>
    public void ResolveScene()
    {
        TargetSceneIndex = CheckpointId switch
        {
            -1 => 1,    // No checkpoint yet — load tutorial from the start.
            0  => 1,    // First checkpoint is still inside the tutorial.
            _  => 2     // Second checkpoint or later → main scene.
        };
    }
}

/// <summary>
/// JSON-friendly substitute for <see cref="Vector3"/>, which Unity's
/// built-in JsonUtility cannot serialize as a plain float triplet inside
/// a custom class when nested.
/// </summary>
[Serializable]
public struct SerializableVector3
{
    public float X;
    public float Y;
    public float Z;

    public SerializableVector3(Vector3 v) { X = v.x; Y = v.y; Z = v.z; }

    public Vector3 ToVector3() => new(X, Y, Z);

    public static implicit operator SerializableVector3(Vector3 v) => new(v);
    public static implicit operator Vector3(SerializableVector3 s)  => s.ToVector3();
}
