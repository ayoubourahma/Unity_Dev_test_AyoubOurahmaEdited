using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class CameraBoundsGizmo : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = Color.cyan;

    private void OnDrawGizmos()
    {
        if (!TryGetComponent(out BoxCollider bounds))
        {
            return;
        }

        Matrix4x4 originalMatrix = Gizmos.matrix;
        Color originalColor = Gizmos.color;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.matrix = originalMatrix;
        Gizmos.color = originalColor;
    }
}
