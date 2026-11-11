using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Start menu with Start Game and Quit Game buttons.
/// Quit shows an "Are you sure?" popup with Yes / No.
///
/// Setup: put this on an empty GameObject in the start menu scene,
/// then connect each button's On Click () to the matching method below.
/// </summary>
public class StartMenu : MonoBehaviour
{
    [Tooltip("Exact name of the game scene to load. Must also be added to Build Settings.")]
    [SerializeField] private string gameSceneName = "Level5";

    [Tooltip("The 'Are you sure you want to quit?' popup panel.")]
    [SerializeField] private GameObject quitConfirmPanel;

    [Tooltip("Seconds to wait after a click, so the button sound can finish playing.")]
    [SerializeField] private float soundDelay = 0.3f;

    [Tooltip("Clicks in the first moments after the scene opens are ignored (stray clicks).")]
    [SerializeField] private float ignoreInputTime = 0.5f;

    private float sceneStartTime;

    // True while we are still inside the "ignore stray clicks" window
    private bool TooEarly => Time.unscaledTime - sceneStartTime < ignoreInputTime;

    private void Start()
    {
        sceneStartTime = Time.unscaledTime;

        // The game scenes pause time, mute audio and hide the cursor in places.
        // Reset everything here so the menu always works, even when coming back from the game.
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // The popup always starts hidden
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
    }

    // Hooked to the Start button (On Click)
    public void StartGame()
    {
        if (TooEarly) return;   // ignore stray clicks right after the scene opens
        StartCoroutine(LoadGameRoutine());
    }

    // Hooked to the main Quit button (On Click): opens the popup, does NOT quit yet
    public void OpenQuitConfirm()
    {
        // TEMPORARY debug line: delete once the popup works
        Debug.Log("StartMenu: OpenQuitConfirm called. Panel assigned: " + (quitConfirmPanel != null));

        if (TooEarly) return;
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(true);
    }

    // Hooked to the "No" button (On Click): closes the popup and returns to the menu
    public void CloseQuitConfirm()
    {
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
    }

    // Hooked to the "Yes" button (On Click): actually quits the game
    public void ConfirmQuit()
    {
        if (TooEarly) return;
        StartCoroutine(QuitRoutine());
    }

    // Wait a moment so the click sound is heard, THEN load the scene.
    // (Loading a scene destroys the sound that is still playing in this one.)
    private IEnumerator LoadGameRoutine()
    {
        yield return new WaitForSecondsRealtime(soundDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator QuitRoutine()
    {
        yield return new WaitForSecondsRealtime(soundDelay);
#if UNITY_EDITOR
        // Application.Quit() does nothing inside the editor, so stop Play mode instead
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}