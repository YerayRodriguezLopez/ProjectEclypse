using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager. Singleton, persists across scenes via DontDestroyOnLoad.
/// Owns the authoritative GameState and drives all side-effects (time scale, XR input,
/// timer) automatically whenever state changes.
///
/// Responsibilities (by region):
///   • Lifecycle    – singleton setup, initialization
///   • State        – GameState enum + transition logic
///   • Timer        – runtime elapsed-time tracking + final time snapshot for UI
///   • Checkpoints  – respawn data persistence
///   • Companions   – player anchor registration + companion registry
///   • Cinematics   – triggering sequences by ID (provisional)
///   • Scene        – scene loading
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded_Companions;
            InitializeGame();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_Companions;
    }

    #endregion

    #region Enums

    public enum GameState
    {
        MainMenu,
        Playing,
        Pause,
        Combat,
        BossCombat,
        PlayerDeath,
        Respawning,
        Win,
        Lose
    }

    #endregion

    #region Events

    /// <summary>
    /// --- State ---
    /// Fired on every state change.
    /// Passes (previousState, newState).
    /// Subscribe to drive UI panels, music layers, enemy AI, etc.
    /// </summary>
    public event Action<GameState, GameState> OnGameStateChanged;

    /// <summary>
    /// --- State ---
    /// Fired when pause state changes.
    /// Passes true when pausing, false when resuming.
    /// Subscribe to show/hide the pause menu.
    /// </summary>
    public event Action<bool> OnPauseChanged;

    /// <summary>
    /// --- Input ---
    /// Fired whenever XR input is enabled or disabled.
    /// Passes true when input is enabled, false when disabled.
    /// Subscribe to update any input-dependent UI indicators.
    /// </summary>
    public event Action<bool> OnInputChanged;

    /// <summary>
    /// --- Player ---
    /// Fired the moment the player dies, before the respawn delay begins.
    /// Subscribe to trigger death animations, ragdoll, death-screen fade-in, etc.
    /// </summary>
    public event Action OnPlayerDied;

    /// <summary>
    /// --- Player ---
    /// Fired during the respawn sequence, after the delay but before input is
    /// re-enabled. Passes the world-space position the player should teleport to.
    /// Subscribe in PlayerController to handle the actual teleport — the
    /// GameManager re-enables input only after this event returns.
    /// </summary>
    public event Action<Vector3> OnPlayerRespawned;

    /// <summary>
    /// --- Checkpoints ---
    /// Fired when a new checkpoint is registered.
    /// Passes (checkpointId, respawnPosition).
    /// Subscribe to show checkpoint UI feedback (popup, icon, etc.).
    /// </summary>
    public event Action<int, Vector3> OnCheckpointReached;

    /// <summary>
    /// --- Timer ---
    /// Fired when the timer is stopped and reset.
    /// Carries the FinalRunTime snapshot so UI screens can display the
    /// completed run time even after RunTime has been cleared.
    /// </summary>
    public event Action<float> OnTimerReset;

    #endregion

    #region SerializedFields

    [Header("XR Input")]
    [Tooltip("Assign the XRI Default Input Actions asset here.")]
    [SerializeField] private InputActionAsset _xrInputActions;

    [Header("Respawn")]
    [SerializeField] private float _respawnDelay = 3f;

    #endregion

    #region ReadOnlyProperties

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public bool      IsGamePaused { get; private set; }

    /// <summary>Live elapsed play-time in seconds. Updated each frame while running.</summary>
    public float RunTime { get; private set; }

    /// <summary>
    /// Snapshot of RunTime captured the moment it is reset.
    /// Read this from Win / Lose / Score screens — it is stable after
    /// StopAndResetTimer() and will not be zero like RunTime will be.
    /// </summary>
    public float FinalRunTime { get; private set; }

    public int     LastCheckpointId       { get; private set; } = -1;
    public Vector3 LastCheckpointPosition { get; private set; }

    #endregion

    #region PrivateFields

    private bool _isTimerRunning;
    private bool _isInputEnabled;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Resets all game data to a clean starting state.
    /// Called automatically on first Awake; safe to call again for a full reset.
    /// </summary>
    public void InitializeGame()
    {
        LastCheckpointId       = -1;
        LastCheckpointPosition = Vector3.zero;
        IsGamePaused           = false;
        _isInputEnabled        = false;

        ResetTimer();
        // disabled for debug
        //SetState(GameState.MainMenu);
    }

    public void Start()
    {
        SetState(GameState.Playing);
    }

    #endregion

    #region StateMachine

    /// <summary>
    /// Transitions to a new GameState and automatically applies all side-effects.
    /// Has no effect if the requested state is already active.
    /// </summary>
    public void SetState(GameState newState)
    {
        if (newState != CurrentState)
        {
            GameState previousState = CurrentState;
            CurrentState = newState;

            ApplyStateEffects(newState);
            OnGameStateChanged?.Invoke(previousState, newState);

            Debug.Log($"[GameManager] State: {previousState} → {newState}");
        }
    }

    private void ApplyStateEffects(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                SetTimeScale(1f);
                SetInputEnabled(false);
                StopAndResetTimer();
                IsGamePaused = false;
                break;

            case GameState.Playing:
                SetTimeScale(1f);
                SetInputEnabled(true);
                StartTimer();
                IsGamePaused = false;
                break;

            case GameState.Pause:
                SetTimeScale(0f);
                SetInputEnabled(false);
                PauseTimer();
                IsGamePaused = true;
                break;

            case GameState.Combat:
            case GameState.BossCombat:
                SetTimeScale(1f);
                SetInputEnabled(true);
                ResumeTimer();
                IsGamePaused = false;
                break;

            case GameState.PlayerDeath:
                SetTimeScale(1f);
                SetInputEnabled(false);
                PauseTimer();
                break;

            case GameState.Respawning:
                SetTimeScale(1f);
                SetInputEnabled(false);
                break;

            case GameState.Win:
            case GameState.Lose:
                SetTimeScale(0f);
                SetInputEnabled(false);
                StopAndResetTimer();
                break;
        }
    }

    #endregion

    #region Pause

    /// <summary>Toggles between Pause and Playing states.</summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Pause)
        {
            SetState(GameState.Playing);
            OnPauseChanged?.Invoke(false);
        }
        else
        {
            SetState(GameState.Pause);
            OnPauseChanged?.Invoke(true);
        }
    }

    #endregion

    #region XRInput

    /// <summary>
    /// Flips the current XR input enabled state.
    /// Called automatically by the state machine; also available for manual calls
    /// during cutscenes or UI moments outside a state transition.
    /// </summary>
    public void ToggleInput()
    {
        SetInputEnabled(!_isInputEnabled);
    }

    private void SetInputEnabled(bool enabled)
    {
        _isInputEnabled = enabled;

        if (_xrInputActions)
        {
            foreach (InputActionMap actionMap in _xrInputActions.actionMaps)
            {
                if (enabled)
                {
                    actionMap.Enable();
                }
                else
                {
                    actionMap.Disable();
                }
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] No InputActionAsset assigned. " +
                             "Assign the XRI Default Input Actions asset in the Inspector.");
        }

        OnInputChanged?.Invoke(enabled);
    }

    #endregion

    #region Timer

    private void Update()
    {
        if (_isTimerRunning)
        {
            RunTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Starts the timer from its current value (does not reset it).
    /// Called automatically when transitioning to Playing state.
    /// Expose publicly so other systems can trigger it manually if needed.
    /// </summary>
    public void StartTimer()
    {
        _isTimerRunning = true;
    }

    /// <summary>Pauses accumulation without changing the current RunTime value.</summary>
    public void PauseTimer()
    {
        _isTimerRunning = false;
    }

    /// <summary>Resumes accumulation from wherever RunTime was paused.</summary>
    public void ResumeTimer()
    {
        _isTimerRunning = true;
    }

    /// <summary>
    /// Stops the timer, snapshots RunTime into FinalRunTime, then clears RunTime to zero.
    /// Fires OnTimerReset with the captured value so UI screens can display
    /// the completed time even after RunTime itself is zero.
    /// Called automatically on Win, Lose, and MainMenu state transitions.
    /// </summary>
    private void StopAndResetTimer()
    {
        _isTimerRunning = false;
        FinalRunTime    = RunTime;
        RunTime         = 0f;

        OnTimerReset?.Invoke(FinalRunTime);
    }

    /// <summary>
    /// Wipes both RunTime and FinalRunTime to zero without snapshotting.
    /// Reserved for InitializeGame() on a completely fresh session start.
    /// </summary>
    private void ResetTimer()
    {
        _isTimerRunning = false;
        RunTime         = 0f;
        FinalRunTime    = 0f;
    }

    private void SetTimeScale(float scale)
    {
        Time.timeScale      = scale;
        Time.fixedDeltaTime = 0.02f * scale;
    }

    #endregion

    #region Checkpoints

    /// <summary>
    /// Records the last reached checkpoint. Called by CheckpointTriggerBehaviour.
    /// Fires OnCheckpointReached so UI can display feedback.
    /// </summary>
    /// <param name="checkpointId">Manually assigned checkpoint ID.</param>
    /// <param name="respawnPosition">World-space position the player respawns at.</param>
    public void SetCheckpoint(int checkpointId, Vector3 respawnPosition)
    {
        LastCheckpointId       = checkpointId;
        LastCheckpointPosition = respawnPosition;

        OnCheckpointReached?.Invoke(checkpointId, respawnPosition);

        Debug.Log($"[GameManager] Checkpoint set — ID: {checkpointId}, Position: {respawnPosition}");
    }

    #endregion

    #region PlayerFlow

    /// <summary>
    /// Called by the player when it dies.
    /// Fires OnPlayerDied, then manages the full death → respawn → playing sequence:
    ///   1. Transitions to PlayerDeath (disables input, pauses timer)
    ///   2. Waits for respawn delay
    ///   3. Transitions to Respawning
    ///   4. Fires OnPlayerRespawned — subscriber teleports the player
    ///   5. Transitions to Playing (re-enables input, resumes timer)
    ///
    /// Has no effect if the player is already in the death sequence.
    /// </summary>
    public void PlayerDied()
    {
        if (CurrentState != GameState.PlayerDeath)
        {
            OnPlayerDied?.Invoke();
            SetState(GameState.PlayerDeath);
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);

        SetState(GameState.Respawning);

        // Fire before re-enabling input so the subscriber (PlayerController)
        // completes the teleport while the player is still locked out of controls.
        OnPlayerRespawned?.Invoke(LastCheckpointPosition);

        SetState(GameState.Playing);
    }

    /// <summary>
    /// Resets all game data and reloads the active scene from scratch.
    /// RunTime is reset; FinalRunTime holds the last completed run until
    /// InitializeGame() is called, so a score screen can still read it first.
    /// </summary>
    public void RestartLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StopAllCoroutines();
        InitializeGame();
        SceneManager.LoadScene(currentSceneIndex);
    }

    /// <summary>Resets all game data and loads the main menu (build index 0).</summary>
    public void GoToMenu()
    {
        StopAllCoroutines();
        InitializeGame();
        SceneManager.LoadScene(0);
    }

    #endregion

    #region Companions

    // ── Player ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The current player GameObject. Set by <see cref="PlayerRegistrar"/> on Awake
    /// in every scene that contains a player.
    /// </summary>
    public GameObject Player { get; private set; }

    // ── Per-slot follow anchors ───────────────────────────────────────────────

    /// <summary>
    /// Ordered list of follow-anchor Transforms on the player — one per companion.
    /// Companion registration order determines which anchor each one receives:
    ///   first registered → index 0, second → index 1, third → index 2.
    /// Populated by <see cref="PlayerRegistrar"/> on each scene load.
    /// </summary>
    public IReadOnlyList<Transform> FollowAnchors => _followAnchors;
    private readonly List<Transform> _followAnchors = new();

    /// <summary>
    /// Returns the follow anchor at the given index, or null if out of range.
    /// Index corresponds to companion registration order.
    /// </summary>
    public Transform GetFollowAnchor(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _followAnchors.Count)
        {
            Debug.LogWarning($"[GameManager] GetFollowAnchor: slot {slotIndex} out of range " +
                             $"(registered anchors: {_followAnchors.Count}).");
            return null;
        }
        return _followAnchors[slotIndex];
    }

    /// <summary>
    /// Called by <see cref="PlayerRegistrar"/> on Awake.
    /// Replaces the anchor list and pushes each companion its own slot anchor.
    /// </summary>
    /// <param name="player">The player GameObject in the just-loaded scene.</param>
    /// <param name="followAnchors">
    /// Ordered anchor Transforms — index must match each companion's SlotIndex.
    /// Falls back to a single-element list containing the player's own Transform if empty.
    /// </param>
    public void RegisterPlayer(GameObject player, List<Transform> followAnchors = null)
    {
        Player = player;

        _followAnchors.Clear();

        if (followAnchors != null && followAnchors.Count > 0)
            _followAnchors.AddRange(followAnchors);
        else
            _followAnchors.Add(player.transform);

        Debug.Log($"[GameManager] Player registered: {player.name}. " +
                  $"Follow anchors: {_followAnchors.Count}.");

        PushAnchorsToCompanions();
    }

    // ── Companion registry ────────────────────────────────────────────────────

    private readonly List<CompanionAI> _companions = new();

    /// <summary>Read-only view of all currently active companions.</summary>
    public IReadOnlyList<CompanionAI> Companions => _companions;

    /// <summary>
    /// Called by <see cref="CompanionAI"/> in its own Awake.
    /// Idempotent — safe to call multiple times for the same companion.
    /// Registration order determines anchor assignment: first companion registered
    /// gets anchor 0, second gets anchor 1, etc.
    /// </summary>
    public void RegisterCompanion(CompanionAI companion)
    {
        if (_companions.Contains(companion)) return;

        int index = _companions.Count; // capture before Add
        _companions.Add(companion);
        Debug.Log($"[GameManager] Companion registered: {companion.name} " +
                  $"→ anchor {index} (total: {_companions.Count})");

        Transform anchor = GetFollowAnchor(index);
        if (anchor != null)
            companion.SetFollowTarget(anchor);
    }

    /// <summary>
    /// Called by <see cref="CompanionAI"/> in OnDestroy.
    /// </summary>
    public void UnregisterCompanion(CompanionAI companion)
    {
        if (!_companions.Remove(companion)) return;
        Debug.Log($"[GameManager] Companion unregistered: {companion.name} " +
                  $"(total: {_companions.Count})");
    }

    /// <summary>
    /// Pushes each companion its own anchor based on its SlotIndex.
    /// Called after anchors are refreshed on scene load.
    /// </summary>
    private void PushAnchorsToCompanions()
    {
        for (int i = 0; i < _companions.Count; i++)
        {
            Transform anchor = GetFollowAnchor(i);
            if (anchor != null)
                _companions[i].SetFollowTarget(anchor);
        }
    }

    #endregion

    #region SceneLoader

    /// <summary>
    /// Loads a scene by build index.
    /// Intended as the hook for the elevator scene-transition system.
    /// </summary>
    /// <param name="sceneId">Build index of the target scene.</param>
    public void LoadNextScene(int sceneId)
    {
        StopAllCoroutines();
        SceneManager.LoadSceneAsync(sceneId);
    }

    /// <summary>
    /// Logs each scene load and confirms the active companion count.
    /// The actual follow-target re-hook happens in RegisterPlayer(), called by
    /// the player prefab's PlayerRegistrar on Awake in the new scene.
    /// </summary>
    private void OnSceneLoaded_Companions(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: '{scene.name}'. " +
                  $"Active companions: {_companions.Count}.");
    }

    #endregion

    #region Cinematics

    /// <summary>
    /// Triggers a cinematic by integer ID and disables player input for its duration.
    ///
    /// TODO: Provisional signature for early development.
    /// Replace int cinematicId with a CinematicDataSO ScriptableObject once
    /// sequences are authored. The SO should carry: Timeline asset,
    /// skip-behaviour flag, camera rig reference, and any dialogue data.
    ///
    /// Future signature: public void TriggerCinematic(CinematicDataSO cinematic)
    /// </summary>
    /// <param name="cinematicId">Temporary integer identifier for the cinematic.</param>
    public void TriggerCinematic(int cinematicId)
    {
        SetInputEnabled(false);

        // TODO: Look up the cinematic by ID from a registered dictionary or
        //       ScriptableObject list, then play via Unity Timeline / Cinemachine.

        Debug.Log($"[GameManager] TriggerCinematic — ID: {cinematicId}. Wire up Timeline here.");
    }

    #endregion
}
