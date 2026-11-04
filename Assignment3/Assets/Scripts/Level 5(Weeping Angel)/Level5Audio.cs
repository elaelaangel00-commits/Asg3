using UnityEngine;

public class Level5Audio : MonoBehaviour
{
    public static Level5Audio Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;   // your BackgroundMusic source
    [SerializeField] private AudioSource stingSource;   // new 2D source for stings

    [Header("Clips")]
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip levelCompleteClip;

    private void Awake()
    {
        Instance = this;
        AudioListener.pause = false;                 // reset after a restart
        stingSource.ignoreListenerPause = true;      // stings still play when paused
    }

    public void PlayGameOver()
    {
        if (musicSource != null) musicSource.Stop();
        AudioListener.pause = true;                  // silence the angels and the room
        if (gameOverClip != null) stingSource.PlayOneShot(gameOverClip);
    }

    public void PlayLevelComplete()
    {
        if (musicSource != null) musicSource.Stop();
        AudioListener.pause = true;
        if (levelCompleteClip != null) stingSource.PlayOneShot(levelCompleteClip);
    }
}