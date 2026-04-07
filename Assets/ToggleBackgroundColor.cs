using UnityEngine;
using UnityEngine.UI;
public class ToggleBackgroundColor : MonoBehaviour
{
    public Toggle toggle;
    public Image targetImage;

    public Color onColor = new Color(81, 193, 81);
    public Color offColor = new Color(188, 93, 93);

    void Start()
    {
        toggle.onValueChanged.AddListener(UpdateColor);
        UpdateColor(toggle.isOn);
    }

    void UpdateColor(bool isOn)
    {
        targetImage.color = isOn ? onColor : offColor;
    }
}
