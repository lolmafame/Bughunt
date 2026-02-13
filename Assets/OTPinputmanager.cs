using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // <--- new system

public class OTPInputManager : MonoBehaviour
{
    public TMP_InputField[] inputFields;

    void Start()
    {
        for (int i = 0; i < inputFields.Length; i++)
        {
            int index = i;

            inputFields[i].onValueChanged.AddListener((value) =>
            {
                if (value.Length > 1)
                {
                    value = value.Substring(0, 1);
                    inputFields[index].text = value;
                }

                if (value.Length == 1 && index < inputFields.Length - 1)
                {
                    inputFields[index + 1].ActivateInputField();
                }
            });

            inputFields[i].text = "";
        }

        inputFields[0].ActivateInputField();
    }

    void Update()
    {
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            for (int i = 0; i < inputFields.Length; i++)
            {
                if (inputFields[i].isFocused)
                {
                    if (inputFields[i].text != "")
                    {
                        inputFields[i].text = "";
                    }
                    else if (i > 0)
                    {
                        inputFields[i - 1].ActivateInputField();
                        inputFields[i - 1].text = "";
                    }
                    break;
                }
            }
        }
    }

    public string GetOTP()
    {
        string otp = "";
        foreach (var field in inputFields)
            otp += field.text;

        return otp;
    }
}
