using UnityEngine;

/// <summary>
/// Attach alongside DissolveVoronoiController.
/// Plays a ParticleSystem when dissolve begins and stops it when dissolve ends.
/// The ParticleSystem Shape module should be set to SkinnedMeshRenderer or Mesh
/// so Unity handles surface distribution automatically — no CPU math needed.
/// </summary>
[RequireComponent(typeof(DissolveVoronoiController))]
public class DissolveParticleEmitter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The particle system that produces the dissolve motes. " +
             "Place it as a child of this GameObject so it moves with the character.")]
    [SerializeField]
    private ParticleSystem dissolveParticles;

    [Header("Emission")]
    [Tooltip("Particles emitted per second while dissolving. " +
             "Scale with character size — humanoid ~80-120 is a good starting point.")]
    [Range(10, 300)]
    [SerializeField]
    private  int emissionRate = 50;

    [Tooltip("Burst of particles fired the instant dissolve starts, " +
             "to immediately populate the effect without waiting for steady-state emission.")]
    [Range(0, 150)]
    [SerializeField]
    private  int initialBurst = 40;

    [Tooltip("Match this to the _EdgeColor on the dissolve material.")]
    [SerializeField]
    private  Color moteColor = new Color(0f, 1f, 0.8f, 1f);

    // ── Internal ─────────────────────────────────────────────────────────
    private DissolveVoronoiController _controller;
    private ParticleSystem.EmissionModule _emission;
    private bool _wasDissolving;

    private void Awake()
    {
        _controller = GetComponent<DissolveVoronoiController>();

        if (!dissolveParticles)
        {
            Debug.LogWarning("[DissolveParticleEmitter] No ParticleSystem assigned.", this);
            return;
        }

        _emission = dissolveParticles.emission;
        _emission.enabled = false;

        // Apply emission rate — main module colour is set from inspector
        var main = dissolveParticles.main;
        main.startColor = moteColor;
    }

    private void OnEnable()
    {
        // Subscribe to controller events via polling in Update —
        // keeps the controller free of emitter dependencies.
        _wasDissolving = false;
    }

    private void Update()
    {
        if (dissolveParticles)
        {
            bool isDissolving = _controller.IsDissolving;

            // Dissolve just started
            if (isDissolving && !_wasDissolving)
                OnDissolveStarted();

            // Dissolve just ended
            if (!isDissolving && _wasDissolving)
                OnDissolveEnded();

            _wasDissolving = isDissolving;
        }
    }

    private void OnDissolveStarted()
    {
        var emission = dissolveParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        dissolveParticles.Play();

        // Burst to immediately populate the effect
        if (initialBurst > 0)
            dissolveParticles.Emit(initialBurst);
    }

    private void OnDissolveEnded()
    {
        // Stop emitting new particles but let existing ones finish their lifetime
        var emission = dissolveParticles.emission;
        emission.enabled = false;
        // dissolveParticles.Stop() would kill existing particles instantly —
        // we intentionally don't call it so the last motes drift away naturally.
    }

    /// <summary>Call this if you need to hard-reset mid-effect (e.g. object pooling).</summary>
    public void ForceStop()
    {
        if (dissolveParticles)
        {
            var emission = dissolveParticles.emission;
            emission.enabled = false;
            dissolveParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _wasDissolving = false;
        }
    }
}
