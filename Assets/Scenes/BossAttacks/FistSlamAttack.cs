using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "FistSlamAttack", menuName = "Boss/Attacks/Fist Slam")]
public class FistSlamAttack : BossAttack
{
    public float warningDuration = 1.5f;
    public float fistFallSpeed = 20f;
    public float impactRadius = 3f;
    public float spawnHeight = 15f;

    public override void Execute(Boss boss, Transform target)
    {
        boss.StartCoroutine(FistSlamRoutine(boss, target.position));
    }

    private IEnumerator FistSlamRoutine(Boss boss, Vector3 targetPosition)
    {
        Vector3 groundPos = new Vector3(targetPosition.x, 0.1f, targetPosition.z);

        // Warning indicator desde el Boss
        GameObject warning = Instantiate(boss.warningIndicatorPrefab, groundPos, Quaternion.identity);
        yield return new WaitForSeconds(warningDuration);
        Destroy(warning);

        // Puño desde el Boss
        Vector3 spawnPos = groundPos + Vector3.up * spawnHeight;
        GameObject fist = Instantiate(boss.fistPrefab, spawnPos, Quaternion.identity);

        while (fist != null && fist.transform.position.y > groundPos.y)
        {
            fist.transform.position += Vector3.down * fistFallSpeed * Time.deltaTime;
            yield return null;
        }

        Collider[] hits = Physics.OverlapSphere(groundPos, impactRadius);
        foreach (var hit in hits)
            hit.GetComponent<IHealthable>()?.TakeDamage(damage);

        if (fist != null) Destroy(fist, 0.3f);
    }
}