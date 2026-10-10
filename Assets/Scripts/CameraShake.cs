using Unity.Cinemachine;
using UnityEngine;

public sealed class CameraShake : MonoBehaviour
{
    private static CameraShake instance;

    [SerializeField] private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        instance = this;

        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        if (impulseSource == null)
        {
            Debug.LogError("CameraShake requires a CinemachineImpulseSource.", this);
        }
    }

    public static void GenerateImpulse(Vector3 position, float force)
    {
        if (instance == null || instance.impulseSource == null)
        {
            Debug.LogError("Camera shake is not configured in the active scene.");
            return;
        }

        instance.impulseSource.GenerateImpulseAtPositionWithVelocity(
            position,
            Vector3.up * force);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
