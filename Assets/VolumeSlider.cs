using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSliderUI : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public TMP_Text valueText;
    public Image icon;

    [Header("Icons")]
    public Sprite muteIcon;
    public Sprite lowIcon;
    public Sprite highIcon;

    [Header("Type")]
    public bool isMusic;

    void Start()
    {
        slider.minValue = 1;
        slider.maxValue = 10;
        slider.wholeNumbers = true;

        float saved = PlayerPrefs.GetFloat(isMusic ? "MusicVolume" : "SFXVolume", 10);
        slider.value = saved;

        UpdateUI(saved);
        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnChanged(float value)
    {
        UpdateUI(value);

        if (isMusic)
            SoundManager.Instance.SetMusicVolume(value);
        else
            SoundManager.Instance.SetSFXVolume(value);
    }

    void UpdateUI(float value)
    {
        // Update text
        valueText.text = value.ToString("0");

        // Update icon
        if (value <= 1)
        {
            icon.sprite = muteIcon;
        }
        else if (value <= 5)
        {
            icon.sprite = lowIcon;
        }
        else
        {
            icon.sprite = highIcon;
        }
    }
}