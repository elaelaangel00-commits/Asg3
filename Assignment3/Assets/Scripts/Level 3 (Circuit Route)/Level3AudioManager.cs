using UnityEngine;

// One place for every sound in the circuit room. Put this on an empty GameObject
// called "AudioManager" and drag all the clips in. Any clip left empty is simply skipped.
//
// Other scripts just call e.g. CircuitAudio.Connect(position);
//  - 3D sounds play at a position in the world (they get quieter with distance)
//  - 2D sounds play at the same volume wherever the player is
public class Level3AudioManager : MonoBehaviour
{
    public static Level3AudioManager Instance { get; private set; }

    [Header("Background music (non-diegetic, 2D, loops)")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.35f;

    [Header("Power box hum (diegetic, 3D, loops)")]
    public AudioClip hum;
    public Transform humSource;              // drag the PowerBox here
    [Range(0f, 1f)] public float humVolume = 0.6f;
    public float humMaxDistance = 15f;

    [Header("Wire sounds (3D, played at the object)")]
    public AudioClip grab;                   // picking up a plug at a source
    public AudioClip connect;                // plug goes into the right socket
    public AudioClip wrong;                  // wrong socket spark / zap
    public AudioClip shortCircuit;           // Section 2: wrong socket resets everything
    public AudioClip wrap;                   // wire wrapped around a rod
    public AudioClip unwrap;                 // last rod unwrapped

    [Header("Feedback sounds (2D)")]
    public AudioClip drop;                   // plug dropped, wire returns to source
    public AudioClip fall;                   // fell into the water
    public AudioClip sectionPowered;         // a power box light turns on
    public AudioClip doorAppear;             // exit door appears

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public float sfxMaxDistance = 20f;

    AudioSource musicSource;
    AudioSource sfx2D;

    void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
        if (music != null) { musicSource.clip = music; musicSource.Play(); }

        sfx2D = gameObject.AddComponent<AudioSource>();
        sfx2D.spatialBlend = 0f;

        if (hum != null && humSource != null)
        {
            AudioSource h = humSource.gameObject.AddComponent<AudioSource>();
            h.clip = hum;
            h.loop = true;
            h.volume = humVolume;
            h.spatialBlend = 1f;
            h.rolloffMode = AudioRolloffMode.Linear;
            h.minDistance = 1f;
            h.maxDistance = humMaxDistance;
            h.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- calls used by the other scripts ----------------

    public static void Grab(Vector3 at) => At(Instance?.grab, at);
    public static void Connect(Vector3 at) => At(Instance?.connect, at);
    public static void Wrong(Vector3 at) => At(Instance?.wrong, at);
    public static void ShortCircuit(Vector3 at) => At(Instance?.shortCircuit, at);
    public static void Wrap(Vector3 at) => At(Instance?.wrap, at);
    public static void Unwrap(Vector3 at) => At(Instance?.unwrap, at);

    public static void Drop() => Flat(Instance?.drop);
    public static void Fall() => Flat(Instance?.fall);
    public static void SectionPowered() => Flat(Instance?.sectionPowered);
    public static void DoorAppear() => Flat(Instance?.doorAppear);

    // ---------------- helpers ----------------

    // 3D one-shot: a temporary AudioSource at that position, removed when the clip ends
    static void At(AudioClip clip, Vector3 position)
    {
        if (Instance == null || clip == null) return;

        GameObject go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = Instance.sfxVolume;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = Instance.sfxMaxDistance;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // 2D one-shot: same volume everywhere
    static void Flat(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        Instance.sfx2D.PlayOneShot(clip, Instance.sfxVolume);
    }
}