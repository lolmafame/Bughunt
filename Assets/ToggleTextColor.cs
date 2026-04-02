using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ToggleTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Toggle toggle;
    public TMP_Text label;

    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleChanged);
        UpdateColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!toggle.isOn)
            label.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!toggle.isOn)
            label.color = normalColor;
    }

    void OnToggleChanged(bool isOn)
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        label.color = toggle.isOn ? selectedColor : normalColor;
    }
}