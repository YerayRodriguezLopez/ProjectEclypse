using System.Collections;
using System.Linq;
using UnityEngine;

public class Boss : NPC
{
    public override float Health { get; set; } = 100;
    public override float MaxHealth { get; set; } = 100;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 6f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;

    [Header("Referencias")]
    public Transform playerTransform;

    [Header("Prefabs compartidos")]
    public GameObject warningIndicatorPrefab;
    public GameObject fistPrefab;
    public GameObject rockPrefab;
    public GameObject laserPrefab;
    [Header("Ataques")]
    public BossAttack[] attacks;

    private bool isAlive = true;

    public AudioManager audioManager;

    private void Start()
    {
        MaxHealth = Health;
        StartCoroutine(AttackRoutine());
    }

    private void Awake()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
    }
    

    public override void Attack()
    {
        if (attacks == null || attacks.Length == 0) return;

        BossAttack selected = SelectWeightedAttack();
        Debug.Log($"Boss ejecuta: {selected.attackName}");
        selected.Execute(this, playerTransform);
    }

    private BossAttack SelectWeightedAttack()
    {
        int totalWeight = attacks.Sum(a => a.weight);
        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;
        foreach (var attack in attacks)
        {
            cumulative += attack.weight;
            if (roll < cumulative) return attack;
        }

        return attacks[0];
    }

    public override void Die()
    {
        isAlive = false;
        StopAllCoroutines();
        Debug.Log("Boss derrotado!");
    }

    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (isAlive)
        {
            Attack();
            yield return new WaitForSeconds(AttackCooldown);
        }
    }
}