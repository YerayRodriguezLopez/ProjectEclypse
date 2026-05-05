/// <summary>
/// Implemented by any object that participates in the save/load cycle.
/// <see cref="GameManager"/> orchestrates the call order:
///   Save → collects data from all saveables → serializes.
///   Load → deserializes → pushes data back to all saveables.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Write this object's persistent state into <paramref name="data"/>.
    /// Called by <see cref="GameManager.SaveGame"/> before serialization.
    /// </summary>
    void OnSave(SaveData data);

    /// <summary>
    /// Read persistent state from <paramref name="data"/> and apply it.
    /// Called by <see cref="GameManager.LoadGame"/> after deserialization.
    /// </summary>
    void OnLoad(SaveData data);
}
