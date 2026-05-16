using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "LaserAttack", menuName = "Boss/Attacks/Laser")]
public class LaserAttack : BossAttack
{
    public float laserDuration = 2.5f;
    public float damageTickRate = 0.2f;
    public float laserLength = 40f;
    public float laserWidth = 0.1f;

    private const float SWEEP_DEGREES = 30f;
    private const float START_OFFSET = -15f;

    [HideInInspector] public Transform laserOrigin;

    public override void Execute(Boss boss, Transform target)
    {
        laserOrigin = boss.laserOrigin;
        boss.StartCoroutine(LaserRoutine(boss, target));
    }

    private IEnumerator LaserRoutine(Boss boss, Transform target)
    {
        GameObject laser = Instantiate(boss.laserPrefab, laserOrigin.position, Quaternion.identity);
        boss.laserInstance = laser;
        laser.transform.SetParent(boss.transform); 

        laser.transform.localScale = new Vector3(laserWidth, laserWidth, laserLength);

        Vector3 dirToTarget = (target.position - laserOrigin.position).normalized;
        Quaternion baseRotation = Quaternion.LookRotation(dirToTarget);

        Quaternion startRotation = baseRotation * Quaternion.Euler(0f, START_OFFSET, 0f);
        Quaternion endRotation = baseRotation * Quaternion.Euler(0f, START_OFFSET + SWEEP_DEGREES, 0f);

        laser.transform.rotation = startRotation;

        float elapsed = 0f;
        float damageTimer = 0f;

        while (elapsed < laserDuration)
        {
            float t = elapsed / laserDuration;

            laser.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            laser.transform.position = new Vector3(laserOrigin.position.x, laserOrigin.position.y, laserOrigin.position.z);
            elapsed += Time.deltaTime;
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageTickRate)
            {
                damageTimer = 0f;
                CheckLaserHit(boss, laser);
            }

            yield return null;
        }

        laser.transform.rotation = endRotation;
        Destroy(laser);
    }

    private void CheckLaserHit(Boss boss, GameObject laser)
    {
        Vector3 origin = laser.transform.position;
        Vector3 direction = laser.transform.forward;

        int layerMask = ~(1 << boss.gameObject.layer);

        //Debug.DrawRay(origin, direction * laserLength, Color.red, damageTickRate);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, laserLength, layerMask))
        {
            Debug.Log($"Laser hit: {hit.collider.name} | Parent: {hit.collider.transform.parent?.name}");

            IHealthable healthable = null;

            if (hit.collider.transform.parent != null)
                hit.collider.transform.parent.TryGetComponent(out healthable);

            if (healthable == null)
                hit.collider.TryGetComponent(out healthable);

            if (healthable != null && healthable.CanBeHurt)
            {
                Debug.Log("Dealing damage!");
                healthable.TakeDamage(20);
            }
        }
    }
}