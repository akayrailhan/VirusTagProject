using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InfectionManager : NetworkBehaviour
{
    public static InfectionManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private int playersToStart = 4;
    [SerializeField] private string gameSceneName = "Game";

    [Header("Scenes")]
    [SerializeField] private string lobbyMenuSceneName = "LobbyMenu";

    [Header("Scoring")]
    [SerializeField] private float scoreTickInterval = 1f;
    [SerializeField] private int scorePerTick = 1;

    private float _nextScoreTime;

    private readonly List<PlayerState> _players = new();
    private bool _startRequested;
    private bool _matchStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // Host/client fark etmez: disconnect yakalayalım
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        if (!IsServer) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        if (!IsServer) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    // HOST çıkınca client'lar disconnect olur -> lobby'e dön
    private void OnClientDisconnect(ulong clientId)
    {
        // Biz disconnect olduysak (local client), menüye dön
        if (NetworkManager.Singleton == null) return;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Network kapanmış olabilir, o yüzden sadece normal SceneManager ile yükle
            SceneManager.LoadScene(lobbyMenuSceneName, LoadSceneMode.Single);
        }
    }

    // Host'un "çık" butonuna bağlayabileceğin fonksiyon:
    // (Host çıkınca zaten herkes düşer; host da lobby'e döner)
    public void HostLeaveToLobby()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(lobbyMenuSceneName, LoadSceneMode.Single);
    }

    public void RegisterPlayer(PlayerState ps)
    {
        if (!IsServer) return;
        if (ps == null) return;
        if (_players.Contains(ps)) return;

        _players.Add(ps);
        Debug.Log($"[InfectionManager] RegisterPlayer Owner={ps.OwnerClientId} Count={_players.Count}");

        TryStartMatch();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMatchServerRpc()
    {
        if (!IsServer) return;

        _startRequested = true;
        _matchStarted = false;

        _players.Clear();

        Debug.Log("[InfectionManager] Start requested. Waiting Game scene load + player collection...");

        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            CollectPlayersFromConnectedClients();
            TryStartMatch();
        }
    }

    private void OnLoadEventCompleted(
        string sceneName,
        LoadSceneMode mode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;
        if (!_startRequested) return;
        if (sceneName != gameSceneName) return;

        Debug.Log("[InfectionManager] Game scene loaded. Collecting players from ConnectedClients...");

        CollectPlayersFromConnectedClients();
        TryStartMatch();
    }

    private void CollectPlayersFromConnectedClients()
    {
        _players.Clear();

        if (NetworkManager.Singleton == null) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var po = client.PlayerObject;
            if (po != null && po.TryGetComponent<PlayerState>(out var ps))
            {
                _players.Add(ps);
            }
        }

        Debug.Log($"[InfectionManager] Collected players: {_players.Count}/{playersToStart}");
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!_matchStarted) return;

        if (Time.time < _nextScoreTime) return;
        _nextScoreTime = Time.time + scoreTickInterval;

        foreach (var p in _players)
        {
            if (p == null) continue;

            var data = p.CurrentState.Value;
            if (!data.IsInfected)
            {
                data.Score += scorePerTick;
                p.CurrentState.Value = data;
            }
        }
    }

    private void TryStartMatch()
    {
        if (!IsServer) return;
        if (!_startRequested) return;
        if (_matchStarted) return;
        if (_players.Count < playersToStart) return;

        foreach (var p in _players)
            p.SetInfectionStatus(false);

        int index = Random.Range(0, _players.Count);
        _players[index].SetInfectionStatus(true);

        _matchStarted = true;

        Debug.Log($"[InfectionManager] Match started. Infected OwnerClientId={_players[index].OwnerClientId}");
    }
}
