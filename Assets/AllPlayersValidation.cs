using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AllPlayersValidation : MonoBehaviour
{
    public List<Toggle> toggles;
    public GameObject warningObject;

    public GameObject hideObject;
    public GameObject openObject;

    [Header("Scene Loading")]
    public GameObject loadingPanel;
    public float loadingTime = 5f;
    public string sceneName;

    public void OnProceedClicked()
    {
        if (!AllTogglesOn())
        {
            warningObject.SetActive(true);
            return;
        }

        warningObject.SetActive(false);

        hideObject.SetActive(false);
        openObject.SetActive(true);

        // Start loading scene
        StartCoroutine(LoadSceneRoutine());
    }

    bool AllTogglesOn()
    {
        foreach (Toggle t in toggles)
        {
            if (!t.isOn)
                return false;
        }
        return true;
    }

    IEnumerator LoadSceneRoutine()
    {
        loadingPanel.SetActive(true);

        yield return new WaitForSeconds(loadingTime);

        SceneManager.LoadScene(sceneName);
    }
}