using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public GameObject campaignPanel;
    public GameObject loginPanel;
    public GameObject computerScreen; // drag ComputerScreen here
    public GameObject LoadingScreen; // drag LoadingScreen here

    void Start()
    {
        if (PlayerPrefs.GetInt("OpenCampaign", 0) == 1)
        {
            PlayerPrefs.SetInt("OpenCampaign", 0);

            if (loginPanel != null) loginPanel.SetActive(false);
            if (computerScreen != null) computerScreen.SetActive(false);
            if (computerScreen != null) LoadingScreen.SetActive(false);
           
            if (campaignPanel != null) campaignPanel.SetActive(true);

            CameraSwitcher cam = FindAnyObjectByType<CameraSwitcher>();
            if (cam != null) cam.ZoomIn();
        }
    }
}