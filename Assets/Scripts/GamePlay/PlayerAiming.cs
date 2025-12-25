using Unity.Netcode;
using UnityEngine;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform turretTransform; // Dönmesi gereken parça

    private void Awake()
    {
        if (turretTransform == null)
            turretTransform = transform;

        // inputReader genelde inspector’dan veriliyor, yoksa burada otomatik bulmak riskli.
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (inputReader == null) return;

        Vector2 aimScreenPosition = inputReader.AimPosition;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 aimWorldPosition = cam.ScreenToWorldPoint(aimScreenPosition);
        aimWorldPosition.z = 0f;

        Vector3 direction = aimWorldPosition - turretTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        turretTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
