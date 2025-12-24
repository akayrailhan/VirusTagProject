using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private float refreshInterval = 0.5f; // yarım saniyede bir güncelle

    private float _nextUpdateTime;

    private void Update()
    {
        if (Time.time < _nextUpdateTime) return;
        _nextUpdateTime = Time.time + refreshInterval;

        // Sahnedeki tüm oyuncuları bul
        PlayerState[] players = FindObjectsOfType<PlayerState>();
        var list = new List<PlayerData>();

        foreach (var ps in players)
        {
            list.Add(ps.CurrentState.Value);
        }

        // Skora göre sırala (büyükten küçüğe)
        list.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Metni hazırla: "İsim - Skor"
        StringBuilder sb = new StringBuilder();
        foreach (var p in list)
        {
            // PlayerName bir FixedString, ToString() ile normal string’e çeviriyoruz
            sb.AppendLine($"{p.PlayerName.ToString()} : {p.Score}");
        }

        if (leaderboardText != null)
        {
            leaderboardText.text = sb.ToString();
        }
    }
}