using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    [Header("GLOBAL VOLUME (1–10)")]
    [Range(1, 10)] public float musicVolume = 10;
    [Range(1, 10)] public float sfxVolume = 10;

    float musicMultiplier => musicVolume / 10f;
    float sfxMultiplier => sfxVolume / 10f;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource dangerMusicSource;
    public AudioSource sfxSource;
    public AudioSource footstepSource;
    public AudioSource spiderFootstepSource;
    public AudioSource spiderAmbientSource;
    public AudioSource playerVoiceSource;    // for hurt/breathing sounds

    [Header("--- MUSIC ---")]
    public AudioClip bgMusicClip;
    public AudioClip dangerMusicClip;

    [Header("--- PLAYER SOUNDS ---")]
    public AudioClip[] walkFootsteps;
    public AudioClip[] runFootsteps;
    public AudioClip[] hurtClips;            // when player gets hit
    public AudioClip staminaExhaustedClip;   // when stamina runs out
    public AudioClip sprintLoopClip;         // looping breath while sprinting

    [Header("--- SPIDER SOUNDS ---")]
    public AudioClip[] spiderFootsteps;
    public AudioClip spiderAmbientClip;
    public AudioClip spiderDetectedClip;
    public AudioClip spiderLostClip;

    [Header("--- TERMINAL SOUNDS ---")]
    public AudioClip terminalOpenClip;
    public AudioClip terminalCloseClip;
    public AudioClip terminalTypingClip;
    public AudioClip terminalCorrectClip;
    public AudioClip terminalWrongClip;

    [Header("--- UI SOUNDS ---")]
    public AudioClip pauseOpenClip;
    public AudioClip pauseCloseClip;
    public AudioClip buttonClickClip;

    [Header("--- GAME EVENT SOUNDS ---")]
    public AudioClip terminalCompleteClip;
    public AudioClip allTerminalsClip;
    public AudioClip gameOverClip;
    public AudioClip wallRemovedClip;

    [Header("Music Settings")]
    public float bgMusicVolume = 0.4f;
    public float dangerMusicVolume = 0.7f;
    public float fadeSpeed = 1f;

    [Header("Footstep Settings")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    public float spiderStepInterval = 0.4f;

    [Header("Sprint Audio Settings")]
    public float sprintBreathVolume = 0.5f;

    private bool isDanger = false;
    private float playerStepTimer = 0f;
    private float spiderStepTimer = 0f;
    private bool wasSprinting = false;
    private ThirdPersonMovement playerMove;
    private UnityEngine.AI.NavMeshAgent spiderAgent;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {

        LoadVolumes();

        // Find player and spider automatically
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerMove = player.GetComponent<ThirdPersonMovement>();

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spiderAgent = spider.GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Start background music
        if (bgMusicClip != null)
        {
            musicSource.clip = bgMusicClip;
            musicSource.loop = true;
            musicSource.volume = bgMusicVolume * musicMultiplier;
            musicSource.Play();
        }

        // Prepare danger music
        if (dangerMusicClip != null)
        {
            dangerMusicSource.clip = dangerMusicClip;
            dangerMusicSource.loop = true;
            dangerMusicSource.volume = 0f; // stays 0 initially, multiplier applied in fade
            dangerMusicSource.Play();
        }

        // Start spider ambient
        if (spiderAmbientClip != null)
        {
            spiderAmbientSource.clip = spiderAmbientClip;
            spiderAmbientSource.loop = true;
            spiderAmbientSource.volume = 0.3f;
            spiderAmbientSource.Play();
        }

        // Prepare sprint breath loop
        if (sprintLoopClip != null)
        {
            playerVoiceSource.clip = sprintLoopClip;
            playerVoiceSource.loop = true;
            playerVoiceSource.volume = 0f;
            playerVoiceSource.Play();
        }
    }

    void Update()
    {
        HandleMusicFade();
        HandlePlayerFootsteps();
        HandleSpiderFootsteps();
        HandleSprintAudio();
    }

    // ------------------------------------------------
    // MUSIC
    // ------------------------------------------------
    void HandleMusicFade()
    {
        float targetMusic = isDanger ? 0f : bgMusicVolume * musicMultiplier;
        float targetDanger = isDanger ? dangerMusicVolume * musicMultiplier : 0f;

        musicSource.volume = Mathf.MoveTowards(
            musicSource.volume, targetMusic, fadeSpeed * Time.deltaTime);

        dangerMusicSource.volume = Mathf.MoveTowards(
            dangerMusicSource.volume, targetDanger, fadeSpeed * Time.deltaTime);
    }

    public void SetDangerMode(bool danger)
    {
        isDanger = danger;
    }

    // ------------------------------------------------
    // PLAYER FOOTSTEPS
    // ------------------------------------------------
    void HandlePlayerFootsteps()
    {
        if (playerMove == null || !playerMove.enabled) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = new Vector3(h, 0f, v).magnitude >= 0.1f;
        bool isRunning = playerMove.IsActuallyRunning(); // ← replaces old isRunning line

        if (isMoving)
        {
            playerStepTimer -= Time.deltaTime;
            if (playerStepTimer <= 0f)
            {
                PlayPlayerFootstep(isRunning);
                playerStepTimer = isRunning ? runStepInterval : walkStepInterval;
            }
        }
        else
        {
            playerStepTimer = 0f;
            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }
    }

    void PlayPlayerFootstep(bool running)
    {
        AudioClip[] clips = running ? runFootsteps : walkFootsteps;
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        footstepSource.clip = clip;
        footstepSource.Play(); // interrupts the previous clip, no stacking, no delay
    }

    // ------------------------------------------------
    // SPRINT AUDIO
    // ------------------------------------------------
    void HandleSprintAudio()
    {
        if (playerMove == null || !playerMove.enabled)
        {
            if (playerVoiceSource != null)
                playerVoiceSource.volume = 0f;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = new Vector3(h, 0f, v).magnitude >= 0.1f;
        bool isSprinting = playerMove.IsActuallyRunning(); // ← replaces old isSprinting line

        if (sprintLoopClip != null)
        {
            // Smoothly fade sprint breath in and out
            float targetVolume = isSprinting ? sprintBreathVolume * sfxMultiplier : 0f;
            playerVoiceSource.volume = Mathf.MoveTowards(
                playerVoiceSource.volume, targetVolume, Time.deltaTime * 3f);
        }

        // Play stamina exhausted sound once when stamina runs out
        if (wasSprinting && !isSprinting && staminaExhaustedClip != null)
        {
            if (playerMove.GetStaminaNormalized() <= 0f)
                sfxSource.PlayOneShot(staminaExhaustedClip);
        }

        wasSprinting = isSprinting;
    }

    // ------------------------------------------------
    // SPIDER FOOTSTEPS
    // ------------------------------------------------
    void HandleSpiderFootsteps()
    {
        if (spiderAgent == null) return;

        if (spiderAgent.velocity.magnitude > 0.5f)
        {
            spiderStepTimer -= Time.deltaTime;
            if (spiderStepTimer <= 0f)
            {
                PlaySpiderFootstep();
                spiderStepTimer = spiderStepInterval;
            }
        }
    }

    void PlaySpiderFootstep()
    {
        if (spiderFootsteps == null || spiderFootsteps.Length == 0) return;
        AudioClip clip = spiderFootsteps[Random.Range(0, spiderFootsteps.Length)];
        spiderFootstepSource.PlayOneShot(clip, 0.5f * sfxMultiplier);
    }

    // ------------------------------------------------
    // PUBLIC ONE-SHOT METHODS
    // ------------------------------------------------
    public void PlaySpiderDetected()
    {
        PlaySFX(spiderDetectedClip);
        SetDangerMode(true);
    }

    public void PlaySpiderLost()
    {
        PlaySFX(spiderLostClip);
        SetDangerMode(false);
    }

    public void PlayHurt()
    {
        if (hurtClips == null || hurtClips.Length == 0) return;
        AudioClip clip = hurtClips[Random.Range(0, hurtClips.Length)];
        playerVoiceSource.PlayOneShot(clip, 0.8f * sfxMultiplier);
    }

    public void PlayTerminalOpen() => PlaySFX(terminalOpenClip);
    public void PlayTerminalClose() => PlaySFX(terminalCloseClip);
    public void PlayTerminalTyping() => PlaySFX(terminalTypingClip);
    public void PlayTerminalCorrect() => PlaySFX(terminalCorrectClip);
    public void PlayTerminalWrong() => PlaySFX(terminalWrongClip);
    public void PlayPauseOpen() => PlaySFX(pauseOpenClip);
    public void PlayPauseClose() => PlaySFX(pauseCloseClip);
    public void PlayButtonClick() => PlaySFX(buttonClickClip);
    public void PlayTerminalComplete() => PlaySFX(terminalCompleteClip);
    public void PlayAllTerminalsDone() => PlaySFX(allTerminalsClip);
    public void PlayGameOver() => PlaySFX(gameOverClip);
    public void PlayWallRemoved() => PlaySFX(wallRemovedClip);

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxMultiplier);
    }

    // --------------------
    // VOLUME CONTROL
    // --------------------
    public void SetMusicVolume(float value) // 1–10
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value) // 1–10
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    void LoadVolumes()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 10);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 10);
    }
}