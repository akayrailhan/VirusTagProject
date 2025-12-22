using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;

// Quest 2 & 3: Lobi oluşturma, listeleme ve katılma işlemlerini yönetir
public static class LobbyManager
{
    // track last time we logged a TooManyRequests message to avoid spamming console
    private static float _lastTooManyRequestsLogTime = -9999f;
    // Eğer host bir lobi oluşturduysa, ID'yi burada saklıyoruz
    public static string CurrentLobbyId { get; private set; }

    // Bu istemci yerel olarak lobi sahibi mi?
    public static bool IsHostLocal { get; private set; } = false;

    // Lobi Oluşturma (Host Beacon Protocol)
    public static async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string relayJoinCode)
    {
        try
        {
            Debug.Log($"[Lobby] Creating lobby '{lobbyName}'...");
            // Log local authenticated player id (if available)
            if (Unity.Services.Authentication.AuthenticationService.Instance != null && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[Lobby] Local PlayerId (creating host): {Unity.Services.Authentication.AuthenticationService.Instance.PlayerId}");
            }

            // Quest 3: Host Beacon - Relay kodunu lobi verisine (Data) gizle
            // Include host player's display name in the lobby's player entry
            var playerData = new Dictionary<string, PlayerDataObject>();
            string myName = ApplicationController.Instance != null ? ApplicationController.Instance.GetPlayerName() : "Player";
            playerData["PlayerName"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myName);
            // default ready state = not ready
            playerData["Ready"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0");

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false, // ÖNEMLİ: Lobinin herkes tarafından görünmesini sağlar
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                },
                Player = new Player { Data = playerData }
            };

            // DÜZELTME: Unity.Services.Lobbies.Lobbies.Instance kullanarak tam yol belirtiyoruz
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"[Lobby] Created lobby: {lobby.Name} | ID: {lobby.Id} | Code: {lobby.LobbyCode}");

            // Heartbeat başlat (Lobi ölmesin diye)
            LobbyHeartbeatHandler.SetLobbyId(lobby.Id);

            // Kaydet: host bu lobby'nin sahibi
            CurrentLobbyId = lobby.Id;
            IsHostLocal = true;

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
            // Attach our player name as player data when joining
            var playerData = new Dictionary<string, PlayerDataObject>();
            string myName = ApplicationController.Instance != null ? ApplicationController.Instance.GetPlayerName() : "Player";
            playerData["PlayerName"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, myName);
            playerData["Ready"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0");

            var options = new JoinLobbyByIdOptions
            {
                Player = new Player { Data = playerData }
            };

            var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

            // Log local authenticated player id that attempted to join
            if (Unity.Services.Authentication.AuthenticationService.Instance != null && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[Lobby] Local PlayerId (joining): {Unity.Services.Authentication.AuthenticationService.Instance.PlayerId} Name={myName}");
            }

            // Save current lobby id for this client
            if (lobby != null)
            {
                CurrentLobbyId = lobby.Id;
                IsHostLocal = false;
            }

            // Debug: print returned lobby data keys/values to help diagnose Join issues
            if (lobby != null)
            {
                Debug.Log($"[LobbyManager] JoinLobby returned lobby ID={lobby.Id} Name={lobby.Name} Players={lobby.Players?.Count}");
                if (lobby.Data != null)
                {
                    foreach (var kv in lobby.Data)
                    {
                        Debug.Log($"[LobbyManager] Lobby.Data[{kv.Key}] = {kv.Value?.Value}");
                    }
                }
                else
                {
                    Debug.Log("[LobbyManager] Joined lobby has no Data dictionary.");
                }
            }

            return lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] Join failed: {e.Message}");
            return null;
        }
    }

    // Get lobby by id (useful for clients polling state)
    public static async Task<Lobby> GetLobbyById(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return null;

        const int maxAttempts = 4;
        int attempt = 0;
        int baseDelayMs = 500; // initial backoff

        while (attempt < maxAttempts)
        {
            attempt++;
            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                return lobby;
            }
            catch (LobbyServiceException e)
            {
                string msg = e.Message ?? string.Empty;
                // If rate-limited, backoff and retry with exponential delay + jitter
                if (msg.Contains("Too Many Requests") || msg.Contains("429"))
                {
                    float now = UnityEngine.Time.realtimeSinceStartup;
                    if (now - _lastTooManyRequestsLogTime > 5f)
                    {
                        Debug.LogWarning($"[Lobby] GetLobby failed: Too Many Requests (rate limited). Attempt {attempt}/{maxAttempts}. Backing off.");
                        _lastTooManyRequestsLogTime = now;
                    }

                    // exponential backoff with jitter
                    int delay = baseDelayMs * (1 << (attempt - 1));
                    int jitter = UnityEngine.Random.Range(0, 200);
                    await System.Threading.Tasks.Task.Delay(delay + jitter);
                    continue; // retry
                }

                // Non-rate-limit errors: log and return null
                Debug.LogError($"[Lobby] GetLobby failed: {e.Message}");
                return null;
            }
        }

        Debug.LogWarning("[Lobby] GetLobby: exceeded retry attempts due to rate limiting.");
        return null;
    }

    // Host, oyunu başlattığında lobinin verisini güncelle (clients buna bakacak)
    public static async Task<bool> SetGameStarted(string lobbyId)
    {
        try
        {
            var data = new Dictionary<string, DataObject>
            {
                { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "1") }
            };

            await LobbyService.Instance.UpdateLobbyAsync(lobbyId, new UpdateLobbyOptions { Data = data });
            Debug.Log("[Lobby] Marked GameStarted on lobby.");
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] SetGameStarted failed: {e.Message}");
            return false;
        }
    }

    // Set this player's Ready state (true = ready, false = not ready)
    public static async Task<bool> SetPlayerReady(string lobbyId, bool ready)
    {
        if (string.IsNullOrEmpty(lobbyId)) return false;

        try
        {
            string playerId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogWarning("[Lobby] Cannot set ready: not authenticated");
                return false;
            }

            var update = new Dictionary<string, PlayerDataObject>
            {
                { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ready ? "1" : "0") }
            };

            await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, new UpdatePlayerOptions { Data = update });
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] SetPlayerReady failed: {e.Message}");
            return false;
        }
    }

    // Returns true if every player in the lobby has Ready == "1"
    public static async Task<bool> AreAllPlayersReady(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return false;

        Lobby lobby = await GetLobbyById(lobbyId);
        if (lobby == null || lobby.Players == null) return false;

        foreach (var p in lobby.Players)
        {
            if (p.Data == null || !p.Data.ContainsKey("Ready") || p.Data["Ready"].Value != "1")
            {
                return false;
            }
        }

        return true;
    }
    
    // Verilen lobbyId için lobiyi getirir ve içindeki oyuncu isimlerini döndürür
    public static async Task<List<string>> GetPlayerNamesById(string lobbyId)
    {
        var names = new List<string>();
        if (string.IsNullOrEmpty(lobbyId)) return names;

        Lobby lobby = await GetLobbyById(lobbyId);
        if (lobby == null || lobby.Players == null) return names;

        foreach (var p in lobby.Players)
        {
            // Prefer Player.Data["PlayerName"] if present (we set this on join/create),
            // otherwise fall back to the player's Id.
            if (p.Data != null && p.Data.ContainsKey("PlayerName") && !string.IsNullOrEmpty(p.Data["PlayerName"].Value))
            {
                names.Add(p.Data["PlayerName"].Value);
            }
            else
            {
                names.Add(p.Id);
            }
        }

        return names;
    }

    // Simple DTO for player info including ready state
    public class PlayerInfo
    {
        public string Id;
        public string Name;
        public bool Ready;
    }

    // Get detailed player info (name + ready) for a lobby
    public static async Task<List<PlayerInfo>> GetPlayersInfoById(string lobbyId)
    {
        var result = new List<PlayerInfo>();
        if (string.IsNullOrEmpty(lobbyId)) return result;

        Lobby lobby = await GetLobbyById(lobbyId);
        if (lobby == null || lobby.Players == null) return result;

        foreach (var p in lobby.Players)
        {
            var info = new PlayerInfo { Id = p.Id, Name = p.Id, Ready = false };
            if (p.Data != null)
            {
                if (p.Data.ContainsKey("PlayerName") && !string.IsNullOrEmpty(p.Data["PlayerName"].Value))
                    info.Name = p.Data["PlayerName"].Value;

                if (p.Data.ContainsKey("Ready") && p.Data["Ready"].Value == "1")
                    info.Ready = true;
            }

            result.Add(info);
        }

        return result;
    }
    
}

// Lobiyi canlı tutmak için Heartbeat (Kalp Atışı) sistemi
