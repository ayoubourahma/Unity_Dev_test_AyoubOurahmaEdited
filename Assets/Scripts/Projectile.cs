using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public ParticleSystem impactFX;

    public static int ActiveProjectileCount { get; private set; }

    private System.Action releaseToPool;
    private bool hasBeenReleased;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveProjectileCount()
    {
        ActiveProjectileCount = 0;
    }

    internal void SetReleaseHandler(System.Action releaseHandler)
    {
        releaseToPool = releaseHandler;
    }

    private void OnEnable()
    {
        hasBeenReleased = false;
        ActiveProjectileCount++;
    }

    private void OnDisable()
    {
        ActiveProjectileCount = Mathf.Max(0, ActiveProjectileCount - 1);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProjectilePool.PlayImpact(impactFX, transform.position);
        CameraShake.GenerateImpulse(transform.position, 0.8f);

        if (collision.gameObject.CompareTag("PlayerTank"))
        {
            collision.gameObject.GetComponent<TankHealth>().takeDamage(20);
        }

        if (collision.gameObject.CompareTag("EnemyTank"))
        {
            collision.gameObject.GetComponent<EnemyTankHealth>().takeDamage(20);
        }

        if (hasBeenReleased)
        {
            return;
        }

        hasBeenReleased = true;
        if (releaseToPool != null)
        {
            releaseToPool();
        }
        else
        {
            Destroy(gameObject);
        }

    }
}
