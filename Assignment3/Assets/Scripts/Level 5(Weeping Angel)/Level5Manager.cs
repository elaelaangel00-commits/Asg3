using UnityEngine;
using UnityEngine.UI;   // use TMPro instead if your texts are TextMeshPro

public class Level5Manager : MonoBehaviour
{
    public static Level5Manager Instance { get; private set; }

    [SerializeField] private int totalAngels = 3;
    [SerializeField] private Text progressText;
    [SerializeField] private GameObject keyObject;       // starts hidden or in a locked box
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private AudioSource unlockSound;

    private int lockedCount;
    private bool gameEnded;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        keyObject.SetActive(false);
        UpdateHUD();
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

    public void PlayerCaught()
    {
        if (gameEnded) return;
        gameEnded = true;
        gameOverPanel.SetActive(true);
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