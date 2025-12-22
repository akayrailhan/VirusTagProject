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
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.StartHost();
        Debug.Log("[Relay] Host started on NetworkManager.");
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
                return NetworkManager.Singleton.StartClient();
            }

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning($"[Relay] Join Attempt Failed (Code might not be active yet): {e.Message}");
            return false;
        }
    }
}