using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "LaserAttack", menuName = "Boss/Attacks/Laser")]
public class LaserAttack : BossAttack
{
    public float laserDuration = 2.5f;       // Cuánto dura el laser
    public float rotationSpeed = 45f;         // Grados por segundo que persigue al jugador
    public float damageTickRate = 0.2f;       // Cada cuántos segundos hace daño
    public float laserLength = 20f;
    public float laserWidth = 0.3f;

    public override void Execute(Boss boss, Transform target)
    {
        boss.StartCoroutine(LaserRoutine(boss, target));
    }

    private IEnumerator LaserRoutine(Boss boss, Transform target)
    {
        // Crea el laser desde el Boss
        GameObject laser = Instantiate(boss.laserPrefab, boss.transform.position, Quaternion.identity);
        laser.transform.SetParent(boss.transform); // Se mueve con el boss

        // Escala el LineRenderer o el objeto para que tenga la longitud correcta
        laser.transform.localScale = new Vector3(laserWidth, laserWidth, laserLength);

        float elapsed = 0f;
        float damageTimer = 0f;

        // Apunta inicialmente al jugador de golpe
        Vector3 dirToPlayer = (target.position - boss.transform.position).normalized;
        laser.transform.rotation = Quaternion.LookRotation(dirToPlayer);

        while (elapsed < laserDuration)
        {
            elapsed += Time.deltaTime;
            damageTimer += Time.deltaTime;

            // Rota lentamente hacia el jugador
            Vector3 targetDir = (target.position - boss.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            laser.transform.rotation = Quaternion.RotateTowards(
                laser.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime * 0.5f
            );

            // Daño por tick si el laser apunta al jugador
            if (damageTimer >= damageTickRate)
            {
                damageTimer = 0f;
                CheckLaserHit(boss, laser, target);
            }

            yield return null;
        }

        Destroy(laser);
    }

    private void CheckLaserHit(Boss boss, GameObject laser, Transform target)
    {
        // Raycast en la dirección del laser
        if (Physics.Raycast(boss.transform.position, laser.transform.forward,
            out RaycastHit hit, laserLength))
        {
            hit.collider.GetComponent<IHealthable>()?.TakeDamage(damage * damageTickRate);
        }
    }
}