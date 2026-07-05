using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    private const int DisplayRowCount = 5;
    private const int TopRowsBeforeLocal = 4;

    [SerializeField]
    private TMP_Text fifthRankText;

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private TMP_Text[] nameTexts = new TMP_Text[DisplayRowCount];

    [SerializeField]
    private TMP_Text[] scoreTexts = new TMP_Text[DisplayRowCount];

    [SerializeField]
    private float refreshInterval = 0f;

    private readonly List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    private readonly List<LeaderboardEntry> displayEntries = new List<LeaderboardEntry>();
    private float nextRefreshTime;

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        if (refreshInterval > 0f && Time.time < nextRefreshTime)
        {
            return;
        }

        if (refreshInterval > 0f)
        {
            nextRefreshTime = Time.time + Mathf.Max(0.05f, refreshInterval);
        }

        Refresh();
    }

    private void Refresh()
    {
        CollectEntries();
        entries.Sort(CompareEntries);
        AssignRanks(entries);
        BuildDisplayEntries();
        DrawEntries();
    }

    private void CollectEntries()
    {
        entries.Clear();
        Status[] statuses = FindObjectsOfType<Status>();
        for (int i = 0; i < statuses.Length; i++)
        {
            Status status = statuses[i];
            if (status == null || status.Object == null || !status.Object.IsValid)
            {
                continue;
            }

            entries.Add(
                new LeaderboardEntry
                {
                    Status = status,
                    Name = GetDisplayName(status),
                    Score = status.Score,
                    PlayerKey = status.Object.InputAuthority.RawEncoded,
                }
            );
        }
    }

    private string GetDisplayName(Status status)
    {
        string displayName = status.PlayerDisplayName.ToString();
        return string.IsNullOrWhiteSpace(displayName) ? "Guest00" : displayName;
    }

    private int CompareEntries(LeaderboardEntry a, LeaderboardEntry b)
    {
        int scoreCompare = b.Score.CompareTo(a.Score);
        if (scoreCompare != 0)
        {
            return scoreCompare;
        }

        return a.PlayerKey.CompareTo(b.PlayerKey);
    }

    private void AssignRanks(List<LeaderboardEntry> source)
    {
        for (int i = 0; i < source.Count; i++)
        {
            LeaderboardEntry entry = source[i];
            entry.Rank = i + 1;
            source[i] = entry;
        }
    }

    private void BuildDisplayEntries()
    {
        displayEntries.Clear();
        LeaderboardEntry? localEntry = GetLocalEntry();
        bool showLocalAfterTop = localEntry.HasValue && localEntry.Value.Rank > DisplayRowCount;
        int topCount = showLocalAfterTop ? TopRowsBeforeLocal : DisplayRowCount;

        for (int i = 0; i < entries.Count && displayEntries.Count < topCount; i++)
        {
            displayEntries.Add(entries[i]);
        }

        if (showLocalAfterTop)
        {
            displayEntries.Add(localEntry.Value);
        }
    }

    private LeaderboardEntry? GetLocalEntry()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Status status = entries[i].Status;
            if (status != null && status.Object != null && status.Object.HasInputAuthority)
            {
                return entries[i];
            }
        }

        return null;
    }

    private void DrawEntries()
    {
        UpdateContentRows();

        for (int i = 0; i < DisplayRowCount; i++)
        {
            if (i >= displayEntries.Count)
            {
                SetRow(i, "", "");
                continue;
            }

            LeaderboardEntry entry = displayEntries[i];
            SetRow(i, entry.Name, entry.Score.ToString());
        }

        if (fifthRankText != null)
        {
            fifthRankText.text =
                displayEntries.Count >= DisplayRowCount
                    ? displayEntries[DisplayRowCount - 1].Rank.ToString()
                    : "";
        }
    }

    private void SetRow(int index, string playerName, string score)
    {
        if (nameTexts != null && index < nameTexts.Length && nameTexts[index] != null)
        {
            nameTexts[index].text = playerName;
        }

        if (scoreTexts != null && index < scoreTexts.Length && scoreTexts[index] != null)
        {
            scoreTexts[index].text = score;
        }
    }

    private void UpdateContentRows()
    {
        if (contentParent == null)
        {
            return;
        }

        int visibleCount = Mathf.Min(displayEntries.Count, contentParent.childCount);
        for (int i = 0; i < contentParent.childCount; i++)
        {
            contentParent.GetChild(i).gameObject.SetActive(i < visibleCount);
        }
    }

    private struct LeaderboardEntry
    {
        public Status Status;
        public string Name;
        public int Score;
        public int PlayerKey;
        public int Rank;
    }
}
