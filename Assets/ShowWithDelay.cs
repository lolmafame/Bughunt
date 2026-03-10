using System.Collections;
using UnityEngine;

public class ShowWithDelay : MonoBehaviour
{
    public GameObject firstObject;   
    public GameObject secondObject;  
    public float delay = 3f;

    void Start()
    {
        secondObject.SetActive(false);
        StartCoroutine(SwitchObjects());
    }

    IEnumerator SwitchObjects()
    {
        yield return new WaitForSeconds(delay);

        firstObject.SetActive(false);
        secondObject.SetActive(true); 
    }
}