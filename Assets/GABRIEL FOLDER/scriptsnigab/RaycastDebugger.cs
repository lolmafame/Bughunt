using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData ped = new PointerEventData(EventSystem.current);
            ped.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            Debug.Log($"=== Raycast hit {results.Count} objects ===");
            foreach (var r in results)
                Debug.Log($"  {r.gameObject.name} | depth {r.depth} | sort {r.sortingOrder}");
        }
    }
}