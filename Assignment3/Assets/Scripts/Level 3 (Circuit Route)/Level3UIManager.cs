using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Level3UIManager : MonoBehaviour
{
    public static Level3UIManager Instance { get; private set; }
    public static bool IsPaused { get; private set; } 

    [Header("HUD")]
    public GameObject hud;           
    public TMP_Text objectiveText;   
    public TMP_Text progressText;   
    public TMP_Text promptText;      
    public TMP_Text messageText;    

    [Header("Pause menu")]
    public GameObject pausePanel;    
    public Button resumeButton;    
    public Button mainMenuButton;    
    public string startMenuScene = "StartMenuScene";  
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
        Resume(); 
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

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

    //---------------- HUD ----------------

    public void SetObjective(string objective, string progress)
    {
        if (objectiveText != null) objectiveText.text = objective;
        if (progressText != null) progressText.text = progress;
    }

    public void SetPrompt(string text)
    {
        if (promptText != null && promptText.text != text) promptText.text = text;
    }

    public static void Message(string text, Color colour, float duration = 2f)
    {
        if (Instance == null || Instance.messageText == null) return;
        Instance.messageText.text = text;
        Instance.messageText.color = colour;
        Instance.messageTimer = duration;
    }

    //---------------- Pause menu ----------------

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f; //stops physics, movement
        AudioListener.pause = true;       

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
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(startMenuScene);
    }
}