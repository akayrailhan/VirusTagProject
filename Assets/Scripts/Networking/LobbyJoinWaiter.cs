using UnityEngine;
using System.Collections;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

// Polls lobby until host marks GameStarted, then calls RelayManager.JoinRelay
public class LobbyJoinWaiter : MonoBehaviour
{
    public static void StartWaiting(string lobbyId, string joinCode)
    {
        GameObject go = new GameObject("LobbyJoinWaiter");
        DontDestroyOnLoad(go);
        var waiter = go.AddComponent<LobbyJoinWaiter>();
        waiter.StartCoroutine(waiter.PollLobbyAndJoin(lobbyId, joinCode));
    }

    private IEnumerator PollLobbyAndJoin(string lobbyId, string joinCode)
    {
        // Poll every 1 second
        while (true)
        {
            var task = Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(lobbyId);
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
            {
                var ex = task.Exception != null ? task.Exception.GetBaseException() : null;
                string em = ex != null ? ex.Message : "(no exception)";

                // If we were rate-limited, back off and continue polling instead of aborting
                if (em.Contains("Too Many Requests") || em.Contains("429"))
                {
                    Debug.LogWarning("[LobbyJoinWaiter] Rate limited while fetching lobby; backing off 5s and retrying.");
                    float backoff = 5f;
                    float t = 0f;
                    while (t < backoff)
                    {
                        t += Time.deltaTime;
                        yield return null;
                    }
                    continue; // retry
                }

                Debug.LogWarning($"[LobbyJoinWaiter] Failed to get lobby or lobby closed: {em}");
                Destroy(gameObject);
                yield break;
            }

            Lobby lobby = task.Result;
            if (lobby == null)
            {
                Debug.LogWarning("[LobbyJoinWaiter] Lobby was not found (null). Destroying waiter.");
                Destroy(gameObject);
                yield break;
            }
            if (lobby.Data != null && lobby.Data.ContainsKey("GameStarted") && lobby.Data["GameStarted"].Value == "1")
            {
                // Host started game — now join relay and exit
                Debug.Log("[LobbyJoinWaiter] GameStarted flag detected, joining relay...");

                // Try joining relay with a few retries/backoff in case of transient failures
                int attempts = 0;
                const int maxAttempts = 3;
                bool joined = false;
                while (attempts < maxAttempts && !joined)
                {
                    attempts++;
                    var joinTask = RelayManager.JoinRelay(joinCode);
                    while (!joinTask.IsCompleted) yield return null;

                    if (joinTask.IsFaulted)
                    {
                        Debug.LogError($"[LobbyJoinWaiter] JoinRelay task faulted on attempt {attempts}.");
                    }
                    else if (!joinTask.Result)
                    {
                        Debug.LogWarning($"[LobbyJoinWaiter] JoinRelay reported failure on attempt {attempts}.");
                    }
                    else
                    {
                        Debug.Log("[LobbyJoinWaiter] Successfully started client via Relay.");
                        joined = true;
                        break;
                    }

                    // backoff before retrying
                    float backoff = 2f * attempts; // 2s, 4s, ...
                    float t = 0f;
                    Debug.Log($"[LobbyJoinWaiter] Backing off {backoff}s before next join attempt.");
                    while (t < backoff)
                    {
                        t += Time.deltaTime;
                        yield return null;
                    }
                }

                if (!joined)
                {
                    Debug.LogError("[LobbyJoinWaiter] All JoinRelay attempts failed. Giving up.");
                }

                Destroy(gameObject);
                yield break;
            }

            // wait a second then poll again
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }
}
