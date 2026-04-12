using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to the RoomEntry prefab.
/// MP_MainMenuUI will auto-find these components — 
/// this script is optional but helps with manual setup.
/// </summary>
public class RoomEntry : MonoBehaviour
{
    public TMP_Text roomLabel;   // Shows "Room Name   2/4"
    public Button joinButton;    // Clicking this joins the room
}
