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
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[Relay] Cannot join relay: NetworkManager.Singleton is null.");
                return false;
            }

            if (transport == null)
            {
                Debug.LogError("[Relay] Cannot join relay: UnityTransport component not found on NetworkManager.");
                return false;
            }

            try
            {
                transport.SetRelayServerData(relayServerData);
                // JoinAllocation provides server endpoint info; log host/port from allocation to avoid relying on RelayServerData fields
                string host = "(unknown)";
                int port = 0;
                try
                {
                    if (allocation != null && allocation.ServerEndpoints != null && allocation.ServerEndpoints.Count > 0)
                    {
                        host = allocation.ServerEndpoints[0].Host;
                        port = allocation.ServerEndpoints[0].Port;
                    }
                }
                catch { }
                Debug.Log($"[Relay] Relay server data set for client. Host={host} Port={port}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Relay] Failed to set relay server data: {ex.Message}");
                return false;
            }

            // 3. Client Başlat
            try
            {
                NetworkManager.Singleton.StartClient();
                Debug.Log("[Relay] NetworkManager.StartClient() called.");

                // quick check if client state indicates starting (best-effort)
                if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("[Relay] NetworkManager reports IsClient = true (client started). Returning success.");
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Relay] StartClient threw exception: {ex.Message}");
                return false;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Join Failed: {e.Message}");
            return false;
        }
    }
}