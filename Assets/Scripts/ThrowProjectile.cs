using UnityEngine;

public class ThrowProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Drag your Rigidbody‐based projectile prefab here.")]
    public Rigidbody projectilePrefab;

    [Tooltip("Where the projectile will be spawned from.")]
    public Transform throwPoint;

    [Tooltip("Initial speed applied to the projectile.")]
    public float launchForce = 15f;

    [Header("Fire Rate")]
    [Tooltip("Minimum time (in seconds) between consecutive shots.")]
    public float timeBetweenShots = 0.5f;

    // Time at which we’re next allowed to shoot
    private float nextTimeToFire = 0f;
    private Tank_Inputs input;

    private void Awake()
    {
        input = GetComponent<Tank_Inputs>();
        if (input == null)
        {
            Debug.LogError("ProjectileThrower requires a Tank_Inputs component.", this);
        }
    }

    void Update()
    {
        if (input != null && input.FirePressedThisFrame && Time.time >= nextTimeToFire)
        {
            ThrowProjectile1();
            nextTimeToFire = Time.time + timeBetweenShots;
        }
    }

    private void ThrowProjectile1()
    {
        if (projectilePrefab == null || throwPoint == null)
        {
            Debug.LogWarning("ProjectileThrower: Assign 'projectilePrefab' and 'throwPoint' in the Inspector.");
            return;
        }

        ProjectilePool.Spawn(
            projectilePrefab,
            throwPoint.position,
            throwPoint.rotation,
            throwPoint.forward * launchForce
        );
        GameAudioManager.PlayTankShot();
        CameraShake.GenerateImpulse(throwPoint.position, 0.3f);
    }
}
