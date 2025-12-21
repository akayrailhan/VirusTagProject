using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.Netcode;

// Quest 5: Callsign Forge - Oyuncu ismini ve oyun akışını yöneten ana kontrolcü
public class ApplicationController : MonoBehaviour
{
    public static ApplicationController Instance;

    private void Awake()
    {
        // Singleton pattern: Sadece bir tane ApplicationController olmalı
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne değişince yok olma

            if (NetworkManager.Singleton != null)
                DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Oyuncu ismini PlayerPrefs'e kaydet (Quest 5)
    public void SavePlayerName(string playerName)
    {
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
        Debug.Log($"[AppController] Player name saved: {playerName}");
    }

    // Kayıtlı ismi getir
    public string GetPlayerName()
    {
        return PlayerPrefs.GetString("PlayerName", "Player" + Random.Range(100, 999));
    }

    // Bir sonraki sahneye (Lobby Menüsü) geç
    public void GoToLobbyMenu()
    {
        SceneManager.LoadScene("LobbyMenu");
    }

    public void StartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[StartGame] NetworkManager.Singleton is null!");
            return;
        }

        // SADECE host/server sahne yüklemeyi tetikler
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[StartGame] Only host can start the game.");
            return;
        }

        // Build Settings’teki sahne adıyla aynı olmalı
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}