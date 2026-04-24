using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Companion AI that follows the player via NavMesh, detects enemies with a
/// throttled OverlapSphere (Quest 3 / Android-friendly), and executes typed
/// normal and ultimate skills.
///
/// ── Persistence ─────────────────────────────────────────────────────────────
/// DontDestroyOnLoad keeps the prefab alive. After each scene load, GameManager
/// calls SetFollowTarget() with the fresh anchor — zero manual re-wiring.
/// Skill ScriptableObject references are baked into the prefab and survive every
/// scene transition automatically.
///
/// ── Collider split (rule 4) ──────────────────────────────────────────────────
/// • CompanionRangeBoundary on the player's follow-anchor child
///     → SphereCollider trigger, radius = _maxFollowRange
///     → Exposes IsCompanionInRange; zero per-frame cost.
/// • Enemy detection here → Physics.OverlapSphere throttled to _detectionInterval.
///     No trigger colliders needed on enemies.
///
/// ── State wiring ────────────────────────────────────────────────────────────
/// Subscribes to GameManager.OnGameStateChanged.
/// AI loop activates on Combat / BossCombat / Playing; pauses otherwise.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CompanionAI : NPC, ISaveable
{
    // ─────────────────────────────────────────────────────────────────────────
    // NPC abstract property implementations
    // ─────────────────────────────────────────────────────────────────────────

    [Header("NPC Stats")]
    [SerializeField] private float _health         = 100f;
    [SerializeField] private float _damage         = 10f;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private float _attackSpeed    = 1f;
    [SerializeField] private float _attackRange    = 2f;

    /// <summary>Configured max health — captured once at Awake before any damage is taken.</summary>
    public float MaxHealth { get; private set; }

    public override float Health
    {
        get => _health;
        set => _health = value;
    }
    public override float Damage
    {
        get => _damage;
        set => _damage = value;
    }
    public override float AttackCooldown
    {
        get => _attackCooldown;
        set => _attackCooldown = value;
    }
    public override float AttackSpeed
    {
        get => _attackSpeed;
        set => _attackSpeed = value;
    }
    public override float AttackRange
    {
        get => _attackRange;
        set => _attackRange = value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Follow & movement
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Follow Settings")]
    [Tooltip("Design-time reference, overwritten at runtime by GameManager.")]
    [SerializeField] private Transform _followTarget;

    [SerializeField] private float _maxFollowRange    = 10f;
    [SerializeField] private float _followStopDistance = 2f;
    [SerializeField] private float _loseFocusDelay     = 5f;


    // ─────────────────────────────────────────────────────────────────────────

    [Header("Detection")]
    [SerializeField] private float     _detectionRange    = 8f;
    [SerializeField] private float     _detectionInterval = 0.2f;
    [SerializeField] private LayerMask _enemyLayer;

    // ─────────────────────────────────────────────────────────────────────────
    // Skills
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Skills")]
    [Tooltip("Normal skill — only NormalSkill assets accepted here.")]
    [SerializeField] private NormalSkill   _skill;

    [Tooltip("Ultimate skill — only UltimateSkill assets accepted here.")]
    [SerializeField] private UltimateSkill _ultimate;

    [Tooltip("HP ratio (0–1) the caster must drop below to trigger a healing normal skill.")]
    [SerializeField, Range(0f, 1f)] private float _healTriggerThreshold = 0.5f;

    [Tooltip("HP ratio (0–1) any party member must drop below to trigger the ultimate heal.")]
    [SerializeField, Range(0f, 1f)] private float _ultimateHealThreshold = 0.3f;

    // ─────────────────────────────────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────────────────────────────────

    private NavMeshAgent _agent;
    private GameObject   _currentTarget;
    private bool         _hasTarget;
    private float        _loseFocusTimer;
    private bool         _attackOnCooldown;
    private bool         _skillOnCooldown;
    private bool         _ultimateUsed;
    private float        _detectionTimer;

    /// <summary>
    /// Controlled by the GameManager state subscription.
    /// When false the Update loop exits immediately — no NavMesh, no scans.
    /// </summary>
    private bool _aiEnabled;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        MaxHealth = _health;
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = _followStopDistance;

        if (GameManager.Instance)
        {
            GameManager.Instance.RegisterCompanion(this);
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            // Sync to the state that may already be active.
            ApplyAIState(GameManager.Instance.CurrentState);
        }
        else
        {
            Debug.LogWarning($"[CompanionAI] {name}: GameManager not found during Awake.");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.UnregisterCompanion(this);
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    private void Update()
    {
        if (!_aiEnabled || !_followTarget) return;

        EnforceMaxRange();

        _detectionTimer += Time.deltaTime;
        if (_detectionTimer >= _detectionInterval)
        {
            _detectionTimer = 0f;
            ScanForEnemies();
        }

        if (_hasTarget)
            HandleCombatMovement();
        else
            HandleFollowMovement();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GameManager state subscription
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGameStateChanged(GameManager.GameState previous,
                                    GameManager.GameState next)
    {
        ApplyAIState(next);
    }

    /// <summary>
    /// Activates the AI loop for states where companions should be active,
    /// and pauses it for everything else (menus, paused, death, win/lose).
    /// </summary>
    private void ApplyAIState(GameManager.GameState state)
    {
        bool shouldBeActive = state is GameManager.GameState.Playing
                                    or GameManager.GameState.Combat
                                    or GameManager.GameState.BossCombat;

        SetAIEnabled(shouldBeActive);
    }

    /// <summary>
    /// Enables or disables the AI loop and NavMesh movement.
    /// Clears combat state when disabling so the companion doesn't resume
    /// mid-fight after a pause or death screen.
    /// </summary>
    private void SetAIEnabled(bool enabled)
    {
        _aiEnabled = enabled;
        _agent.isStopped = !enabled;

        if (!enabled)
        {
            LoseFocus();
            _agent.ResetPath();
            Debug.Log($"[CompanionAI] {name}: AI paused (state-driven).");
        }
        else
        {
            Debug.Log($"[CompanionAI] {name}: AI activated (state-driven).");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — called by GameManager
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reassigns the follow target. Called by GameManager after each scene load.
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        Debug.Log($"[CompanionAI] {name}: follow target → '{target?.name}'.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Movement
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rule 3 — if the companion has drifted beyond the max range, redirect it
    /// back toward the follow target via SetDestination.
    /// Uses a plain distance check — no trigger sphere needed, no Warp, no
    /// underground teleport risk.
    /// </summary>
    private void EnforceMaxRange()
    {
        if (Vector3.Distance(transform.position, _followTarget.position) <= _maxFollowRange)
            return;

        // Don't fight the NavMesh — just redirect toward the anchor.
        _agent.SetDestination(_followTarget.position);
        Debug.Log($"[CompanionAI] {name}: outside max range — redirecting to anchor.");
    }

    private void HandleFollowMovement()
    {
        // Only issue a new destination if the anchor has moved meaningfully.
        // Updating every frame while the player is stationary keeps the agent
        // in a permanent "not arrived" state, causing the oscillation wiggle.
        float anchorDelta = Vector3.Distance(_agent.destination, _followTarget.position);
        if (anchorDelta > _followStopDistance * 0.5f)
            _agent.SetDestination(_followTarget.position);
    }

    /// <summary>
    /// Rule 1 — pursue enemy; lose focus after _loseFocusDelay seconds if the
    /// enemy stays beyond _maxFollowRange from the player anchor.
    /// </summary>
    private void HandleCombatMovement()
    {
        if (!_currentTarget)
        {
            LoseFocus();
            return;
        }

        bool enemyOutOfRange =
            Vector3.Distance(_currentTarget.transform.position, _followTarget.position)
            > _maxFollowRange;

        if (enemyOutOfRange)
        {
            _loseFocusTimer += Time.deltaTime;
            Debug.Log($"[CompanionAI] {name}: target out of range " +
                      $"({_loseFocusTimer:F1}/{_loseFocusDelay}s)");

            if (_loseFocusTimer >= _loseFocusDelay) LoseFocus();
            return;
        }

        _loseFocusTimer = 0f;

        float distToEnemy = Vector3.Distance(transform.position,
                                             _currentTarget.transform.position);
        if (distToEnemy <= _attackRange)
        {
            _agent.ResetPath();

            // Basic attack — independent cooldown.
            if (!_attackOnCooldown)
                StartCoroutine(AttackRoutine());

            // Normal skill — cooldown from the SO itself.
            // Healing skills check caster HP; offensive skills fire freely on cooldown.
            if (!_skillOnCooldown && SkillShouldFire())
                StartCoroutine(SkillRoutine());

            // Ultimate — fires once per engagement.
            // Healing ultimates check party HP; offensive ultimates check target HP.
            if (!_ultimateUsed && UltimateShouldFire())
                StartCoroutine(UltimateRoutine());
        }
        else
        {
            _agent.stoppingDistance = _attackRange;
            _agent.SetDestination(_currentTarget.transform.position);
        }
    }

    /// <summary>
    /// Returns true when the normal skill should fire.
    /// Healing skills: only when caster HP is below the heal trigger threshold.
    /// Offensive skills: freely, governed only by the SO cooldown timer.
    /// </summary>
    private bool SkillShouldFire()
    {
        if (!_skill) return false;

        if (_skill is HealSkill)
            return Health / MaxHealth <= _healTriggerThreshold;

        // Offensive — always ready once the cooldown timer clears.
        return true;
    }

    /// <summary>
    /// Returns true when the ultimate should fire.
    /// Healing ultimates: when any party member is below the ultimate heal threshold.
    /// Offensive ultimates: when the current target is below the same threshold.
    /// </summary>
    private bool UltimateShouldFire()
    {
        if (!_ultimate) return false;

        if (_ultimate is UltimateHealSkill)
        {
            // Check every companion and the player for low HP.
            if (!GameManager.Instance) return false;

            foreach (CompanionAI companion in GameManager.Instance.Companions)
            {
                if (companion.Health / companion.MaxHealth <= _ultimateHealThreshold)
                    return true;
            }

            // Also check player if it implements IHurtable.
            if (GameManager.Instance.Player &&
                GameManager.Instance.Player.TryGetComponent(out IHurtable playerHurtable) &&
                playerHurtable.Health <= _ultimateHealThreshold)
                return true;

            return false;
        }

        // Offensive ultimate — check current target HP.
        if (_currentTarget &&
            _currentTarget.TryGetComponent(out IHurtable targetHurtable))
            return targetHurtable.Health <= _ultimateHealThreshold;

        return false;
    }

    private void LoseFocus()
    {
        Debug.Log($"[CompanionAI] {name}: lost focus — returning to player.");
        _currentTarget          = null;
        _hasTarget              = false;
        _loseFocusTimer         = 0f;
        _skillOnCooldown        = false;
        _ultimateUsed           = false;
        _agent.stoppingDistance = _followStopDistance;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Enemy detection — throttled OverlapSphere (rule 4 / Quest 3 perf)
    // ─────────────────────────────────────────────────────────────────────────

    private void ScanForEnemies()
    {
        if (_hasTarget || !_followTarget) return;

        Collider[] hits = Physics.OverlapSphere(transform.position,
                                                _detectionRange, _enemyLayer);
        foreach (Collider col in hits)
        {
            if (Vector3.Distance(col.transform.position, _followTarget.position)
                > _maxFollowRange) continue;

            AcquireTarget(col.gameObject);
            return;
        }
    }

    private void AcquireTarget(GameObject enemy)
    {
        _currentTarget  = enemy;
        _hasTarget      = true;
        _loseFocusTimer = 0f;
        Debug.Log($"[CompanionAI] {name}: acquired target '{enemy.name}'.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NPC abstract implementations
    // ─────────────────────────────────────────────────────────────────────────

    public override void Die()
    {
        Debug.Log($"[CompanionAI] {name}: died.");
        GameManager.Instance?.UnregisterCompanion(this);
        gameObject.SetActive(false);
    }

    public override void Attack()
    {
        string t = _currentTarget ? _currentTarget.name : "nothing";
        Debug.Log($"[CompanionAI] {name}: basic attack → {t} for {Damage}.");

        if (_currentTarget &&
            _currentTarget.TryGetComponent(out IHurtable hurtable))
            hurtable.TakeDamage(Damage);
    }

    public void UseSkill()
    {
        if (!_skill)
        {
            Debug.LogWarning($"[CompanionAI] {name}: no NormalSkill assigned.");
            return;
        }
        Debug.Log($"[CompanionAI] {name}: using skill '{_skill.SkillName}'.");
        StartCoroutine(_skill.ExecuteCoroutine(this, _currentTarget));
    }

    public void UseUltimate()
    {
        if (!_ultimate)
        {
            Debug.LogWarning($"[CompanionAI] {name}: no UltimateSkill assigned.");
            return;
        }
        Debug.Log($"[CompanionAI] {name}: using ultimate '{_ultimate.SkillName}'.");
        StartCoroutine(_ultimate.ExecuteCoroutine(this, _currentTarget));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        _attackOnCooldown = true;
        Attack();
        yield return new WaitForSeconds(AttackCooldown);
        _attackOnCooldown = false;
    }

    private IEnumerator SkillRoutine()
    {
        _skillOnCooldown = true;
        UseSkill();
        yield return new WaitForSeconds(_skill.Cooldown);
        _skillOnCooldown = false;
    }

    private IEnumerator UltimateRoutine()
    {
        _ultimateUsed = true;
        UseUltimate();
        yield break;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Debug Gizmos (rule 5) — all radii driven by serialized fields
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Detection range — yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        // Attack range — red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        if (_followTarget)
        {
            // Max follow range — green (on the anchor)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_followTarget.position, _maxFollowRange);

            // Follow stop distance — cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_followTarget.position, _followStopDistance);
        }

        // Line to current target — magenta
        if (_currentTarget)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }
    }
    
        #region ISaveable

    /// <summary>
    /// Writes this companion's health into the correct index of
    /// <see cref="SaveData.CompanionHealths"/>.
    ///
    /// Index is resolved from the companion's position in
    /// <see cref="GameManager.Companions"/> — the same index that determines
    /// which follow anchor it receives, so ordering is always consistent.
    ///
    /// If the list is shorter than the companion's index, it is padded with
    /// -1 (sentinel for "slot was empty at save time") so deserialization
    /// can detect gaps safely.
    /// </summary>
    public void OnSave(SaveData data)
    {
        if (!GameManager.Instance)
        {
            Debug.LogWarning($"[CompanionAI] {name}: OnSave — GameManager not found.");
            return;
        }

        int index = GameManager.Instance.GetCompanionIndex(this);
        if (index < 0)
        {
            Debug.LogWarning($"[CompanionAI] {name}: OnSave — companion not registered in GameManager.");
            return;
        }

        // Pad any gaps with -1 so the list index always matches the slot index.
        while (data.CompanionHealths.Count <= index)
            data.CompanionHealths.Add(-1f);

        data.CompanionHealths[index] = Health;
    }

    /// <summary>
    /// Restores this companion's health from its own index in
    /// <see cref="SaveData.CompanionHealths"/>.
    ///
    /// A stored value of -1 means this slot was empty when the game was saved
    /// (e.g. the companion had already died); in that case no restoration is
    /// applied and the companion keeps its default health.
    /// </summary>
    public void OnLoad(SaveData data)
    {
        if (!GameManager.Instance)
        {
            Debug.LogWarning($"[CompanionAI] {name}: OnLoad — GameManager not found.");
            return;
        }

        int index = GameManager.Instance.GetCompanionIndex(this);
        if (index < 0)
        {
            Debug.LogWarning($"[CompanionAI] {name}: OnLoad — companion not registered in GameManager.");
            return;
        }

        if (index >= data.CompanionHealths.Count)
        {
            Debug.LogWarning($"[CompanionAI] {name}: OnLoad — no saved health for slot {index}. " +
                             "Keeping default health.");
            return;
        }

        float saved = data.CompanionHealths[index];
        if (saved < 0f)
        {
            Debug.Log($"[CompanionAI] {name}: OnLoad — slot {index} was empty at save time. " +
                      "Keeping default health.");
            return;
        }

        Health = saved;
        Debug.Log($"[CompanionAI] {name}: OnLoad — health restored to {Health} (slot {index}).");
    }

    #endregion
}
