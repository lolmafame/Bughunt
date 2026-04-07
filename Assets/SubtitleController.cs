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

    [Header("Content")]
    public GameObject contentGroup;

    [Header("Dialogue Box")]
    public GameObject dialogueBox;
    public Animator dialogueAnimator;

    [Header("Blink Settings")]
    public float blinkSpeed = 0.5f;

    [Header("Dialogue")]
    public SubtitleLine[] lines;

    private Coroutine blinkCoroutine;

    private int currentIndex = 0;
    private Coroutine typingCoroutine;

    private bool isTyping = false;
    private bool lineFinished = false;
    private bool dialogueStarted = false;


    public void BeginDialogue()
    {
        if (dialogueStarted) return;

        dialogueStarted = true;
        StartCoroutine(StartDialogue());
    }

    IEnumerator StartDialogue()
    {
        dialogueBox.SetActive(true);

        if (contentGroup != null)
            contentGroup.SetActive(false);

        if (dialogueAnimator != null)
            dialogueAnimator.SetTrigger("Open");

        yield return new WaitForSeconds(0.5f); // match fold animation length

        if (contentGroup != null)
            contentGroup.SetActive(true);

        HideIndicators();
        StartLine();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentIndex >= lines.Length)
                return;

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

        if (line.focusObjects != null)
        {
            foreach (GameObject obj in line.focusObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (line.glitchClip != null && glitchAudioSource != null)
        {
            glitchAudioSource.PlayOneShot(line.glitchClip);
        }

        StartCoroutine(LineDelay(line));
    }

    IEnumerator LineDelay(SubtitleLine line)
    {
        yield return new WaitForSeconds(line.delayBeforeTyping);

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(SubtitleLine line)
    {
        subtitleText.text = "";
        isTyping = true;

        StartTypingSound();

        string fullText = line.text;
        int i = 0;

        while (i < fullText.Length)
        {
            if (fullText[i] == '<')
            {
                int closingIndex = fullText.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    subtitleText.text += fullText.Substring(i, closingIndex - i + 1);
                    i = closingIndex + 1;
                    continue;
                }
            }

            subtitleText.text += fullText[i];
            i++;

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
        if (currentIndex >= lines.Length)
            return;

        if (lines[currentIndex].focusObjects != null)
        {
            foreach (GameObject obj in lines[currentIndex].focusObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        currentIndex++;

        if (currentIndex >= lines.Length)
        {
            StartCoroutine(EndDialogue());
            return;
        }

        StartLine();
    }

    IEnumerator EndDialogue()
    {
        subtitleText.text = "";
        HideIndicators();

        // Hide text and icons FIRST
        if (contentGroup != null)
            contentGroup.SetActive(false);

        yield return new WaitForSeconds(0.05f);

        if (dialogueAnimator != null)
            dialogueAnimator.SetTrigger("Close");

        yield return new WaitForSeconds(0.5f);

        dialogueBox.SetActive(false);
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