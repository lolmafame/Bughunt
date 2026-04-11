using UnityEngine;
using UnityEngine.UI;

public class MP_StaminaUI : MonoBehaviour
{
    public Image staminaFill;
    private MP_ThirdPersonMovement player;

    void Update()
    {
        // Find local player if not assigned yet
        if (player == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players)
            {
                MP_ThirdPersonMovement move = p.GetComponent<MP_ThirdPersonMovement>();
                if (move != null && move.IsOwner)
                {
                    player = move;
                    break;
                }
            }
            return;
        }

        if (staminaFill != null)
            staminaFill.fillAmount = player.GetStaminaNormalized();
    }
}