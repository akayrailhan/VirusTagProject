using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private float refreshInterval = 0.5f; // yarım saniyede bir güncelle

    [Header("Game Over")]
    [SerializeField] private float gameDuration = 120f;      // ✅ 2 dakika = 120 saniye
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Win Condition")]
    [SerializeField] private int winScore = 100; // ✅ tam 100 olunca bitir

    [Header("Cleanup")]
    [SerializeField] private bool cleanupGameplayNetworkObjects = true;
    [SerializeField] private bool keepPlayerObjects = true; // false -> player'lar da silinir

    [Header("Lobby")]
    [SerializeField] private bool closeLobbyOnGameOver = true; // host ise lobby'yi siler

    private float _nextUpdateTime;
    private float _endTime;
    private bool _endingStarted;

    private void Start()
    {
        _endTime = Time.time + gameDuration;
    }

    private void Update()
    {
        // 1) Oyun bitiş akışı (2 dk)
        if (!_endingStarted && Time.time >= _endTime)
        {
            // Netcode yoksa: local geçiş
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                _endingStarted = true;
                SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
                return;
            }

            // Sadece server/host tetikler; client hiçbir şey yapmaz
            if (!NetworkManager.Singleton.IsServer)
                return;

            _endingStarted = true;
            EndGameAndGoGameOver(); // async void, kendi içinde sahne yükler
            return;
        }

        // 2) Leaderboard güncelle
        if (Time.time < _nextUpdateTime) return;
        _nextUpdateTime = Time.time + refreshInterval;

        PlayerState[] players = FindObjectsOfType<PlayerState>();
        var list = new List<PlayerData>(players.Length);

        foreach (var ps in players)
            list.Add(ps.CurrentState.Value);

        // Skora göre sırala (büyükten küçüğe)
        list.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Metni hazırla: "1. İsim : Skor"
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            string line = $"{i + 1}. {p.PlayerName.ToString()} : {p.Score}";

            // Eğer bu satırdaki oyuncu BEN isem (Local Client)
            if (p.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                // Sarı ve Kalın yap (TextMeshPro HTML tagleri ile)
                sb.AppendLine($"<color=yellow><b>{line}</b></color>");
            }
            else
            {
                // Başkasıysa normal yaz
                sb.AppendLine(line);
            }
        }

        if (leaderboardText != null)
            leaderboardText.text = sb.ToString();

        // 3) ✅ Win condition: SADECE tam 100 olunca bitir
        if (!_endingStarted && list.Count > 0 && list[0].Score == winScore)
        {
            // Netcode yoksa: local geçiş
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                _endingStarted = true;
                SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
                return;
            }

            // Sadece server/host tetikler; client hiçbir şey yapmaz
            if (!NetworkManager.Singleton.IsServer)
                return;

            _endingStarted = true;
            EndGameAndGoGameOver(); // aynı akış
            return;
        }
    }

    private async void EndGameAndGoGameOver()
    {
        try
        {
            // (opsiyonel) gameplay objelerini temizle
            if (cleanupGameplayNetworkObjects)
                CleanupGameplayNetworkObjects();

            // (opsiyonel) lobby'yi kapat/sil (host ise)
            if (closeLobbyOnGameOver)
            {
                if (LobbyManager.IsHostLocal)
                {
                    bool ok = await LobbyManager.LeaveLobby(); // Host -> DeleteLobbyAsync
                    Debug.Log($"[LeaderboardUI] CloseLobby result: {ok}");
                }
                else
                {
                    Debug.Log("[LeaderboardUI] closeLobbyOnGameOver açık ama bu instance LobbyManager'a göre host değil; lobby silme atlandı.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardUI] EndGame error: {e}");
        }

        // Herkese sync olacak şekilde GameOver sahnesine geç
        NetworkManager.Singleton.SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
    }

    private void CleanupGameplayNetworkObjects()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        var allNetworkObjects = FindObjectsOfType<NetworkObject>(true);

        foreach (var no in allNetworkObjects)
        {
            if (no == null) continue;

            // NetworkManager'ı asla silme
            if (no.GetComponent<NetworkManager>() != null) continue;

            // Player kalsın mı?
            if (keepPlayerObjects && no.IsPlayerObject) continue;

            if (no.IsSpawned) no.Despawn(true);
            else Destroy(no.gameObject);
        }
    }
}