using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyTankAI : MonoBehaviour
{
    public static int ActiveEnemyCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveEnemyCount()
    {
        ActiveEnemyCount = 0;
    }

    [Header("References")]
    [Tooltip("Assign the player's Transform here.")]
    public Transform playerTransform;

    [Header("Range Settings")]
    [Tooltip("Distance at which a patrolling tank starts chasing the player.")]
    public float followRange = 35f;

    [SerializeField, Min(0f), Tooltip("Distance beyond which a chasing tank returns to patrolling.")]
    private float lostTargetRange = 45f;

    [Tooltip("Distance within which the tank will stop chasing and start shooting.")]
    public float minDistance = 12f;

    [Header("Movement Settings")]
    [Tooltip("Speed at which the tank moves.")]
    public float tankSpeed = 15f;

    [Tooltip("Rotation speed (degrees/sec) when turning the tank body.")]
    public float tankRotationSpeed = 20f;

    [Header("Patrol Settings")]
    [SerializeField, Min(0f), Tooltip("Approximate pause between patrol destinations.")]
    private float patrolWaitTime = 0.75f;
    [SerializeField, Min(0.1f), Tooltip("How close the agent gets before considering a patrol point reached.")]
    private float patrolStoppingDistance = 1.5f;
    [SerializeField, Min(0f), Tooltip("Rejects random destinations with a shorter NavMesh route.")]
    private float minimumPatrolPathLength = 18f;
    [SerializeField, Min(0.05f), Tooltip("Seconds between chase destination updates.")]
    private float chaseRepathInterval = 0.25f;

    [Header("Turret Settings")]
    [Tooltip("The turret (child Transform) that should track the player.")]
    public Transform turretTransform;

    [Tooltip("How quickly the turret “lags” behind when turning (higher = snappier).")]
    public float turretLagSpeed = 6f;

    private Vector3 finalTurretLookDir;

    [Header("Projectile Settings")]
    [Tooltip("Drag your Rigidbody-based projectile prefab here.")]
    public Rigidbody projectilePrefab;

    [Tooltip("Where the projectile will be spawned from (e.g. turret barrel tip).")]
    public Transform throwPoint;

    public float launchForce = 15f;

    [Header("Fire Rate")]
    [Tooltip("Minimum time (in seconds) between consecutive shots.")]
    public float timeBetweenShots = 0.5f;

    private float nextTimeToFire;
    private float nextChaseRepathTime;
    private float nextPatrolTime;
    private static int cachedNavMeshSceneHandle = -1;
    private static Vector3[] navMeshVertices;
    private static int[] navMeshTriangles;
    private static float[] cumulativeTriangleAreas;
    private static float totalNavMeshArea;
    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    private NavMeshPath patrolPath;
    private bool isChasingPlayer;
    private bool hasPatrolDestination;
    private bool reportedPatrolFailure;

    private void Start()
    {
        patrolPath = new NavMeshPath();

        TankController player = FindFirstObjectByType<TankController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("EnemyTankAI requires a NavMeshAgent component.", this);
            enabled = false;
            return;
        }

        navMeshAgent.enabled = false;
        float sampleDistance = Mathf.Max(navMeshAgent.radius * 2f, 5f);
        if (!NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit spawnPosition,
                sampleDistance,
                navMeshAgent.areaMask))
        {
            Debug.LogError(
                "EnemyTankAI found no NavMesh near its spawn point. Bake a NavMesh for this scene before playing.",
                this);
            enabled = false;
            return;
        }

        transform.position = spawnPosition.position;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        navMeshAgent.enabled = true;
        if (!navMeshAgent.Warp(spawnPosition.position))
        {
            Debug.LogError("EnemyTankAI could not place its NavMeshAgent on the NavMesh.", this);
            navMeshAgent.enabled = false;
            enabled = false;
            return;
        }

        navMeshAgent.updateRotation = false;
        navMeshAgent.speed = tankSpeed;
        navMeshAgent.acceleration = Mathf.Max(tankSpeed * 2f, 1f);
        navMeshAgent.stoppingDistance = patrolStoppingDistance;

        if (turretTransform != null)
        {
            finalTurretLookDir = turretTransform.forward;
        }

        TrySetPatrolDestination();
    }

    private void OnEnable()
    {
        ActiveEnemyCount++;
    }

    private void OnDisable()
    {
        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
    }

    private void Update()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        float distanceSquared = float.PositiveInfinity;
        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            distanceSquared = toPlayer.sqrMagnitude;
        }

        float chaseRange = isChasingPlayer ? lostTargetRange : followRange;
        if (playerTransform != null && distanceSquared <= chaseRange * chaseRange)
        {
            HandleChase(distanceSquared);
            HandleTurret();
            HandleShooting(distanceSquared);
        }
        else
        {
            HandlePatrol();
            if (playerTransform != null)
            {
                HandleTurret();
            }
            else
            {
                RotateTowardMovement();
            }
        }
    }

    private void HandleChase(float distanceSquared)
    {
        if (!isChasingPlayer)
        {
            isChasingPlayer = true;
            hasPatrolDestination = false;
            nextChaseRepathTime = 0f;
        }

        navMeshAgent.stoppingDistance = Mathf.Max(0f, minDistance);
        navMeshAgent.isStopped = distanceSquared <= minDistance * minDistance;
        RotateToward(playerTransform.position - transform.position);

        if (!navMeshAgent.isStopped && Time.time >= nextChaseRepathTime)
        {
            navMeshAgent.SetDestination(playerTransform.position);
            nextChaseRepathTime = Time.time + chaseRepathInterval;
        }
    }

    private void HandlePatrol()
    {
        if (isChasingPlayer)
        {
            isChasingPlayer = false;
            hasPatrolDestination = false;
            nextPatrolTime = Time.time + patrolWaitTime;
            navMeshAgent.ResetPath();
        }

        navMeshAgent.stoppingDistance = patrolStoppingDistance;
        navMeshAgent.isStopped = false;
        RotateTowardMovement();

        if (hasPatrolDestination &&
            !navMeshAgent.pathPending &&
            (navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete ||
             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.5f))
        {
            hasPatrolDestination = false;
            nextPatrolTime = Time.time + Random.Range(patrolWaitTime * 0.5f, patrolWaitTime * 1.5f);
        }

        if (!hasPatrolDestination && Time.time >= nextPatrolTime)
        {
            TrySetPatrolDestination();
        }
    }

    private void TrySetPatrolDestination()
    {
        if (patrolPath == null || navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        if (!EnsureNavMeshSamplingData())
        {
            return;
        }

        const int maximumAttempts = 32;
        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            Vector3 destination = GetRandomNavMeshPoint();
            if (!navMeshAgent.CalculatePath(destination, patrolPath) ||
                patrolPath.status != NavMeshPathStatus.PathComplete ||
                GetPathLength(patrolPath) < minimumPatrolPathLength ||
                !navMeshAgent.SetDestination(destination))
            {
                continue;
            }

            hasPatrolDestination = true;
            reportedPatrolFailure = false;
            return;
        }

        nextPatrolTime = Time.time + 1f;
        if (!reportedPatrolFailure)
        {
            Debug.LogWarning("EnemyTankAI could not find a reachable patrol point on the NavMesh.", this);
            reportedPatrolFailure = true;
        }
    }

    private static bool EnsureNavMeshSamplingData()
    {
        int activeSceneHandle = SceneManager.GetActiveScene().handle;
        if (cachedNavMeshSceneHandle == activeSceneHandle)
        {
            return totalNavMeshArea > 0f;
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        navMeshVertices = triangulation.vertices;
        navMeshTriangles = triangulation.indices;
        cumulativeTriangleAreas = new float[navMeshTriangles.Length / 3];
        totalNavMeshArea = 0f;

        for (int triangle = 0; triangle < cumulativeTriangleAreas.Length; triangle++)
        {
            int index = triangle * 3;
            Vector3 a = navMeshVertices[navMeshTriangles[index]];
            Vector3 b = navMeshVertices[navMeshTriangles[index + 1]];
            Vector3 c = navMeshVertices[navMeshTriangles[index + 2]];
            totalNavMeshArea += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            cumulativeTriangleAreas[triangle] = totalNavMeshArea;
        }

        cachedNavMeshSceneHandle = activeSceneHandle;
        if (totalNavMeshArea <= 0f)
        {
            Debug.LogError("EnemyTankAI cannot patrol because the active scene has no NavMesh triangles.");
            return false;
        }

        return true;
    }

    private static Vector3 GetRandomNavMeshPoint()
    {
        float areaPosition = Random.Range(0f, totalNavMeshArea);
        int low = 0;
        int high = cumulativeTriangleAreas.Length - 1;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (cumulativeTriangleAreas[middle] <= areaPosition)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        int triangle = low;
        int index = triangle * 3;
        Vector3 a = navMeshVertices[navMeshTriangles[index]];
        Vector3 b = navMeshVertices[navMeshTriangles[index + 1]];
        Vector3 c = navMeshVertices[navMeshTriangles[index + 2]];

        float firstWeight = Mathf.Sqrt(Random.value);
        float secondWeight = Random.value;
        return (1f - firstWeight) * a +
               firstWeight * (1f - secondWeight) * b +
               firstWeight * secondWeight * c;
    }

    private static float GetPathLength(NavMeshPath path)
    {
        Vector3[] corners = path.corners;
        float length = 0f;
        for (int i = 1; i < corners.Length; i++)
        {
            length += Vector3.Distance(corners[i - 1], corners[i]);
        }

        return length;
    }

    private void RotateTowardMovement()
    {
        RotateToward(navMeshAgent.desiredVelocity);
    }

    private void RotateToward(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            tankRotationSpeed * Time.deltaTime);
    }

    private void HandleTurret()
    {
        if (turretTransform == null || playerTransform == null)
        {
            return;
        }

        Vector3 turretDirection = playerTransform.position - turretTransform.position;
        turretDirection.y = 0f;
        if (turretDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        finalTurretLookDir = Vector3.Lerp(
            finalTurretLookDir,
            turretDirection.normalized,
            Time.deltaTime * turretLagSpeed);
        turretTransform.rotation = Quaternion.LookRotation(finalTurretLookDir);
    }

    private void HandleShooting(float distanceSquared)
    {
        if (projectilePrefab == null || throwPoint == null)
        {
            return;
        }

        if (distanceSquared <= minDistance * minDistance &&
            distanceSquared <= followRange * followRange &&
            Time.time >= nextTimeToFire)
        {
            ThrowProjectile1();
            nextTimeToFire = Time.time + timeBetweenShots;
        }
    }

    private void ThrowProjectile1()
    {
        ProjectilePool.Spawn(
            projectilePrefab,
            throwPoint.position,
            throwPoint.rotation,
            throwPoint.forward * launchForce);
        GameAudioManager.PlayTankShot();
    }

    private void OnDrawGizmosSelected()
    {
        Color originalColor = Gizmos.color;
        Vector3 center = transform.position + Vector3.up * 0.1f;
        DrawRangeGizmo(center, minDistance, Color.red);
        DrawRangeGizmo(center, followRange, Color.yellow);
        Gizmos.color = originalColor;
    }

    private static void DrawRangeGizmo(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        const int segments = 48;
        float angleStep = Mathf.PI * 2f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
        Gizmos.color = color;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
