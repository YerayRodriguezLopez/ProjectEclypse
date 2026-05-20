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
}