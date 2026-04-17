using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour, IHurtable
{
    public float Health { get; private set; } = 100;
    public bool CanBeHurt { get; private set; } = true;
    public float ITime { get; private set; } = 0.5f;

    void Start()
    {
     
    }
    
    public void Die()
    {

    }
    public void TakeDamage(float damage)
    {
        if (CanBeHurt)
        {
            Health -= damage;
            if (Health <= 0) Die();
            else StartCoroutine(InvulnerabilityCD());
        }
    }

    public void Heal(float heal)
    {
        Health += heal;
    }

    public IEnumerator InvulnerabilityCD()
    {
        CanBeHurt = false;
        yield return new WaitForSeconds(ITime);
        CanBeHurt = true;
    }
}
