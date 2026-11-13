using UnityEngine;
using UnityEngine.SceneManagement;

// Pause menu for Level 1 (same structure as Level2UIController, without the notes screen).
// Esc pauses: freezes time, unlocks the cursor, hides the HUD and disables gameplay scripts.
// Esc again (or the Resume button) restores everything exactly as it was.
public class Level1UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hud;            // e.g. the crosshair
    [SerializeField] private GameObject pausePanel;

    [Header("Gameplay Scripts to Disable while Paused")]
    [SerializeField] private Behaviour[] gameplayScripts;

    [Header("Scene")]
    [SerializeField] private string startMenuScene = "StartMenuScene";

    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudio;
    [SerializeField] private AudioClip menuOpenClip;

    public static bool IsPaused { get; private set; }

    private bool[] previousEnabledStates;   // which scripts were enabled before pausing
    private float previousTimeScale = 1f;

    private void Start()
    {
        // No UI audio source assigned: add a 2D one here so the pause sound always has something to play through
        if (uiAudio == null)
        {
            uiAudio = gameObject.AddComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f;
        }

        // Load the clip now so the first pause isn't silent (the clip is set not to preload)
        if (menuOpenClip != null) menuOpenClip.LoadAudioData();

        IsPaused = false;
        Time.timeScale = 1f;
        ApplyPanels();
        SetCursor(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (IsPaused) ResumeGame();
        else OpenPause();
    }

    public void OpenPause()
    {
        if (IsPaused) return;

        previousTimeScale = Time.timeScale;
        previousEnabledStates = new bool[gameplayScripts.Length];

        for (int i = 0; i < gameplayScripts.Length; i++)
        {
            if (gameplayScripts[i] == null) continue;
            previousEnabledStates[i] = gameplayScripts[i].enabled;
            gameplayScripts[i].enabled = false;
        }

        IsPaused = true;
        Time.timeScale = 0f;
        SetCursor(true);
        ApplyPanels();

        if (uiAudio != null && menuOpenClip != null) uiAudio.PlayOneShot(menuOpenClip);
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = previousTimeScale;

        if (previousEnabledStates != null)
        {
            for (int i = 0; i < gameplayScripts.Length; i++)
            {
                if (gameplayScripts[i] != null) gameplayScripts[i].enabled = previousEnabledStates[i];
            }
        }

        SetCursor(false);
        ApplyPanels();
    }

    public void ReturnToStartMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SetCursor(true);
        SceneManager.LoadSceneAsync(startMenuScene);
    }

    private void ApplyPanels()
    {
        if (hud != null) hud.SetActive(!IsPaused);
        if (pausePanel != null) pausePanel.SetActive(IsPaused);
    }

    private void SetCursor(bool menuOpen)
    {
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;
    }

    private void OnDestroy()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
