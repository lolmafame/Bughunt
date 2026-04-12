using System.Collections.Generic;
using UnityEngine;

public class AchievementDatabase : MonoBehaviour
{
    public List<Achievement> achievements;

    private static Dictionary<string, Achievement> map;
    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (map != null) return;

        map = new Dictionary<string, Achievement>();

        foreach (var a in achievements)
            map[a.id] = a;
    }

    public static List<Achievement> GetAll()
    {
        if (map == null)
        {
            Debug.LogError("AchievementDatabase not initialized!");
            return new List<Achievement>();
        }

        return new List<Achievement>(map.Values);
    }
}