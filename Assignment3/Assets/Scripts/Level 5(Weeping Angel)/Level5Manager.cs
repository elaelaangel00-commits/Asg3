using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level5Manager : MonoBehaviour
{
    public static Level5Manager Instance { get; private set; }

    //Angels
    [SerializeField] private int totalAngels = 3;

   //UI
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;

    //Key
    [SerializeField] private GameObject keyObject;   // hidden until all angels are locked
    [SerializeField] private AudioSource unlockSound;

    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private AudioClip interactSound;

    //Audio
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;     // the Audio Source with the Scream
    [SerializeField] private AudioClip screamClip;      // the Scream file
    [SerializeField] private AudioSource musicSource;   // your BackgroundMusic source

    private int lockedCount;
    private bool gameEnded;

    public bool HasKey { get; private set; }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;          
        if (sfxSource != null) sfxSource.ignoreListenerPause = true;
    }

    private void Start()
    {
        if (keyObject != null) keyObject.SetActive(false);
        UpdateHUD();
    }

    public bool IsAllAngelsSecured()
    {
        return lockedCount >= totalAngels;
    }

    public void RegisterLockedAngel()
    {
        lockedCount++;
        UpdateHUD();

        if (IsAllAngelsSecured())
        {
            if (keyObject != null) keyObject.SetActive(true);
            if (unlockSound != null) unlockSound.Play();
        }
    }

    public void GiveKey()
    {
        HasKey = true;
        UpdateHUD();
    }

    public void PlayerCaught()
    {
        if (gameEnded) return;
        gameEnded = true;
        Debug.Log("PLAYER CAUGHT! Game over starts now");

        if (musicSource != null) musicSource.Stop();
        else Debug.LogWarning("Music Source is not assigned");

        AudioListener.pause = true;                     
       
        if (sfxSource != null && screamClip != null)
            sfxSource.PlayOneShot(screamClip);           

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToStartMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startMenuSceneName);
    }
    public void CompleteLevel()
    {
        if (gameEnded) return;
        gameEnded = true;
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
        if (Level5Audio.Instance != null) Level5Audio.Instance.PlayLevelComplete();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateHUD()
    {
        if (progressText == null) return;

        if (HasKey)
            progressText.text = "Find the exit door";
        else if (IsAllAngelsSecured())
            progressText.text = "All angels locked! Get the key";
        else
            progressText.text = "Angels locked: " + lockedCount + "/" + totalAngels;
    }
}