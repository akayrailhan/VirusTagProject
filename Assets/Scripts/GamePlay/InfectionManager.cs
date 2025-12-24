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
        if (!IsServer) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    // İstersen PlayerState.OnNetworkSpawn içinde çağır (server tarafında).
    // Ama senin akışta çoğu zaman PlayerObject Lobby'de spawn olduğu için Game'de tekrar OnNetworkSpawn tetiklenmeyebilir.
    // O yüzden asıl toplama işi OnLoadEventCompleted içinde.
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

        // Lobby'de basıldığı an listemiz boştur / eski olabilir; temiz başla
        _players.Clear();

        Debug.Log("[InfectionManager] Start requested. Waiting Game scene load + player collection...");

        // Eğer zaten Game sahnesindeysek (edge case) hemen toplayıp deneyelim
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

    private void TryStartMatch()
    {
        if (!IsServer) return;
        if (!_startRequested) return;
        if (_matchStarted) return;
        if (_players.Count < playersToStart) return;

        // önce herkes clean
        foreach (var p in _players)
            p.SetInfectionStatus(false);

        // sonra random 1 kişiyi infected yap
        int index = Random.Range(0, _players.Count);
        _players[index].SetInfectionStatus(true);

        _matchStarted = true;

        Debug.Log($"[InfectionManager] Match started. Infected OwnerClientId={_players[index].OwnerClientId}");
    }
}
