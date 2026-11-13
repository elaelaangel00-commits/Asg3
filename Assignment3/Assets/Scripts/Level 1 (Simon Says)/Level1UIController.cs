using UnityEngine;
using UnityEngine.SceneManagement;

// pause menu for level 1 (same idea as Level2UIController, without the notes screen).
// esc pauses: stops time, frees the mouse, hides the hud and turns off gameplay scripts.
// esc again (or the resume button) puts everything back the way it was.
public class Level1UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hud;            // e.g. the crosshair
    [SerializeField] private GameObject pausePanel;

    [Header("Gameplay Scripts to Disable while Paused")]
    [SerializeField] private Behaviour[] gameplayScripts;   // e.g. shooting and movement

    [Header("Scene")]
    [SerializeField] private string startMenuScene = "StartMenuScene";

    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudio;
    [SerializeField] private AudioClip menuOpenClip;

    // static = other scripts can check Level1UIController.IsPaused
    public static bool IsPaused { get; private set; }

    private bool[] previousEnabledStates;   // remembers which scripts were on before pausing
    private float previousTimeScale = 1f;

    private void Start()
    {
        // no audio source given: add a 2d one so the pause sound can still play
        if (uiAudio == null)
        {
            uiAudio = gameObject.AddComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f;
        }

        // load the clip now so the first pause isn't silent (the clip is set not to preload)
        if (menuOpenClip != null) menuOpenClip.LoadAudioData();

        IsPaused = false;
        Time.timeScale = 1f;
        ApplyPanels();
        SetCursor(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // esc toggles between paused and playing
        if (IsPaused) ResumeGame();
        else OpenPause();
    }

    public void OpenPause()
    {
        if (IsPaused) return;

        // remember the current state so resume can restore it exactly
        previousTimeScale = Time.timeScale;
        previousEnabledStates = new bool[gameplayScripts.Length];

        for (int i = 0; i < gameplayScripts.Length; i++)
        {
            if (gameplayScripts[i] == null) continue;
            previousEnabledStates[i] = gameplayScripts[i].enabled;
            gameplayScripts[i].enabled = false;   // turn the script off while paused
        }

        IsPaused = true;
        Time.timeScale = 0f;   // 0 = time stops (timers, physics, animations)
        SetCursor(true);
        ApplyPanels();

        if (uiAudio != null && menuOpenClip != null) uiAudio.PlayOneShot(menuOpenClip);
    }

    // also called by the resume button
    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = previousTimeScale;

        // turn the scripts back on, but only the ones that were on before
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

    // called by the main menu button
    public void ReturnToStartMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;   // reset time, or the next scene would start frozen
        SetCursor(true);
        SceneManager.LoadSceneAsync(startMenuScene);
    }

    // show the hud while playing, show the pause panel while paused
    private void ApplyPanels()
    {
        if (hud != null) hud.SetActive(!IsPaused);
        if (pausePanel != null) pausePanel.SetActive(IsPaused);
    }

    // menu open = free visible mouse, playing = mouse locked to the centre
    private void SetCursor(bool menuOpen)
    {
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;
    }

    // safety: never leave the game frozen when this object is removed
    private void OnDestroy()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
