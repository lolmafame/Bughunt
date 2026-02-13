using UnityEngine;
using TMPro; 
using UnityEngine.UI;

public class TogglePassword : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Image eyeIconImage;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;



    private bool isPasswordVisible = false;

    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            passwordField.contentType = TMP_InputField.ContentType.Standard;
            eyeIconImage.sprite = eyeOpenSprite;
        }
        else
        {
            passwordField.contentType = TMP_InputField.ContentType.Password;
            eyeIconImage.sprite = eyeClosedSprite;
        }

        passwordField.ForceLabelUpdate();
        passwordField.text = passwordField.text;
    }

}
