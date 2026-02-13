using UnityEngine;

public class Quitgame : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Application Quit requested");
    }
}
