using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    public GameObject lockIcon;
    public GameObject checkmarkIcon;
    public Button levelButton;
    public Image baseImage;
    private Color lockedColor = new Color(1, 1, 1, 0.5f);
    private Color completeColor = new Color (17, 64, 6, 0.2f);
    private Color normalColor = Color.white;

    public void SetState(LevelState state)
    {
        switch (state)
        {
            case LevelState.Closed:
                lockIcon.SetActive(true);
                checkmarkIcon.SetActive(false);
                levelButton.interactable = false;
                baseImage.color = lockedColor; 
                break;

            case LevelState.Open:
                lockIcon.SetActive(false);
                checkmarkIcon.SetActive(false);
                levelButton.interactable = true;
                baseImage.color = normalColor;
                break;

            case LevelState.Complete:
                lockIcon.SetActive(false);
                checkmarkIcon.SetActive(true);
                levelButton.interactable = true;
                baseImage.color = normalColor;
                break;
        }
    }
}