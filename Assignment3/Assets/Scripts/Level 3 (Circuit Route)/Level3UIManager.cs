using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Put this on your Canvas (or UIControllers). Shows the objective, progress, interaction
// prompt and short pop-up messages, and runs the pause menu (Esc).
public class Level3UIManager : MonoBehaviour
{
    public static Level3UIManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }   // other scripts check this

    [Header("HUD")]
    public GameObject hud;           // optional: hidden while paused
    public TMP_Text objectiveText;   // top-left: what to do
    public TMP_Text progressText;    // under it: Power 1/3, Wires 2/5
    public TMP_Text promptText;      // under the crosshair: [E] Grab plug
    public TMP_Text messageText;     // centre: Wrong socket! / Section powered!

    [Header("Pause menu")]
    public GameObject pausePanel;    // PausePanel
    public Button resumeButton;      // ResumeBtn
    public Button mainMenuButton;    // MainMenuBtn
    public string startMenuScene = "StartMenuScene";   // must match the scene name exactly
    public KeyCode pauseKey = KeyCode.Escape;

    float messageTimer;

    void Awake()
    {
        Instance = this;
        SetPrompt("");
        if (messageText != null) messageText.text = "";
    }

    void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        Resume();   // always start unpaused, with the panel hidden
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // safety: never leave the game frozen when this scene unloads
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (IsPaused) Resume();
            else Pause();
        }

        if (messageTimer <= 0f) return;
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f && messageText != null) messageText.text = "";
    }

    // ---------------- HUD ----------------

    public void SetObjective(string objective, string progress)
    {
        if (objectiveText != null) objectiveText.text = objective;
        if (progressText != null) progressText.text = progress;
    }

    public void SetPrompt(string text)
    {
        if (promptText != null && promptText.text != text) promptText.text = text;
    }

    // Call from anywhere: CircuitUI.Message("Wrong socket!", Color.red);
    public static void Message(string text, Color colour, float duration = 2f)
    {
        if (Instance == null || Instance.messageText == null) return;
        Instance.messageText.text = text;
        Instance.messageText.color = colour;
        Instance.messageTimer = duration;
    }

    // ---------------- pause menu ----------------

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;              // stops physics, movement and anything using deltaTime
        AudioListener.pause = true;       // pauses all sounds

        if (pausePanel != null) pausePanel.SetActive(true);
        if (hud != null) hud.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (hud != null) hud.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoToMainMenu()
    {
        // undo the pause first, otherwise the main menu would load frozen and silent
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(startMenuScene);
    }
}