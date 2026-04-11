using UnityEngine;

[CreateAssetMenu(menuName = "Achievements/Achievement")]
public class Achievement : ScriptableObject
{
    public string id;
    public string title;
    public string description;

    public AchievementCategory category;

    public int targetValue = 1;
}