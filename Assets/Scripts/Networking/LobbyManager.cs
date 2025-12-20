using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Quest 2 & 3: Lobi oluşturma, listeleme ve katılma işlemlerini yönetir
public static class LobbyManager
{
    // Lobi Oluşturma (Host Beacon Protocol)
    public static async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string relayJoinCode)
    {
        try
        {
            Debug.Log($"[Lobby] Creating lobby '{lobbyName}'...");

            // Quest 3: Host Beacon - Relay kodunu lobi verisine (Data) gizle
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false, // ÖNEMLİ: Lobinin herkes tarafından görünmesini sağlar
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            };

            // DÜZELTME: Unity.Services.Lobbies.Lobbies.Instance kullanarak tam yol belirtiyoruz
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"[Lobby] Created lobby: {lobby.Name} | ID: {lobby.Id} | Code: {lobby.LobbyCode}");

            // Heartbeat başlat (Lobi ölmesin diye)
            LobbyHeartbeatHandler.SetLobbyId(lobby.Id);

            return lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] Create failed: {e.Message}");
            return null;
        }
    }

    // Açık Lobileri Listele (Observatory UI)
    public static async Task<List<Lobby>> GetLobbies()
    {
        try
        {
            Debug.Log("[Lobby] Fetching lobby list...");

            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 20, // En fazla 20 lobi getir

                // Filtreleri şimdilik kaldırıyoruz veya basitleştiriyoruz ki hata olmasın
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) // Sadece boş yeri olanlar
                },

                Order = new List<QueryOrder>
                {
                    new QueryOrder(true, QueryOrder.FieldOptions.Created) // En yeni en üstte
                }
            };

            // DÜZELTME: Unity.Services.Lobbies.Lobbies.Instance kullanarak tam yol belirtiyoruz
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            Debug.Log($"[Lobby] Found {response.Results.Count} lobbies.");
            foreach (var l in response.Results) Debug.Log($" - Found: {l.Name}");

            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] Query failed: {e.Message}");
            return new List<Lobby>();
        }
    }

    // Lobiye Katıl (Drop-In Boarding)
    public static async Task<Lobby> JoinLobby(string lobbyId)
    {
        try
        {
            Debug.Log($"[Lobby] Joining lobby {lobbyId}...");
            // DÜZELTME: Unity.Services.Lobbies.Lobbies.Instance kullanarak tam yol belirtiyoruz
            return await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] Join failed: {e.Message}");
            return null;
        }
    }
}

// Lobiyi canlı tutmak için Heartbeat (Kalp Atışı) sistemi
