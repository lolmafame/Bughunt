using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RememberToggle : MonoBehaviour
{
    [SerializeField] private Image IconImage;
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Sprite checkedboxSprite;

    private bool rememberUser = false;

    public void ToggleRemember()
    {
        rememberUser = !rememberUser;

        if (rememberUser)
        {
            IconImage.sprite = boxSprite;
        }
        else
        {
            IconImage.sprite = checkedboxSprite;
        }

    }
}
