using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a click sound whenever any Button under this object is clicked.
///
/// Setup: put this on the Canvas. It finds every Button below it automatically,
/// including buttons inside panels that are currently hidden (like the quit popup).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ButtonClickSounds : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;   // only play when a button is clicked
        source.spatialBlend = 0f;     // 2D sound: same volume wherever the camera is
        source.clip = null;           // the click sound comes from "Click Clip", not from this field
        source.Stop();                // stop anything that already started playing

        // true = also include inactive objects, so the hidden popup's buttons get the sound too
        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            Button captured = b;   // copy, so each listener remembers its own button
            b.onClick.AddListener(() => PlayClick(captured));
        }
    }

    private void PlayClick(Button clicked)
    {
        // TEMPORARY debug line: which button, and how long after the scene started
        Debug.Log("ButtonClickSounds: '" + clicked.name + "' clicked at " +
                  Time.timeSinceLevelLoad.ToString("F2") + "s after scene start", clicked);
        if (clickClip != null) source.PlayOneShot(clickClip, volume);
    }
}