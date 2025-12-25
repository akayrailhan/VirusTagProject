using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Bu noktayı doluluk kontrolünde kullanılacak yarıçap (checkRadius ile max alınır).")]
    public float radius = 0.5f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Sahnedeyken görünür olsun diye
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
