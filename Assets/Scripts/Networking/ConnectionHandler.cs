using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ConnectionHandler : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            // 1. Bağlantı onayı mekanizmasını devreye sok (Quest 6)
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

            // 2. Biri oyundan düşerse ne olacağını dinle (Quest 7)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Gelen veri var mı? (İsim vs.)
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        Debug.Log($"[ConnectionHandler] Bağlanmak isteyenin verisi: {payload}");

        // Herkesi kabul et (Onay)
        response.Approved = true;

        // Oyuncu nesnesini oluştur (Player Prefab)
        response.CreatePlayerObject = true;

        // Başlangıç pozisyonu (İstersen Quest 11 için burayı rastgele yapabilirsin)
        response.Position = Vector3.zero;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // Eğer sunucu kapandıysa ve biz client isek, menüye dönelim (Quest 7)
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("Sunucu bağlantısı koptu, menüye dönülüyor...");
            SceneManager.LoadScene("LobbyMenu");
        }
    }

    private void OnDestroy()
    {
        // Sahne kapanırken eventleri temizle
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }
}