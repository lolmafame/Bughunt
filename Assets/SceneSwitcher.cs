using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneSwitcher : MonoBehaviour
{
    public GameObject loadingPanel;
    public float loadingTime = 5f;

    public void LoadSceneWithDelay(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        loadingPanel.SetActive(true); 

        yield return new WaitForSeconds(loadingTime); 

        SceneManager.LoadScene(sceneName); 
    }
}