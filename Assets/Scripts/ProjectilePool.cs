using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProjectilePool : MonoBehaviour
{
    [Serializable]
    private sealed class PoolConfiguration
    {
        [SerializeField] public Rigidbody projectilePrefab;
        [Min(1)] public int projectileCount = 32;
        [Min(1)] public int impactEffectCount = 16;
    }

    [SerializeField] private List<PoolConfiguration> pools = new List<PoolConfiguration>();

    private static ProjectilePool instance;

    private readonly Dictionary<Rigidbody, Queue<Rigidbody>> projectilePools =
        new Dictionary<Rigidbody, Queue<Rigidbody>>();
    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> impactPools =
        new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        PrewarmPools();
    }

    public void PrewarmPools()
    {
        foreach (PoolConfiguration configuration in pools)
        {
            if (configuration == null)
            {
                Debug.LogError("Projectile Pool contains an empty configuration entry.", this);
                continue;
            }

            if (configuration.projectilePrefab == null)
            {
                Debug.LogError("Projectile Pool has an unassigned projectile prefab.", this);
                continue;
            }

            if (!configuration.projectilePrefab.TryGetComponent(
                    out Projectile projectileComponent))
            {
                Debug.LogError(
                    $"Projectile prefab '{configuration.projectilePrefab.name}' needs a Projectile component.",
                    configuration.projectilePrefab);
                continue;
            }

            if (!projectilePools.ContainsKey(configuration.projectilePrefab))
            {
                PrewarmProjectiles(configuration);
            }

            if (projectileComponent.impactFX != null &&
                !impactPools.ContainsKey(projectileComponent.impactFX))
            {
                PrewarmImpactEffects(
                    projectileComponent.impactFX,
                    Mathf.Max(1, configuration.impactEffectCount));
            }
        }
    }

    public static void Spawn(
        Rigidbody prefab,
        Vector3 position,
        Quaternion rotation,
        Vector3 velocity)
    {
        if (instance == null)
        {
            Debug.LogError("No ProjectilePool is active. Add one to the scene before gameplay.");
            return;
        }

        if (prefab == null || !instance.projectilePools.TryGetValue(prefab, out Queue<Rigidbody> pool))
        {
            Debug.LogError("Projectile prefab is not configured in the scene's ProjectilePool.", prefab);
            return;
        }

        Rigidbody projectile = pool.Count > 0
            ? pool.Dequeue()
            : instance.CreateProjectile(prefab);

        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.gameObject.SetActive(true);
        projectile.linearVelocity = velocity;
    }

    public static void PlayImpact(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return;
        }

        if (instance == null || !instance.impactPools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
        {
            Debug.LogError("Impact effect is not prewarmed in the active ProjectilePool.", prefab);
            return;
        }

        ParticleSystem effect = pool.Count > 0
            ? pool.Dequeue()
            : instance.CreateImpactEffect(prefab);

        effect.transform.SetPositionAndRotation(position, Quaternion.identity);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.gameObject.SetActive(true);
        effect.Play(true);
        instance.StartCoroutine(instance.ReturnImpactWhenFinished(effect, prefab));
    }

    private void PrewarmProjectiles(PoolConfiguration configuration)
    {
        Rigidbody prefab = configuration.projectilePrefab;
        int count = Mathf.Max(1, configuration.projectileCount);
        Queue<Rigidbody> pool = new Queue<Rigidbody>(count);
        projectilePools.Add(prefab, pool);

        for (int i = 0; i < count; i++)
        {
            pool.Enqueue(CreateProjectile(prefab));
        }
    }

    private Rigidbody CreateProjectile(Rigidbody prefab)
    {
        Rigidbody projectile = Instantiate(prefab, transform);
        projectile.gameObject.SetActive(false);
        projectile.GetComponent<Projectile>().SetReleaseHandler(
            () => ReleaseProjectile(prefab, projectile));
        return projectile;
    }

    private void PrewarmImpactEffects(ParticleSystem prefab, int count)
    {
        Queue<ParticleSystem> pool = new Queue<ParticleSystem>(count);
        impactPools.Add(prefab, pool);

        for (int i = 0; i < count; i++)
        {
            pool.Enqueue(CreateImpactEffect(prefab));
        }
    }

    private ParticleSystem CreateImpactEffect(ParticleSystem prefab)
    {
        ParticleSystem effect = Instantiate(prefab, transform);
        effect.gameObject.SetActive(false);
        return effect;
    }

    private void ReleaseProjectile(Rigidbody prefab, Rigidbody projectile)
    {
        projectile.linearVelocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;
        projectile.gameObject.SetActive(false);
        projectilePools[prefab].Enqueue(projectile);
    }

    private IEnumerator ReturnImpactWhenFinished(ParticleSystem effect, ParticleSystem prefab)
    {
        while (effect != null && effect.IsAlive(true))
        {
            yield return null;
        }

        if (effect != null)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.gameObject.SetActive(false);
            impactPools[prefab].Enqueue(effect);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
