using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private static Camera targetCamera;

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 directionToCamera = targetCamera.transform.position - transform.position;
        if (directionToCamera.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera, targetCamera.transform.up);
        }
    }
}
