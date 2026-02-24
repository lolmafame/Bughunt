using UnityEngine;

[System.Serializable]
public class SubtitleLine
{
    [TextArea(2, 5)]
    public string text;

    public float typingSpeed = 0.04f;
    public bool glitchTyping = false;

    public AudioClip glitchClip;

    [Header("Focus Object (Optional)")]
    public GameObject focusObject;

    [Header("Delay Before Text")]
    public float delayBeforeTyping = 0.3f;
}