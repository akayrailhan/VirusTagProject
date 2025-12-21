using Unity.Netcode;
using UnityEngine;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform turretTransform; // Dönmesi gereken parça

    private void LateUpdate()
    {
        if (!IsOwner) return;

        // Mouse pozisyonunu dünya koordinatına çevir
        Vector2 aimScreenPosition = inputReader.AimPosition;
        Vector3 aimWorldPosition = Camera.main.ScreenToWorldPoint(aimScreenPosition);

        // Z eksenini sıfırla (2D olduğu için)
        aimWorldPosition.z = 0f;

        // Yönü hesapla ve döndür
        Vector3 direction = aimWorldPosition - turretTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        turretTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}