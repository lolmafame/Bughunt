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

        foreach (var a in AchievementDatabase.GetAll())
        {
            bool saved = PlayerPrefs.GetInt(a.id, 0) == 1;

            if (saved)
            {
                unlocked.Add(a.id);
            }
        }
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

        Achievement a = AchievementDatabase.GetAll().Find(x => x.id == id);
        if (a == null) return;

        if (!progress.ContainsKey(id)) return;

        if (progress[id] >= a.targetValue)
        {
            unlocked.Add(id);

            a.isUnlocked = true; // ⭐ ADD THIS

            PlayerPrefs.SetInt(id, 1); // ⭐ SAVE IT

            Debug.Log("Unlocked: " + a.title);

            AchievementUI.Instance.ShowUnlocked(a);
        }
    }

    public bool IsUnlocked(string id)
    {
        return unlocked.Contains(id);
    }
}