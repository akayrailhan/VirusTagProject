using UnityEngine;
using Unity.Services.Lobbies;
using System.Threading.Tasks;

public class LobbyHeartbeatHandler : MonoBehaviour
{
    private static string _currentLobbyId;
    private float _heartbeatTimer;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public static void SetLobbyId(string lobbyId)
    {
        _currentLobbyId = lobbyId;
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(_currentLobbyId)) return;

        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer <= 0f)
        {
            _heartbeatTimer = 15f; // 15 saniyede bir
            SendHeartbeat();
        }
    }

    private async void SendHeartbeat()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobbyId);
            Debug.Log("[Heartbeat] Sent ping.");
        }
        catch (System.Exception)
        {
            Debug.LogWarning("[Heartbeat] Failed to send ping (Lobby might be closed).");
            _currentLobbyId = null;
        }
    }
}