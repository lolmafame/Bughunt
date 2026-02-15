using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingDots : MonoBehaviour
{
    public TMP_Text loadingText;

    [SerializeField]
    private string baseText = "Please Wait"; 

    public float dotSpeed = 0.4f;

    private Coroutine dotRoutine;

    void OnEnable()
    {
        dotRoutine = StartCoroutine(AnimateDots());
    }

    void OnDisable()
    {
        if (dotRoutine != null)
            StopCoroutine(dotRoutine);
    }

    public void SetBaseText(string newText)
    {
        baseText = newText;
    }

    public void StopDots()
    {
        StopAllCoroutines();
        loadingText.text = baseText;
    }

    IEnumerator AnimateDots()
    {
        int dotCount = 0;

        while (true)
        {
            loadingText.text = baseText + new string('.', dotCount);

            dotCount++;
            if (dotCount > 3)
                dotCount = 0;

            yield return new WaitForSecondsRealtime(dotSpeed);
        }
    }
}
