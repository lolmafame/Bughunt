using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public ThirdPersonMovement player;
    public Image staminaFill;

    void Update()
    {
        if (player != null)
        {
            staminaFill.fillAmount = player.GetStaminaNormalized();
        }
    }
}
