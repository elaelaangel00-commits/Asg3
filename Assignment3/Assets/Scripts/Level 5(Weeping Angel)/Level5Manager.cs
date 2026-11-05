using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Level5Manager : MonoBehaviour
{
    public static Level5Manager Instance { get; private set; }

    // Remembers that the intro was already shown, so a restart doesn't show it again
    private static bool introSeen;

    // ---------- Angels ----------
    [Header("Angels")]
    [SerializeField] private int totalAngels = 3;
    [SerializeField] private AudioClip angelsVanishSound;   // optional

    // ---------- Item puzzle ----------
    [Header("Items")]
    [SerializeField] private int requiredItems = 3;

    // ---------- UI ----------
    [Header("UI")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text messageText;      // temporary hint text (can be left empty)
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Intro popup")]
    [SerializeField] private GameObject introPanel;     // the hint popup shown at the start
    [SerializeField] private bool showIntroOnRestart = false;

    [Header("Held item indicator")]
    [SerializeField] private TMP_Text heldText;         // "Holding: Red Skull"
    [SerializeField] private Image heldIcon;            // colour swatch of the held skull

    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private AudioClip interactSound;

    // ---------- Audio ----------
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;     // the Audio Source with the Scream
    [SerializeField] private AudioClip screamClip;      // the Scream file
    [SerializeField] private AudioSource musicSource;   // your BackgroundMusic source

    private int lockedCount;
    private int placedCount;
    private bool puzzleSolved;
    private bool gameEnded;
    private bool introOpen;
    private Coroutine messageRoutine;

    // Items the player is carrying, in the order they were picked up
    private readonly List<ItemPickUp> inventory = new List<ItemPickUp>();
    private int selectedIndex;

    public bool HasHeldItem => inventory.Count > 0;
    public ItemPickUp HeldItem => inventory.Count > 0 ? inventory[selectedIndex] : null;
    public int RequiredItems => requiredItems;

    // Items taken from the floor = still carried + already on the table
    private int CollectedCount => inventory.Count + placedCount;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (sfxSource != null) sfxSource.ignoreListenerPause = true;
    }

    private void Start()
    {
        if (messageText != null) messageText.text = "";

        if (introPanel != null)
        {
            if (!introSeen || showIntroOnRestart) ShowIntro();
            else introPanel.SetActive(false);
        }

        UpdateHUD();
    }

    private void Update()
    {
        // While the intro popup is open, only listen for the key that closes it
        if (introOpen)
        {
            if (IntroDismissPressed()) CloseIntro();
            return;
        }

        if (gameEnded || puzzleSolved) return;

        // Number keys 1/2/3 choose which carried item to place
        int key = GetNumberKeyPressed();
        if (key >= 0) SelectItem(key);
    }

    // ===================== INTRO POPUP =====================

    private void ShowIntro()
    {
        introOpen = true;
        introPanel.SetActive(true);
        Time.timeScale = 0f;                       // pause the game behind the popup
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Also called by the Start button (On Click)
    public void CloseIntro()
    {
        introOpen = false;
        introSeen = true;
        if (introPanel != null) introPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Works with both the old and the new Unity Input System
    private bool IntroDismissPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame ||
                              kb.enterKey.wasPressedThisFrame ||
                              kb.eKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.Return) ||
               Input.GetKeyDown(KeyCode.E);
#endif
    }

    private int GetNumberKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return -1;
        if (kb.digit1Key.wasPressedThisFrame) return 0;
        if (kb.digit2Key.wasPressedThisFrame) return 1;
        if (kb.digit3Key.wasPressedThisFrame) return 2;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
#endif
        return -1;
    }

    // ===================== ITEM PUZZLE =====================

    // Called by ItemPickup when the player gets close to a skull
    public bool TryPickUp(ItemPickUp item)
    {
        if (inventory.Contains(item)) return false;

        inventory.Add(item);
        selectedIndex = inventory.Count - 1;   // newest item becomes the selected one
        UpdateHUD();
        ShowMessage("Collected " + item.ColoredName + " (" + CollectedCount + "/" + requiredItems + ")");
        return true;
    }

    // Called by TableDeposit: removes the SELECTED item from the player's inventory
    public ItemPickUp TakeHeldItem()
    {
        if (inventory.Count == 0) return null;

        ItemPickUp item = inventory[selectedIndex];
        inventory.RemoveAt(selectedIndex);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, inventory.Count - 1));

        UpdateHUD();
        return item;
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= inventory.Count) return;

        selectedIndex = index;
        UpdateHUD();
        ShowMessage("Selected: " + inventory[index].ColoredName);
    }

    public void SetPlacedCount(int count)
    {
        placedCount = count;
        UpdateHUD();
    }

    // Called by TableDeposit when the sequence is correct
    public void PuzzleSolved()
    {
        puzzleSolved = true;
        UpdateHUD();
        ShowMessage("Correct! The angels vanished. Find the exit door.", 3f);
        DisappearAngels();
    }

    // Removes every weeping angel from the level
    private void DisappearAngels()
    {
        WeepingAngelAI[] angels = FindObjectsByType<WeepingAngelAI>(FindObjectsSortMode.None);
        foreach (WeepingAngelAI angel in angels)
            angel.gameObject.SetActive(false);

        if (angelsVanishSound != null && sfxSource != null)
            sfxSource.PlayOneShot(angelsVanishSound);
    }

    // Shows a short message on screen, then clears it
    public void ShowMessage(string text, float seconds = 2f)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    private IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.text = text;
        yield return new WaitForSeconds(seconds);
        messageText.text = "";
    }

    // ===================== ANGELS =====================

    public bool IsAllAngelsSecured()
    {
        return lockedCount >= totalAngels;
    }

    public void RegisterLockedAngel()
    {
        lockedCount++;
        UpdateHUD();
    }

    // ===================== GAME STATE =====================

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //public void ReturnToStartMenu()
    //{
    //    Time.timeScale = 1f;
    //    SceneManager.LoadScene(startMenuSceneName);
    //}

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

    // ===================== HUD =====================

    private void UpdateHUD()
    {
        UpdateHeldIndicator();

        if (progressText == null) return;

        if (puzzleSolved)
        {
            progressText.text = "Find the exit door";
            return;
        }

        string text = "Skulls collected: " + CollectedCount + "/" + requiredItems +
                      "\nPlaced on table: " + placedCount + "/" + requiredItems;

        if (inventory.Count > 0)
        {
            text += "\n\nCarrying (press number to select):";
            for (int i = 0; i < inventory.Count; i++)
            {
                string marker = (i == selectedIndex) ? "> " : "   ";
                string tag = (i == selectedIndex) ? "  (selected)" : "";
                text += "\n" + marker + (i + 1) + ". " + inventory[i].ColoredName + tag;
            }
        }

        progressText.text = text;
    }

    // Shows which skull the player is holding right now
    private void UpdateHeldIndicator()
    {
        ItemPickUp held = HeldItem;

        if (heldText != null)
        {
            if (puzzleSolved)
                heldText.text = "";
            else if (held != null)
                heldText.text = "Holding: " + held.ColoredName + "\nFind its matching slot on the table";
            else
                heldText.text = "Hands empty";
        }

        if (heldIcon != null)
        {
            heldIcon.enabled = held != null && !puzzleSolved;
            if (held != null) heldIcon.color = held.ItemColor;
        }
    }
    private void LateUpdate()
    {
        // Runs after every other script's Update, so it wins over a look script
        // that keeps re-locking the cursor
        if (introOpen || gameEnded)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}