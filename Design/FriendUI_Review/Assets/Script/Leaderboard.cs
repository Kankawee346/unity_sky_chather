using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;
}

[Serializable]
class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public static class Leaderboard
{
    private const string Key = "leaderboard_v1";
    private const int MaxEntries = 10;

    public static List<LeaderboardEntry> Load()
    {
        var json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<LeaderboardEntry>();
        try
        {
            var data = JsonUtility.FromJson<LeaderboardData>(json);
            return data != null && data.entries != null ? data.entries : new List<LeaderboardEntry>();
        }
        catch { return new List<LeaderboardEntry>(); }
    }

    public static void SaveScore(string playerName, int score)
    {
        if (string.IsNullOrWhiteSpace(playerName)) playerName = "PLAYER";
        playerName = playerName.Trim();
        if (playerName.Length > 10) playerName = playerName.Substring(0, 10);

        var entries = Load();
        var existing = entries.FirstOrDefault(e => string.Equals(e.name, playerName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (score > existing.score) existing.score = score;
            existing.name = playerName;
        }
        else
        {
            entries.Add(new LeaderboardEntry { name = playerName, score = score });
        }

        entries = entries.OrderByDescending(e => e.score).ThenBy(e => e.name).Take(MaxEntries).ToList();
        var data = new LeaderboardData { entries = entries };
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
