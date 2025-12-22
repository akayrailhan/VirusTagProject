using UnityEngine;
using System.Collections;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using System.Threading.Tasks;

public class LobbyJoinWaiter : MonoBehaviour
{
    private string _lobbyId;
    private string _joinCode;
    private bool _isProcessing = false;

    public static void StartWaiting(string lobbyId, string joinCode)
    {
        // Varsa eski waiter'ı temizle
        var oldWaiter = GameObject.Find("LobbyJoinWaiter");
        if (oldWaiter != null) Destroy(oldWaiter);

        GameObject go = new GameObject("LobbyJoinWaiter");
        DontDestroyOnLoad(go);
        var waiter = go.AddComponent<LobbyJoinWaiter>();
        waiter._lobbyId = lobbyId;
        waiter._joinCode = joinCode;
        waiter.StartCoroutine(waiter.PollLobbyAndJoin());
    }

    private IEnumerator PollLobbyAndJoin()
    {
        Debug.Log("[LobbyJoinWaiter] Sorgulama başladı...");
        
        while (true)
        {
            if (_isProcessing) { yield return new WaitForSeconds(1f); continue; }

            Task<Lobby> task = null;
            try {
                task = LobbyService.Instance.GetLobbyAsync(_lobbyId);
            } catch (System.Exception e) {
                Debug.LogWarning($"[LobbyJoinWaiter] Lobi çekilemedi: {e.Message}");
            }

            if (task != null)
            {
                while (!task.IsCompleted) yield return null;

                if (task.IsFaulted)
                {
                    Debug.LogWarning("[LobbyJoinWaiter] Lobi verisi alınırken hata oluştu, tekrar deneniyor...");
                }
                else
                {
                    Lobby lobby = task.Result;
                    // GameStarted kontrolü
                    if (lobby != null && lobby.Data != null && lobby.Data.ContainsKey("GameStarted") && lobby.Data["GameStarted"].Value == "1")
                    {
                        _isProcessing = true;
                        yield return StartCoroutine(JoinRelayWithRetry());
                        yield break; // Bağlantı denemesi bittiğinde coroutine'i sonlandır
                    }
                }
            }

            yield return new WaitForSeconds(2f); // Her 2 saniyede bir kontrol et
        }
    }

    private IEnumerator JoinRelayWithRetry()
    {
        int attempts = 0;
        const int maxAttempts = 8;
        bool joined = false;

        while (attempts < maxAttempts && !joined)
        {
            attempts++;
            float waitTime = 1.5f * attempts;
            Debug.Log($"[LobbyJoinWaiter] Relay Denemesi {attempts}/{maxAttempts}...");

            var joinTask = RelayManager.JoinRelay(_joinCode);
            while (!joinTask.IsCompleted) yield return null;

            if (joinTask.Result)
            {
                Debug.Log("[LobbyJoinWaiter] BAŞARILI! Relay'e bağlanıldı.");
                joined = true;
            }
            else
            {
                Debug.LogWarning($"[LobbyJoinWaiter] Deneme {attempts} başarısız. {waitTime}s sonra tekrar...");
                yield return new WaitForSeconds(waitTime);
            }
        }

        if (!joined) Debug.LogError("[LobbyJoinWaiter] Oyuna girilemedi, tüm denemeler başarısız!");
        
        // Görev bitti, objeyi imha et
        Destroy(gameObject);
    }
}