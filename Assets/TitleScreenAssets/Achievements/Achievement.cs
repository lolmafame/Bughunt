using UnityEngine;

public enum AchievementCategory
{
    BugHunter,
    Upgrade,
    Speed,
    Leaderboard,
    Progression
}

[System.Serializable]
public class Achievement
{
    public string id;
    public string title;
    public string description;

    public AchievementCategory category; // ⭐ NEW

    public Sprite lockedIcon;
    public Sprite unlockedIcon;

    public int targetValue;
    public bool isUnlocked = false;
}