using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private bool isMuted = false;
    [SerializeField] private bool isMusicMuted = false;
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;

    [Header("SFX Clips")]
    [Tooltip("AudioClip played when an ant queue bumps into a stationary obstacle. If unassigned, procedural tone is used.")]
    [SerializeField] private AudioClip ouchClip;

    private AudioSource sfxSource;

    private AudioClip tapClip;
    private AudioClip hopClip;
    private AudioClip swallowClip;
    private AudioClip blockedClip;
    private AudioClip winClip;
    private AudioClip loseClip;

    private const string SoundMutedKey = "SoundMuted";
    private const string MusicMutedKey = "MusicMuted";

    public bool IsMuted => isMuted;
    public bool IsMusicMuted => isMusicMuted;
    public AudioClip OuchClip { get => ouchClip; set => ouchClip = value; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;
        isMusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        GenerateProceduralAudioClips();

        // Subscribe to game events using named methods to allow reliable unsubscription
        AntQueueController.OnQueueMoveStarted += HandleQueueMoveStarted;
        AntQueueController.OnQueueBlockedFeedback += HandleQueueBlocked;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        AntQueueController.OnQueueMoveStarted -= HandleQueueMoveStarted;
        AntQueueController.OnQueueBlockedFeedback -= HandleQueueBlocked;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleQueueMoveStarted(AntQueueController queue)
    {
        // Suppressed to prevent stacking with queue selection sound per user specification
    }

    private void HandleQueueBlocked(AntQueueController queue)
    {
        PlayBlocked();
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.LevelWon) PlayWin();
        else if (state == GameState.LevelLost) PlayLose();
    }

    public void ToggleMute()
    {
        SetMuted(!isMuted);
    }

    public void ToggleMusic()
    {
        SetMusicMuted(!isMusicMuted);
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt(SoundMutedKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMusicMuted(bool muted)
    {
        isMusicMuted = muted;
        PlayerPrefs.SetInt(MusicMutedKey, isMusicMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlayTap()
    {
        PlayClip(tapClip, 0.7f, Random.Range(0.95f, 1.05f));
    }

    public void PlayHop()
    {
        PlayClip(hopClip, 0.5f, Random.Range(0.9f, 1.1f));
    }

    public void PlaySwallow()
    {
        // Hole-entry audio removed per user specification.
    }

    /// <summary>
    /// Plays the hole-entry sound (swallowClip) immediately once when a distinct stationary queue is tapped.
    /// </summary>
    public void PlayQueueSelect()
    {
        PlayClip(swallowClip, 0.85f, Random.Range(0.95f, 1.05f));
    }

    public void PlayBlocked()
    {
        PlayClip(blockedClip, 0.8f, 1.0f);
    }

    public void PlayBlockedSound()
    {
        PlayBlocked();
    }

    /// <summary>
    /// Plays the ouch / bump impact sound once at actual collision impact, respecting mute/volume settings.
    /// </summary>
    public void PlayOuch()
    {
        if (ouchClip != null)
        {
            PlayClip(ouchClip, 0.95f, Random.Range(0.95f, 1.05f));
        }
        else
        {
            PlayClip(blockedClip, 0.85f, 1.0f);
        }
    }

    public void PlayWin()
    {
        PlayClip(winClip, 1.0f, 1.0f);
    }

    public void PlayLose()
    {
        PlayClip(loseClip, 0.9f, 0.9f);
    }

    private void PlayClip(AudioClip clip, float volumeScale, float pitch)
    {
        if (isMuted || clip == null || sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volumeScale * masterVolume);
    }

    private void GenerateProceduralAudioClips()
    {
        int sampleRate = 44100;

        // 1. Crisp Pop / Tap Clip (0.06s)
        tapClip = CreateTone(sampleRate, 0.06f, (t, dur) =>
        {
            float freq = Mathf.Lerp(800f, 300f, t / dur);
            float env = Mathf.Exp(-t * 40f);
            return Mathf.Sin(2 * Mathf.PI * freq * t) * env;
        });

        // 2. Hop / Slither Clip (0.08s)
        hopClip = CreateTone(sampleRate, 0.08f, (t, dur) =>
        {
            float freq = Mathf.Lerp(350f, 520f, t / dur);
            float env = Mathf.Sin(Mathf.PI * (t / dur));
            return Mathf.Sin(2 * Mathf.PI * freq * t) * env;
        });

        // 3. Satisfying Swallow / Gulp Clip (0.15s)
        swallowClip = CreateTone(sampleRate, 0.15f, (t, dur) =>
        {
            float freq = Mathf.Lerp(220f, 480f, Mathf.Sin(t / dur * Mathf.PI));
            float env = Mathf.Sin(Mathf.PI * (t / dur)) * (1f - (t / dur) * 0.4f);
            return Mathf.Sin(2 * Mathf.PI * freq * t) * env;
        });

        // 4. Blocked Wobbly Boing Clip (0.22s)
        blockedClip = CreateTone(sampleRate, 0.22f, (t, dur) =>
        {
            float freq = 160f + Mathf.Sin(t * 70f) * 40f;
            float env = Mathf.Exp(-t * 12f);
            return Mathf.Sin(2 * Mathf.PI * freq * t) * env;
        });

        // 5. Victory Major Chime (0.65s)
        winClip = CreateTone(sampleRate, 0.65f, (t, dur) =>
        {
            // Arpeggio C5 (523), E5 (659), G5 (784), C6 (1046)
            float freq = (t < 0.12f) ? 523.25f : (t < 0.24f) ? 659.25f : (t < 0.36f) ? 783.99f : 1046.50f;
            float env = Mathf.Exp(-(t % 0.12f) * 10f) * Mathf.Clamp01((dur - t) / dur);
            return (Mathf.Sin(2 * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * freq * t)) * env;
        });

        // 6. Level Lost Tone (0.45s)
        loseClip = CreateTone(sampleRate, 0.45f, (t, dur) =>
        {
            float freq = Mathf.Lerp(380f, 150f, t / dur);
            float env = Mathf.Exp(-t * 6f);
            return (Mathf.Sin(2 * Mathf.PI * freq * t) + 0.2f * Mathf.Sin(3 * Mathf.PI * freq * t)) * env;
        });

        // 7. Bump / Ouch Tone (0.16s) - Fallback if no audio asset assigned
        if (ouchClip == null)
        {
            ouchClip = CreateTone(sampleRate, 0.16f, (t, dur) =>
            {
                float freq = Mathf.Lerp(320f, 130f, t / dur);
                float env = Mathf.Exp(-t * 24f);
                return (Mathf.Sin(2 * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(4 * Mathf.PI * freq * t)) * env;
            });
        }
    }

    private AudioClip CreateTone(int sampleRate, float duration, System.Func<float, float, float> sampleFunc)
    {
        int samplesCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[samplesCount];
        for (int i = 0; i < samplesCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Clamp(sampleFunc(t, duration), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralSFX", samplesCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
