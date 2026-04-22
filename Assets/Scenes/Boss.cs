using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boss : NPC
{
    public override float Health { get; set; } = 100;
    public override float Damage { get; set; } = 15;
    public override float AttackCooldown { get; set; } = 1.5f;
    public override float AttackSpeed { get; set; } = 1;
    public override float AttackRange { get; set; } = 5f;
    private float AttackRadius = 30f;
    public Transform PlayerPosition;

    public GameObject FistPreview;
    private readonly Collider[] _colliders = new Collider[5];
    [SerializeField] private LayerMask _interactableMask;

    private void Start()
    {
        StartCoroutine(WaitForNextAttack());
    }

    public override void Attack()
    {
        Debug.Log("ataco");
        FistDownAttack();
    }

    public override void Die()
    {
      
    }

    public IEnumerator WaitForNextAttack()
    {
        
        
        //if (Health <= 0)
        //{
        //    Die();
        //    yield return null;
        //}
        
        float coolDown = Random.Range(5, 7);
        //Debug.Log(coolDown);
        yield return new WaitForSeconds(coolDown);
        FistPreview.SetActive(false);
        Attack();

    }
    public void FistDownAttack()
    {

        float cx = this.transform.position.x;
        float cy = this.transform.position.z;
        float vx = PlayerPosition.position.x - cx;
        float vy = PlayerPosition.position.z - cy;
        Vector3 dir = new Vector3(vx, 0 ,vy);
        dir.Normalize();
        dir = dir * AttackRadius;

        FistPreview.SetActive(true);
        FistPreview.transform.position = new Vector3(this.transform.position.x, 0, this.transform.position.z);
        
        FistPreview.transform.position += dir;
        StartCoroutine(FistDownAttackDamage(dir));

    }
    public IEnumerator FistDownAttackDamage(Vector3 position)
    {
        yield return new WaitForSeconds(2.5f);
        Physics.OverlapSphereNonAlloc(position, 8, _colliders);
        int hits = Physics.OverlapSphereNonAlloc(position, 8, _colliders);

        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                var collider = _colliders[i];
                if (collider != null && collider.gameObject.layer == 6)
                {
                    Debug.Log("te pego");
                }
            }
        }
        StartCoroutine(WaitForNextAttack());
    }

   
}
