using System.Collections;
using UnityEngine;

public class Player : NPC
{
    //public float Health { get; private set; } = 100;
    //public float MaxHealth { get; private set; } = 100;
    public override bool CanBeHurt { get; set; } = true;
    public override  float ITime { get; set; } = 0.5f;
    public override float Health { get; set; } = 100;
    public override float MaxHealth { get; set; } = 100;
    public override float Damage { get; set; } = 25;
    public override float AttackCooldown { get; set; }
    public override float AttackSpeed { get; set; }
    public override float AttackRange { get; set; }

    public GameObject sword;
    CharacterController CC;

        

    void Start()
    {
        CC = GetComponent<CharacterController>();
        Debug.Log("start");
        GayManager.Instance.ElevatorTeleport += TeleportTo;
    }
    private void OnEnable()
    {
        // Subscribe whenever this component is active.
        GayManager.Instance.OnPlayerDied += HandleDeath;
        GayManager.Instance.OnPlayerRespawned += HandleRespawn;
    }

    private void OnDisable()
    {
        // Always unsubscribe to avoid ghost callbacks.
        if (GayManager.Instance)
        {
            GayManager.Instance.OnPlayerDied -= HandleDeath;
            GayManager.Instance.OnPlayerRespawned -= HandleRespawn;
        }
    }

    public override void TakeDamage(float damage)
    {
        //Debug.Log("ICD: " + CanBeHurt);

        base.TakeDamage(damage);
    }


    public override void Heal(float heal)
    {
        Health += heal;
    }

    public override IEnumerator InvulnerabilityCD()
    {
        yield return base.InvulnerabilityCD();
    }

    public override void Die()
    {
        // Disable movement so the player can't act during the death sequence.
        if (CC) CC.enabled = false;

        // Hand off to GayManager — it owns the state machine, the delay,
        // and will fire OnPlayerRespawned when the sequence completes.
        GayManager.Instance.PlayerDied();
        CC.enabled = true;
    }

    public override void Attack()
    {
        
    }
    
    /// <inheritdoc/>
    public void OnSave(SaveData data)
    {
        data.PlayerHealth = Health;
    }

    /// <inheritdoc/>
    public void OnLoad(SaveData data)
    {
        Health = data.PlayerHealth;
    }

    public void TeleportTo()
    {
        CC.enabled = false;
        Debug.Log("try to teleport");
        this.transform.position = new Vector3(63.5f, 48.0660019f, 82.3000031f);
        CC.enabled = true;


    }
    private void HandleDeath()
    {
        Debug.Log("[PlayerHealth] Death handled — play death anim / VFX here.");
        
    }
    private void HandleRespawn(Vector3 respawnPosition)
    {
        // Disable the controller briefly so the teleport doesn't get rejected.
        if (CC) CC.enabled = false;

        transform.position = respawnPosition;

        if (CC) CC.enabled = true;

        // Restore HP.
        Health = MaxHealth;

        Debug.Log($"[PlayerHealth] Respawned at {respawnPosition}. HP restored to {Health}.");
        // e.g. _animator.SetTrigger("Respawn");
        //      _respawnVFX.Play();
    }
}