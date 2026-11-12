using UnityEngine;

public class Level3AudioManager : MonoBehaviour
{
    public static Level3AudioManager Instance;

    [Header("Sources")]
    public AudioSource MusicSource;     
    public AudioSource SfxSource;
    public AudioSource PlayerMovement;
    public AudioSource AmbienceSource;
    public AudioSource MachineHumSource;

    [Header("Music")]
    public AudioClip Level3BGM;
    [Range(0f, 1f)] public float musicVolume = 0.3f;

    [Header("Ambience")]
    public AudioClip Ambience;
    [Range(0f, 1f)] public float ambienceVolume = 0.4f;
    public AudioClip MachineHum;
    [Range(0f, 1f)] public float machineHumVolume = 0.6f;

    [Header("Player SFX (2D)")]
    public AudioClip PlayerWalk;
    public AudioClip PlayerJump;
    public AudioClip Fall;

    [Header("Wire SFX (2D)")]
    public AudioClip Grab;
    public AudioClip Connect;
    public AudioClip Wrong;
    public AudioClip Wrap;
    public AudioClip Unwrap;
    public AudioClip ReturnWire;

    [Header("World SFX (3D)")]
    public AudioClip ShortCircuit;
    public AudioClip SectionPowered; 
    public AudioClip GateOpens;        
    public AudioClip DoorAppears;

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float walkVolume = 0.5f;
    public float sfxMaxDistance = 30f;

    [Header("Player detection")]
    public bool needGroundForWalk = true; 
    public float jumpSoundCooldown = 0.3f;  

    PlayerMovement player;
    float lastJumpSound = -10f;

    void Awake()
    {
        Instance = this;

        if (SfxSource == null) Debug.LogError("Level3AudioManager: drag an AudioSource into 'Sfx Source'.", this);
        else SfxSource.volume = 1f;   
        PlayMusic();
        PlayAmbience();
        PlayMachineHum();
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        if (player == null) Debug.LogWarning("Level3AudioManager: no PlayerMovement found in the scene - walk/jump sounds won't play.", this);
        if (PlayerMovement == null) Debug.LogWarning("Level3AudioManager: 'Player Movement' AudioSource is empty - walk sound won't play.", this);
        if (PlayerWalk == null) Debug.LogWarning("Level3AudioManager: 'Player Walk' clip is empty.", this);
        if (PlayerJump == null) Debug.LogWarning("Level3AudioManager: 'Player Jump' clip is empty.", this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (player == null) return;
        if (Level3UIManager.IsPaused)
        {
            PlayerMoves(false);
            return;
        }

        bool grounded = player.IsGrounded;

        // jump
        if (Input.GetKeyDown(player.jumpkey) && grounded && Time.time - lastJumpSound > jumpSoundCooldown)
        {
            PlaySFX(PlayerJump);
            lastJumpSound = Time.time;
        }

        // walk
        bool pressingMove = Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f;
        PlayerMoves(pressingMove && (grounded || !needGroundForWalk));
    }

    // ---------- MUSIC ----------
    public void PlayMusic()
    {
        if (Level3BGM == null || MusicSource == null) return;

        MusicSource.clip = Level3BGM;
        MusicSource.loop = true;
        MusicSource.spatialBlend = 0f;
        MusicSource.volume = musicVolume;
        MusicSource.Play();
    }

    // ---------- AMBIENCE ----------
    public void PlayAmbience()
    {
        if (Ambience == null || AmbienceSource == null) return;

        AmbienceSource.clip = Ambience;
        AmbienceSource.loop = true;
        AmbienceSource.spatialBlend = 0f;
        AmbienceSource.volume = ambienceVolume;
        AmbienceSource.Play();
    }

    public void PlayMachineHum()
    {
        if (MachineHum == null || MachineHumSource == null) return;

        MachineHumSource.clip = MachineHum;
        MachineHumSource.loop = true;
        MachineHumSource.spatialBlend = 1f;
        MachineHumSource.rolloffMode = AudioRolloffMode.Linear;
        MachineHumSource.minDistance = 1f;
        MachineHumSource.maxDistance = 15f;
        MachineHumSource.volume = machineHumVolume;
        MachineHumSource.Play();
    }

    // ---------- SFX ----------
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && SfxSource != null) SfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXAt(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        GameObject go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = sfxVolume;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = sfxMaxDistance;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    public void PlayerMoves(bool moving)
    {
        if (PlayerMovement == null) return;

        if (!moving)
        {
            if (PlayerMovement.isPlaying) PlayerMovement.Stop();
            return;
        }

        if (PlayerWalk == null) return;

        if (PlayerMovement.clip != PlayerWalk)
        {
            PlayerMovement.clip = PlayerWalk;
            PlayerMovement.loop = true;
            PlayerMovement.volume = walkVolume;
            PlayerMovement.Play();
        }
        else if (!PlayerMovement.isPlaying)
        {
            PlayerMovement.Play();
        }
    }
}