using System.Collections;
using UnityEngine;
using TMPro;

public class SubtitleController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI subtitleText;

    public GameObject continueIcon;        
    public TextMeshProUGUI spaceIndicator;

    [Header("Typing SFX")]
    public AudioSource typingAudioSource;
    public AudioClip typingClip;

    [Header("Glitch SFX")]
    public AudioSource glitchAudioSource;

    [Header("Dialogue")]
    public SubtitleLine[] lines;

    [Header("Dialogue Box")]
    public GameObject dialogueBox;

    [Header("Blink Settings")]
    public float blinkSpeed = 0.5f;

    private Coroutine blinkCoroutine;

    private int currentIndex = 0;
    private Coroutine typingCoroutine;

    private bool isTyping = false;
    private bool lineFinished = false;

    void Start()
    {
        HideIndicators();
        StartLine();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                StopTypingInstant();
            }
            else if (lineFinished)
            {
                NextLine();
            }
        }
    }

    void StartLine()
    {
        HideIndicators();
        lineFinished = false;

        SubtitleLine line = lines[currentIndex];

        if (line.glitchClip != null && glitchAudioSource != null)
        {
            glitchAudioSource.PlayOneShot(line.glitchClip);
        }

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(SubtitleLine line)
    {
        subtitleText.text = "";
        isTyping = true;

        StartTypingSound();

        foreach (char letter in line.text)
        {
            subtitleText.text += letter;

            float speed = line.typingSpeed;

            if (line.glitchTyping)
                speed *= Random.Range(0.5f, 1.8f);

            yield return new WaitForSeconds(speed);
        }

        StopTypingSound();

        isTyping = false;
        lineFinished = true;

        ShowIndicators();
    }

    void StopTypingInstant()
    {
        StopCoroutine(typingCoroutine);

        subtitleText.text = lines[currentIndex].text;

        StopTypingSound();

        isTyping = false;
        lineFinished = true;

        ShowIndicators();
    }

    void NextLine()
    {
        currentIndex++;

        if (currentIndex >= lines.Length)
        {
            subtitleText.text = "";
            HideIndicators();

            if (dialogueBox != null)
                dialogueBox.SetActive(false);

            return;
        }

        StartLine();
    }

    void ShowIndicators()
    {
        if (continueIcon != null)
        {
            continueIcon.SetActive(true);

            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            blinkCoroutine = StartCoroutine(BlinkIcon());
        }

        if (spaceIndicator != null)
            spaceIndicator.gameObject.SetActive(true);
    }

    void HideIndicators()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        if (continueIcon != null)
            continueIcon.SetActive(false);

        if (spaceIndicator != null)
            spaceIndicator.gameObject.SetActive(false);
    }


    void StartTypingSound()
    {
        if (typingClip == null || typingAudioSource == null) return;

        typingAudioSource.clip = typingClip;
        typingAudioSource.loop = true;
        typingAudioSource.Play();
    }

    void StopTypingSound()
    {
        if (typingAudioSource != null && typingAudioSource.isPlaying)
            typingAudioSource.Stop();
    }

    IEnumerator BlinkIcon()
    {
        while (true)
        {
            continueIcon.SetActive(!continueIcon.activeSelf);
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

}
