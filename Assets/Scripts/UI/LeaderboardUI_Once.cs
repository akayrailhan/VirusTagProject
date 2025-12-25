using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class LeaderboardUI_Once : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI leaderboardText;

    [Header("Options")]
    [SerializeField] private bool includeOnlyActive = true;
    [SerializeField] private float waitForPlayersSeconds = 0.5f;

    [Header("Leave Button")]
    [SerializeField] private Button leaveButton;              // ✅ buraya butonu sürükle
    [SerializeField] private string lobbyMenuSceneName = "LobbyMenu";
    [SerializeField] private string currentLobbyId;           // opsiyonel (join sonrası SetCurrentLobbyId ile de set edebilirsin)

    private bool _leaving;

    private void Awake()
    {
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveClicked);
            leaveButton.onClick.AddListener(OnLeaveClicked);
        }
        else
        {
            Debug.LogWarning("[LeaderboardUI_Once] leaveButton atanmadı (Inspector'dan ver).");
        }
    }

    private void Start()
    {
        StartCoroutine(BuildOnce());
    }

    private IEnumerator BuildOnce()
    {
        float end = Time.time + Mathf.Max(0f, waitForPlayersSeconds);

        PlayerState[] players = Array.Empty<PlayerState>();
        while (Time.time < end)
        {
            players = includeOnlyActive
                ? FindObjectsOfType<PlayerState>()
                : FindObjectsOfType<PlayerState>(true);

            if (players.Length > 0) break;
            yield return null;
        }

        // Son bir kez al
        players = includeOnlyActive
            ? FindObjectsOfType<PlayerState>()
            : FindObjectsOfType<PlayerState>(true);

        var list = new List<PlayerData>(players.Length);
        foreach (var ps in players)
            list.Add(ps.CurrentState.Value);

        list.Sort((a, b) => b.Score.CompareTo(a.Score));

        StringBuilder sb = new StringBuilder();
for (int i = 0; i < list.Count; i++)
{
    var p = list[i];
    sb.AppendLine($"{i + 1}. {p.PlayerName.ToString()} : {p.Score}");
}

if (leaderboardText != null)
    leaderboardText.text = sb.ToString();


        if (leaderboardText != null)
            leaderboardText.text = sb.ToString();
    }

    // Join sonrası çağırmak için
    public void SetCurrentLobbyId(string lobbyId)
    {
        currentLobbyId = lobbyId;
    }

    // Button listener buraya bağlı
    private async void OnLeaveClicked()
    {
        if (_leaving) return;
        _leaving = true;

        if (leaveButton != null) leaveButton.interactable = false;

        try
        {
            // 1) Netcode bağlantısını kapat
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            // 2) Lobby’den çık
            if (!string.IsNullOrEmpty(currentLobbyId))
            {
                await LobbyManager.LeaveLobby(currentLobbyId); // sende imza farklıysa burayı uyarlarsın
            }
            else
            {
                Debug.LogWarning("[LeaderboardUI_Once] currentLobbyId boş. Lobby leave atlandı.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardUI_Once] Leave failed: {e}");
        }

        // 3) Menüye dön
        SceneManager.LoadScene(lobbyMenuSceneName, LoadSceneMode.Single);
    }
}
