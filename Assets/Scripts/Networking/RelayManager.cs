using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using UnityEngine;

public static class RelayManager
{
    public static async Task<string> CreateRelay(int maxConnections)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null) transport.SetRelayServerData(relayServerData);

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Create Failed: {e.Message}");
            return null;
        }
    }

    public static void StartHost()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.StartHost();
    }

    public static async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            if (string.IsNullOrEmpty(joinCode)) return false;

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) return false;

            transport.SetRelayServerData(relayServerData);

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                // --- QUEST 6 EKLEMESİ BAŞLANGIÇ ---
                // Oyuncu ismini al ve JSON/String olarak paketle
                string playerName = ApplicationController.Instance != null ? ApplicationController.Instance.GetPlayerName() : "Unknown";
                string authId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;

                // Basit bir JSON formatı yapıyoruz
                string payload = $"{{\"name\":\"{playerName}\", \"authId\":\"{authId}\"}}";
                byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

                // NetworkManager'a bu veriyi ver, bağlanırken göndersin
                NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
                // --- QUEST 6 EKLEMESİ BİTİŞ ---

                return NetworkManager.Singleton.StartClient();
            }
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning($"[Relay] Join Attempt Failed (Kod henüz aktif olmayabilir): {e.Message}");
            return false;
        }
    }
}