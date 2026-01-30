using UnityEngine;

public class AppearDissapear : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowThing();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowThing()
    {
        gameObject.SetActive(true);
        Invoke("HideThing", 5);
    }

    public void HideThing()
    {
        gameObject.SetActive(false);
        Invoke("ShowThing", 5);
    }
}
