using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemEnforcer : MonoBehaviour
{
    void Awake()
    {
        var systems = Resources.FindObjectsOfTypeAll<EventSystem>();

        int activeCount = 0;

        foreach (var es in systems)
        {
            if (es.gameObject.activeInHierarchy)
                activeCount++;
        }

        if (activeCount > 1)
        {
            Debug.Log("Destroying duplicate EventSystem: " + gameObject.name);
            Destroy(gameObject);
        }
    }
}