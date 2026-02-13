using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingDots : MonoBehaviour
{
    public TMP_Text loadingText;   // Assign your text here
    public float interval = 0.5f;  // Time between dot changes

    private int dotCount = 0;

    void Start()
    {
        StartCoroutine(AnimateLoading());
    }

    IEnumerator AnimateLoading()
    {
        while (true)
        {
            dotCount++;

            if (dotCount > 3)
                dotCount = 0;

            loadingText.text = "Loading" + new string('.', dotCount);

            yield return new WaitForSeconds(interval);
        }
    }
}
