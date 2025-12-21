using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using UnityEngine;

// Quest 1: Warp Gate Handshake - Relay üzerinden güvenli bağlantı (DTLS)
public static class RelayManager
{
    // Host Olmak İçin (Oda Kur)
    public static async Task<string> CreateRelay(int maxConnections)
    {
        try
        {
            // 1. Yer Ayırt (Allocation)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // 2. Join Code Al
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Relay] Created! Join Code: {joinCode}");

            // 3. NetworkManager'ı Ayarla (DTLS = Güvenli Bağlantı)
            // Multiplayer Services SDK Update: AllocationUtils kullanıyoruz
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            // NOT: Host'u hemen başlatmıyoruz. Host, Lobby sahibinin "Start Game" düğmesine
            // basmasından sonra başlatılacak. Burada sadece transport verisini ayarlıyoruz.
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Create Failed: {e.Message}");
            return null;
        }
    }

    // Host'u gerçekten başlatmak için çağrılır (Start Game butonunda)
    public static void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Relay] Cannot start host: NetworkManager.Singleton is null");
            return;
        }

        NetworkManager.Singleton.StartHost();
    }

    // Client Olmak İçin (Odaya Katıl)
    public static async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            // 1. Kodu Kullanarak Bağlan
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 2. Transport Ayarla
            // Multiplayer Services SDK Update: AllocationUtils kullanıyoruz
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            // 3. Client Başlat
            NetworkManager.Singleton.StartClient();
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Join Failed: {e.Message}");
            return false;
        }
    }
}