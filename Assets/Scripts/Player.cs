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

        

    void Start()
    {
     
    }
    

    public override void TakeDamage(float damage)
    {
        //Debug.Log("ICD: " + CanBeHurt);

        base.TakeDamage(damage);
    }


    public void Heal(float heal)
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
}
