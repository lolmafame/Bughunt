using System.Collections.Generic;
using UnityEngine;

public class AchievementDatabase : MonoBehaviour
{
    public List<Achievement> achievements;

    private static Dictionary<string, Achievement> map;

    void Awake()
    {
        map = new Dictionary<string, Achievement>();

        foreach (var a in achievements)
            map[a.id] = a;
    }

    public static Achievement Get(string id)
    {
        return map.ContainsKey(id) ? map[id] : null;
    }

    public static List<Achievement> GetAll()
    {
        return new List<Achievement>(map.Values);
    }
}