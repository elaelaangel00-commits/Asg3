using UnityEngine;
using UnityEngine.UI;   // use TMPro instead if your texts are TextMeshPro

public class Level5Manager : MonoBehaviour
{
    public static Level5Manager Instance { get; private set; }

    private int totalAngels = 3;

    //UI & References 
    private Text progressText;
    private GameObject keyObject;
    private GameObject gameOverPanel;
    private GameObject levelCompletePanel;
    private AudioSource unlockSound;


    private int lockedCount;
    private bool gameEnded;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (keyObject != null) keyObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
    }

    public void RegisterLockedAngel()
    {
        lockedCount++;
        UpdateHUD();

        if (lockedCount >= totalAngels)
        {
            keyObject.SetActive(true);   // or play a box-opening animation
            if (unlockSound != null) unlockSound.Play();
        }
    }
    //Check if all angels are trapped
    public bool IsAllAngelsSecured()
    {
        return lockedCount >= totalAngels;
    }

    public void PlayerCaught()
    {
        if (gameEnded) return;
        gameEnded = true;

        // This will print to your console safely without throwing errors
        Debug.Log("GAME OVER: Player was caught by a Weeping Angel!");

        // Only try to turn on the panel if you have actually assigned one
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Call this from your key pickup / exit door script
    public void CompleteLevel()
    {
        if (gameEnded) return;
        gameEnded = true;
        levelCompletePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }



    private void UpdateHUD()
    {
        progressText.text = $"Angels locked: {lockedCount}/{totalAngels}";
    }
}

