using UnityEngine;

[System.Serializable]
public class SubtitleLine
{
    [TextArea(2, 5)]
    public string text;

    public float typingSpeed = 0.04f;

    public bool glitchTyping = false;

    [Header("Glitch SFX")]
    public AudioClip glitchClip;
}
