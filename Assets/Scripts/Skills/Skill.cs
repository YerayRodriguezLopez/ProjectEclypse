using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base for every companion skill.
/// Carries shared metadata and enforces the Execute contract.
///
/// Single-hit skills override Execute() only.
/// Multi-hit skills (DoubleAttack, UltimateCombo) override ExecuteCoroutine() so
/// they can yield between hits and respect the target's InvulnerabilityCD.
/// The caster MonoBehaviour owns the coroutine lifetime.
/// </summary>
public abstract class Skill : ScriptableObject
{
    [SerializeField] private string _skillName = "Unnamed Skill";
    [SerializeField, TextArea] private string _description = "";
    [SerializeField] private float _cooldown = 3f;

    public string SkillName   => _skillName;
    public string Description => _description;
    public float  Cooldown    => _cooldown;

    /// <summary>
    /// Instant, single-step skill logic.
    /// Called directly for skills that apply their effect in one frame.
    /// Multi-hit skills should leave this empty and override ExecuteCoroutine instead.
    /// </summary>
    public abstract void Execute(NPC caster, GameObject target = null);

    /// <summary>
    /// Coroutine entry point for multi-hit skills that must wait between hits
    /// so the target's InvulnerabilityCD has time to reset CanBeHurt.
    ///
    /// Default implementation just calls Execute() synchronously, so single-hit
    /// skills never need to override this.
    /// </summary>
    public virtual IEnumerator ExecuteCoroutine(NPC caster, GameObject target = null)
    {
        Execute(caster, target);
        yield break;
    }
}
