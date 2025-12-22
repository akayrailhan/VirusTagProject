using UnityEngine;
using System.Collections;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;

public class LobbyJoinWaiter : MonoBehaviour
{
    private string _lobbyId;
    private string _joinCode;

    public static void StartWaiting(string lobbyId, string joinCode)
    {
        if (GameObject.Find("LobbyJoinWaiter")) return;

        GameObject go = new GameObject("LobbyJoinWaiter");
        DontDestroyOnLoad(go);
        var waiter = go.AddComponent<LobbyJoinWaiter>();
        waiter._lobbyId = lobbyId;
        waiter._joinCode = joinCode;
        waiter.StartCoroutine(waiter.PollLobbyAndJoin());
    }

    private IEnumerator PollLobbyAndJoin()
    {
        while (true)
        {
            var task = LobbyService.Instance.GetLobbyAsync(_lobbyId);
            yield return new WaitUntil(() => task.IsCompleted);

            if (!task.IsFaulted && task.Result != null)
            {
                Lobby lobby = task.Result;
                // Bayrak "1" olduğunda bağlantıyı başlat
                if (lobby.Data?.ContainsKey("GameStarted") == true && lobby.Data["GameStarted"].Value == "1")
                {
                    yield return StartCoroutine(JoinRelayWithRetry());
                    yield break;
                }
            }
            yield return new WaitForSeconds(2.5f);
        }
    }

    private IEnumerator JoinRelayWithRetry()
    {
        int attempts = 0;
        const int maxAttempts = 10; // Daha fazla deneme hakkı
        bool joined = false;

        while (attempts < maxAttempts && !joined)
        {
            attempts++;
            var joinTask = RelayManager.JoinRelay(_joinCode);
            yield return new WaitUntil(() => joinTask.IsCompleted);

            if (joinTask.Result)
            {
                Debug.Log("[Waiter] Relay'e BAŞARIYLA BAĞLANILDI.");
                joined = true;
            }
            else
            {
                float wait = 2.0f * attempts; // Gecikmeyi biraz daha artırdık
                Debug.LogWarning($"[Waiter] Deneme {attempts} başarısız. {wait}s bekleniyor...");
                yield return new WaitForSeconds(wait);
            }
        }
        
        if (!joined) Debug.LogError("[Waiter] Oyuna girilemedi!");
        Destroy(gameObject);
    }
}