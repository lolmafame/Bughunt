using UnityEngine;
using TMPro;

public class CertificateUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI panelText;

    public void ShowCertificateByID(int id)
    {
        panel.SetActive(true);

        switch (id)
        {
            case 1:
                panelText.text = "C#";
                break;
            case 2:
                panelText.text = "JAVASCRIPT";
                break;
            case 3:
                panelText.text = "JAVA";
                break;
            case 4:
                panelText.text = "C++";
                break;
            case 5:
                panelText.text = "PYTHON";
                break;
        }
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
    }
}