using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private bool isMuted = false;
    [SerializeField] private bool isMusicMuted = false;
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;

    [Header("SFX Clips")]
    [Tooltip("AudioClip played when selecting an interactable ant queue. (Source: sounds/Tap sound.mp3)")]
    [SerializeField] private AudioClip tapClip;

    [Tooltip("AudioClip played when an ant queue contacts a blocking queue or wrong-color hole. (Source: sounds/wrong move.mp3)")]
    [SerializeField] private AudioClip ouchClip;

    [Tooltip("AudioClip played once when a level is successfully completed. (Source: sounds/winning sound.mp3)")]
    [SerializeField] private AudioClip winClip;

    [Header("SFX Volume Controls")]
    [Range(0f, 1f)] [SerializeField] private float tapVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float impactVolume = 0.95f;
    [Range(0f, 1f)] [SerializeField] private float completionVolume = 1.0f;

    private AudioSource sfxSource;
    public AudioSource SfxSource
    {
        get
        {
            if (sfxSource == null) InitAudioSource();
            return sfxSource;
        }
    }

    private AudioClip proceduralTapClip;
    private AudioClip hopClip;
    private AudioClip swallowClip;
    private AudioClip blockedClip;
    private AudioClip proceduralWinClip;
    private AudioClip loseClip;
    private AudioClip proceduralOuchClip;

    private float lastTapTime = -1f;
    private const float TapThrottleDuration = 0.04f;
    private bool hasPlayedWinSoundForCurrentLevel = false;

    private const string SoundMutedKey = "SoundMuted";
    private const string MusicMutedKey = "MusicMuted";

    public bool IsMuted => isMuted;
    public bool IsMusicMuted => isMusicMuted;
    public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }

    public AudioClip TapClip { get => tapClip; set => tapClip = value; }
    public AudioClip OuchClip { get => ouchClip; set => ouchClip = value; }
    public AudioClip WinClip { get => winClip; set => winClip = value; }

    public float TapVolume { get => tapVolume; set => tapVolume = Mathf.Clamp01(value); }
    public float ImpactVolume { get => impactVolume; set => impactVolume = Mathf.Clamp01(value); }
    public float CompletionVolume { get => completionVolume; set => completionVolume = Mathf.Clamp01(value); }

    public void InitAudioSource()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D Non-positional playback
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        isMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;
        isMusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;

        InitAudioSource();

#if UNITY_EDITOR
        LoadDefaultClipsIfUnassigned();
#endif

        GenerateProceduralAudioClips();

        // Subscribe to game events using named methods to allow reliable unsubscription
        AntQueueController.OnQueueMoveStarted += HandleQueueMoveStarted;
        AntQueueController.OnQueueBlockedFeedback += HandleQueueBlocked;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        LevelManager.OnLevelLoaded += HandleLevelLoaded;
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
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded(Level level)
    {
        ResetCompletionGuard();
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
        if (state == GameState.LevelWon)
        {
            PlayWin();
        }
        else if (state == GameState.LevelLost)
        {
            PlayLose();
        }
        else if (state == GameState.Playing)
        {
            ResetCompletionGuard();
        }
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

        // Muting silences currently playing effects immediately
        if (isMuted && SfxSource != null)
        {
            SfxSource.Stop();
        }
    }

    public void SetMusicMuted(bool muted)
    {
        isMusicMuted = muted;
        PlayerPrefs.SetInt(MusicMutedKey, isMusicMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ResetCompletionGuard()
    {
        hasPlayedWinSoundForCurrentLevel = false;
    }

    public void PlayTap()
    {
        if (Time.unscaledTime - lastTapTime < TapThrottleDuration) return;
        lastTapTime = Time.unscaledTime;

        AudioClip clip = tapClip != null ? tapClip : proceduralTapClip;
        PlayClip(clip, tapVolume, Random.Range(0.98f, 1.02f));
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
    /// Plays the tap/selection sound once when an interactable ant queue is selected.
    /// </summary>
    public void PlayQueueSelect()
    {
        // Prevent duplicate events and excessive volume buildup from identical simultaneous taps
        if (Time.unscaledTime - lastTapTime < TapThrottleDuration) return;
        lastTapTime = Time.unscaledTime;

        AudioClip clip = tapClip != null ? tapClip : proceduralTapClip;
        PlayClip(clip, tapVolume, Random.Range(0.98f, 1.02f));
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
        AudioClip clip = ouchClip != null ? ouchClip : proceduralOuchClip;
        PlayClip(clip, impactVolume, Random.Range(0.98f, 1.02f));
    }

    /// <summary>
    /// Plays the level completed fanfare once when confirmed, coordinated with completion overlay.
    /// </summary>
    public void PlayWin()
    {
        if (hasPlayedWinSoundForCurrentLevel) return;
        hasPlayedWinSoundForCurrentLevel = true;

        AudioClip clip = winClip != null ? winClip : proceduralWinClip;
        PlayClip(clip, completionVolume, 1.0f);
    }

    public void PlayLose()
    {
        PlayClip(loseClip, 0.9f, 0.9f);
    }

    private void PlayClip(AudioClip clip, float volumeScale, float pitch)
    {
        if (isMuted || clip == null || sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale * masterVolume));
    }

#if UNITY_EDITOR
    public void LoadDefaultClipsIfUnassigned()
    {
        if (tapClip == null)
        {
            tapClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Tap sound.mp3");
        }
        if (ouchClip == null)
        {
            ouchClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/wrong move.mp3");
        }
        if (winClip == null)
        {
            winClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/winning sound.mp3");
        }
    }
#endif

    private void GenerateProceduralAudioClips()
    {
        int sampleRate = 44100;

        // 1. Crisp Pop / Tap Clip (0.06s) - Fallback
        proceduralTapClip = CreateTone(sampleRate, 0.06f, (t, dur) =>
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

        // 5. Victory Major Chime (0.65s) - Fallback
        proceduralWinClip = CreateTone(sampleRate, 0.65f, (t, dur) =>
        {
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

        // 7. Bump / Ouch Tone (0.16s) - Fallback
        proceduralOuchClip = CreateTone(sampleRate, 0.16f, (t, dur) =>
        {
            float freq = Mathf.Lerp(320f, 130f, t / dur);
            float env = Mathf.Exp(-t * 24f);
            return (Mathf.Sin(2 * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(4 * Mathf.PI * freq * t)) * env;
        });
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
