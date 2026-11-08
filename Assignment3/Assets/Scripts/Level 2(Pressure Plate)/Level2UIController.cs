using UnityEngine;
using UnityEngine.SceneManagement;

public class Level2UIController : MonoBehaviour
{
    private enum ScreenState
    {
        Gameplay,
        Pause,
        Notes
    }

    [Header("Panels")]
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject notesPanel;

    [Header("Gameplay Scripts to Disable in Menus")]
    [SerializeField] private Behaviour[] gameplayScripts;

    [Header("Scene")]
    [SerializeField] private string startMenuScene = "StartMenu";

    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudio;
    [SerializeField] private AudioClip menuOpenClip;
    [SerializeField] private AudioClip noteOpenClip;

    private ScreenState state;
    private bool[] previousEnabledStates;
    private float previousTimeScale = 1f;

    private void Start()
    {
        state = ScreenState.Gameplay;
        ApplyPanels();

        Time.timeScale = 1f;
        SetCursor(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (state == ScreenState.Gameplay)
            {
                OpenPause();
            }
            else
            {
                ResumeGame();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (state == ScreenState.Gameplay)
            {
                OpenNotes();
            }
            else if (state == ScreenState.Notes)
            {
                ResumeGame();
            }
        }
    }

    public void OpenPause()
    {
        OpenScreen(ScreenState.Pause);

        PlayMenuOpen();
    }

    public void OpenNotes()
    {
        OpenScreen(ScreenState.Notes);

        PlayNoteOpenSound();
    }

    private void OpenScreen(ScreenState nextState)
    {
        if (state == ScreenState.Gameplay)
        {
            previousTimeScale = Time.timeScale;
            previousEnabledStates = new bool[gameplayScripts.Length];

            for (int i = 0; i < gameplayScripts.Length; i++)
            {
                if (gameplayScripts[i] == null)
                {
                    continue;
                }

                previousEnabledStates[i] = gameplayScripts[i].enabled;
                gameplayScripts[i].enabled = false;
            }
        }

        state = nextState;
        Time.timeScale = 0f;
        SetCursor(true);
        ApplyPanels();
    }

    public void ResumeGame()
    {
        state = ScreenState.Gameplay;
        Time.timeScale = previousTimeScale;

        if (previousEnabledStates != null)
        {
            for (int i = 0; i < gameplayScripts.Length; i++)
            {
                if (gameplayScripts[i] != null)
                {
                    gameplayScripts[i].enabled = previousEnabledStates[i];
                }
            }
        }

        SetCursor(false);
        ApplyPanels();
    }

    public void ReturnToStartMenu()
    {
        Time.timeScale = 1f;
        SetCursor(true);
        SceneManager.LoadSceneAsync(startMenuScene);
    }

    private void ApplyPanels()
    {
        if (hud != null)
        {
            hud.SetActive(state == ScreenState.Gameplay);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(state == ScreenState.Pause);
        }

        if (notesPanel != null)
        {
            notesPanel.SetActive(state == ScreenState.Notes);
        }
    }

    private void SetCursor(bool menuOpen)
    {
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;
    }

    public void PlayMenuOpen()
    {
        if (uiAudio != null && menuOpenClip != null)
        {
            uiAudio.PlayOneShot(menuOpenClip);
        }
    }

    public void PlayNoteOpenSound()
    {
        if (uiAudio != null && noteOpenClip != null)
        {
            uiAudio.PlayOneShot(noteOpenClip);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
