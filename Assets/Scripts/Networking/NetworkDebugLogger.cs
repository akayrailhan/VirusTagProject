using UnityEngine;
using Unity.Netcode;

// Attach this to a GameObject (e.g., NetworkManager) to log Netcode connection events
public class NetworkDebugLogger : MonoBehaviour
{
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkDebug] Client connected: {clientId}. IsServer={NetworkManager.Singleton.IsServer} IsClient={NetworkManager.Singleton.IsClient} IsHost={NetworkManager.Singleton.IsHost}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkDebug] Client disconnected: {clientId}");
    }

    private void Update()
    {
        // occasional heartbeat so we can see if this object is alive
        if (Time.frameCount % 300 == 0)
        {
            if (NetworkManager.Singleton != null)
                Debug.Log($"[NetworkDebug] NM state: IsServer={NetworkManager.Singleton.IsServer} IsClient={NetworkManager.Singleton.IsClient} IsHost={NetworkManager.Singleton.IsHost}");
        }
    }
}
