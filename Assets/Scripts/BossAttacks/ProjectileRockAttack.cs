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
        Vector3 startPos = boss.transform.position + Vector3.up * 2f;
        GameObject rock = Instantiate(boss.rockPrefab, startPos, Random.rotation);

        Vector3 targetPos = target.position;
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            if (Vector3.Distance(rock.transform.position, targetPos) < 0.5f)
                break;

            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
            float arc = Mathf.Sin(Mathf.PI * t) * arcHeight;
            rock.transform.position = linearPos + Vector3.up * arc;

            rock.transform.Rotate(Vector3.right * 200f * Time.deltaTime);

            yield return null;
        }

        if (rock == null) yield break;

        // Impacto
        boss.audioManager.Play(AudioClips.BossHit1);
        boss.audioManager.Play(AudioClips.BossHit2);
        Collider[] hits = Physics.OverlapSphere(rock.transform.position, impactRadius);
        foreach (var hit in hits)
            hit.GetComponent<IHealthable>()?.TakeDamage(damage);

        Destroy(rock);
    }
}