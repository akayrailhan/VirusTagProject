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

            if (task.IsFaulted || task.Result == null)
            {
                Debug.LogWarning("[LobbyJoinWaiter] Failed to get lobby or lobby closed.");
                Destroy(gameObject);
                yield break;
            }

            Lobby lobby = task.Result;
            if (lobby.Data != null && lobby.Data.ContainsKey("GameStarted") && lobby.Data["GameStarted"].Value == "1")
            {
                // Host started game — now join relay and exit
                Debug.Log("[LobbyJoinWaiter] GameStarted flag detected, joining relay...");
                var joinTask = RelayManager.JoinRelay(joinCode);
                while (!joinTask.IsCompleted)
                    yield return null;

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
