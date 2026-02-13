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
        loadingPanel.SetActive(true); // show loading panel

        yield return new WaitForSeconds(loadingTime); // wait 5 seconds

        SceneManager.LoadScene(sceneName); // load next scene
    }
}