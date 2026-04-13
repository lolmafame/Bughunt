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

    [Header("Buttons")]
    public Button saveButton;
    public Button defaultButton;

    private const float DEFAULT_VOLUME = 10f;
    private string VolumeKey => isMusic ? "MusicVolume" : "SFXVolume";

    void Start()
    {
        slider.minValue = 1;
        slider.maxValue = 10;
        slider.wholeNumbers = true;

        float saved = PlayerPrefs.GetFloat(VolumeKey, DEFAULT_VOLUME);
        slider.value = saved;

        UpdateUI(saved);
        slider.onValueChanged.AddListener(OnChanged);

        // Hook up buttons
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSave);

        if (defaultButton != null)
            defaultButton.onClick.AddListener(OnDefault);
    }

    void OnChanged(float value)
    {
        UpdateUI(value);

        if (isMusic)
            SoundManager.Instance.SetMusicVolume(value);
        else
            SoundManager.Instance.SetSFXVolume(value);
    }

    void OnSave()
    {
        PlayerPrefs.SetFloat(VolumeKey, slider.value);
        PlayerPrefs.Save(); // Forces an immediate write to disk
        Debug.Log($"{(isMusic ? "Music" : "SFX")} volume saved: {slider.value}");
    }

    void OnDefault()
    {
        slider.value = DEFAULT_VOLUME; // This triggers OnChanged automatically
        PlayerPrefs.DeleteKey(VolumeKey); // Optional: clears the saved value too
    }

    void UpdateUI(float value)
    {
        valueText.text = value.ToString("0");

        if (value <= 1)
            icon.sprite = muteIcon;
        else if (value <= 5)
            icon.sprite = lowIcon;
        else
            icon.sprite = highIcon;
    }

    void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnChanged);
        if (saveButton != null) saveButton.onClick.RemoveListener(OnSave);
        if (defaultButton != null) defaultButton.onClick.RemoveListener(OnDefault);
    }
}