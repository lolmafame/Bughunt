using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    private Dictionary<string, int> progress = new Dictionary<string, int>();
    private HashSet<string> unlocked = new HashSet<string>();

    void Awake()
    {
        Instance = this;
    }

    public void AddProgress(string id, int amount)
    {
        if (!progress.ContainsKey(id))
            progress[id] = 0;

        progress[id] += amount;

        CheckUnlock(id);
    }

    void CheckUnlock(string id)
    {
        if (unlocked.Contains(id)) return;

        Achievement a = AchievementDatabase.Get(id);
        if (a == null) return;

        if (progress[id] >= a.targetValue)
        {
            unlocked.Add(id);
            Debug.Log("Unlocked: " + a.title);

            // TODO: trigger UI update
            AchievementUI.Instance.ShowUnlocked(a);
        }
    }

    public bool IsUnlocked(string id)
    {
        return unlocked.Contains(id);
    }
}