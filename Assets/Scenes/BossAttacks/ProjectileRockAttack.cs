using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileRockAttack", menuName = "Boss/Attacks/Projectile Rock")]
public class ProjectileRockAttack : BossAttack
{
    public float projectileSpeed = 40f;
    public float arcHeight = 5f;
    public float lifetime = 5f;
    public float impactRadius = 2f;

    public override void Execute(Boss boss, Transform target)
    {
        boss.StartCoroutine(ShootRock(boss, target));
    }

    private IEnumerator ShootRock(Boss boss, Transform target)
    {
        // Spawnea la roca en la posición del boss (ligeramente elevada)
        Vector3 startPos = boss.transform.position + Vector3.up * 2f;
        GameObject rock = Instantiate(boss.rockPrefab, startPos, Random.rotation);

        Vector3 targetPos = target.position;
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            // Si la roca llega cerca del objetivo, impacta
            if (Vector3.Distance(rock.transform.position, targetPos) < 0.5f)
                break;

            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            // Interpolación con arco parabólico
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
            float arc = Mathf.Sin(Mathf.PI * t) * arcHeight;
            rock.transform.position = linearPos + Vector3.up * arc;

            // Rota la roca para que parezca que rueda en el aire
            rock.transform.Rotate(Vector3.right * 200f * Time.deltaTime);

            yield return null;
        }

        if (rock == null) yield break;

        // Impacto
        Collider[] hits = Physics.OverlapSphere(rock.transform.position, impactRadius);
        foreach (var hit in hits)
            hit.GetComponent<IHealthable>()?.TakeDamage(damage);

        Destroy(rock);
    }
}