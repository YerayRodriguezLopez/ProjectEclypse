using System.Collections;
using System.Linq;
using UnityEngine;

public class Boss : NPC
{
    public override float Health { get; set; } = 30;
    public override float MaxHealth { get; set; } = 30;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 6f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;

    [Header("Referencias")]
    public Transform playerTransform;
    public Transform laserOrigin;

    [Header("Configuración de Rotación")]
    [Tooltip("Velocidad a la que gira el boss. Valores altos = giro más rápido.")]
    public float rotationSpeed = 5f;

    [Header("Prefabs compartidos")]
    public GameObject warningIndicatorPrefab;
    public GameObject fistPrefab;
    public GameObject rockPrefab;
    public GameObject laserPrefab;

    [Header("Ataques")]
    public BossAttack[] attacks;

    private bool isAlive = true;
    private Animator animator;
    public GameObject laserInstance;

    private void Start()
    {
        this.ITime = 1f;
        MaxHealth = Health;
        animator = GetComponent<Animator>();
        StartCoroutine(AttackRoutine());
    }

    // El Update se encarga de mirar al jugador en cada frame
    private void Update()
    {
        // Solo rota si está vivo y si se ha asignado el transform del jugador
        if (!isAlive || playerTransform == null) return;

        LookAtPlayerHorizontal();
    }

    private void LookAtPlayerHorizontal()
    {
        // Calculamos la dirección del vector desde el boss al jugador
        Vector3 direction = playerTransform.position - transform.position;

        // Forzamos que no haya diferencia en el eje Y para evitar que el boss se incline hacia arriba/abajo
        direction.y = 0;

        // Evitamos error si el jugador está exactamente en la misma posición del boss
        if (direction != Vector3.zero)
        {
            // Creamos la rotación hacia el objetivo
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Rotamos de forma fluida usando Lerp
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            if(laserInstance != null)
            laserInstance.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
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
            {
                cumulative += attack.weight;
                if (roll < cumulative) return attack;
            }
        }

        return attacks[0];
    }

    public override void Die()
    {
        isAlive = false;
        StopAllCoroutines();
        Debug.Log("Boss derrotado!");
        animator.SetTrigger("Dead");
    }

    public override void TakeDamage(float damage)
    {
        if (laserInstance != null)
            laserInstance.SetActive(false);

        if (damage < this.Health)
        {
            base.TakeDamage(damage);
            //StopAllCoroutines();
            animator.SetTrigger("Hit");
            //StartCoroutine(AttackRoutine());
        }
        else
        {
            Die();
        }
    }

    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(AttackCooldown);

        while (isAlive)
        {
            Attack();
            yield return new WaitForSeconds(AttackCooldown);
        }
    }
}