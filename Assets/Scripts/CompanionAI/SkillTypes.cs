using UnityEngine;

/// <summary>
/// Mid-layer for regular (non-ultimate) skills.
/// Serialized fields typed as NormalSkill will only accept normal skill assets,
/// preventing accidental assignment of ultimates.
/// </summary>
public abstract class NormalSkill : Skill { }

/// <summary>
/// Mid-layer for ultimate skills.
/// Serialized fields typed as UltimateSkill will only accept ultimate skill assets,
/// preventing accidental assignment of normal skills.
/// </summary>
public abstract class UltimateSkill : Skill { }
