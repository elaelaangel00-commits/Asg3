using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Central manager ("game director") for Level 5 (the weeping angels room).
///
/// Responsibilities:
///  1. INTRO POPUP    : shows a hint popup at the start and pauses the game behind it.
///  2. ITEM PUZZLE    : tracks skulls the player carries, selects which one to place,
///                      and counts how many are on the table.
///  3. ANGELS         : removes all angels when the puzzle is solved.
///  4. GAME STATE     : game over (caught), level complete (reached exit), restart.
///  5. HUD            : updates the on-screen text (progress, held item, messages).
///
/// Other scripts talk to this one through the static Instance (singleton), e.g.
///  - WeepingAngelAI  -> PlayerCaught()
///  - ItemPickUp      -> TryPickUp()
///  - TableDeposit    -> TakeHeldItem(), SetPlacedCount(), PuzzleSolved(), ReturnItemsToInventory()
/// </summary>


public class Level5Manager : MonoBehaviour
{
    // Singleton: one global access point so other scripts can call Level5Manager.Instance.X()
    // "private set" means only this class can assign it.
    public static Level5Manager Instance { get; private set; }

    // "static" = belongs to the class, not the object, so it survives a scene reload.
    // Remembers that the intro was already shown, so a restart doesn't show it again
    private static bool introSeen;

    // ------------------------------------------------------------------
    // Inspector settings
    // ------------------------------------------------------------------

    // ---------- Angels ----------
    [Header("Angels")]
    [SerializeField] private int totalAngels = 3;
    [SerializeField] private AudioClip angelsVanishSound;   // optional

    // ---------- Item puzzle ----------
    [Header("Items")]
    [SerializeField] private int requiredItems = 3;         // how many skulls must be placed to win

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

    [Header("Exit")]
    [Tooltip("An empty object placed just past the exit door.")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float exitRadius = 1.5f;   // how close the player must get to finish

    /// Audio 
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;     // the Audio Source with the Scream
    [SerializeField] private AudioClip screamClip;      // the Scream file

    /// Name of the start menu scene (must match the scene file name exactly)
    [SerializeField] private string startMenuSceneName = "StartMenuScene";
    /// Private state


    private int lockedCount;            /// number of angels "secured" (see ANGELS section)
    private int placedCount;            /// skulls currently sitting on the table
    private bool puzzleSolved;          /// true after the correct order is placed
    private bool gameEnded;             /// true after game over OR level complete (blocks both)
    private bool introOpen;             /// true while the intro popup is on screen
    private Coroutine messageRoutine;   /// handle to the running message timer, so we can cancel it

    /// Items the player is carrying, in the order they were picked up
    private readonly List<ItemPickUp> inventory = new List<ItemPickUp>();
    private int selectedIndex;          /// which inventory slot is currently "held"

    private Transform player;

    /// Read-only helpers other scripts can use (expression-bodied properties)
    public bool HasHeldItem => inventory.Count > 0;
    public ItemPickUp HeldItem => inventory.Count > 0 ? inventory[selectedIndex] : null;
    public int RequiredItems => requiredItems;

    /// Items taken from the floor = still carried + already on the table
    private int CollectedCount => inventory.Count + placedCount;

   
    /// Unity lifecycle
    

    private void Awake()
    {
        /// Awake runs before any Start(), so Instance is ready when other scripts need it.
        Instance = this;

        /// Reset pause state. Needed after a restart, because game over leaves
        /// timeScale = 0 and AudioListener paused.
        Time.timeScale = 1f;
        AudioListener.pause = false;

        /// Let the scream play even while AudioListener.pause is true (set in PlayerCaught)
        if (sfxSource != null) sfxSource.ignoreListenerPause = true;
    }

    private void Start()
    {
        /// Cache the player's transform once (cheaper than searching every frame)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (messageText != null) messageText.text = "";

        /// Show the intro only the first time (or every time if showIntroOnRestart is ticked)
        if (introPanel != null)
        {
            if (!introSeen || showIntroOnRestart) ShowIntro();
            else introPanel.SetActive(false);
        }

        UpdateHUD();
    }

    private void Update()
    {
        /// Priority order below: each "return" stops lower checks from running.

        /// 1) While the intro popup is open, only listen for the key that closes it
        if (introOpen)
        {
            if (IntroDismissPressed()) CloseIntro();
            return;
        }

        /// 2) Game over / level complete: ignore all input
        if (gameEnded) return;

        /// 3) After the puzzle is solved: finish the level when the player reaches the exit
        if (puzzleSolved)
        {
            CheckExitReached();
            return;
        }

        /// 4) Normal gameplay: number keys 1/2/3 choose which carried item to place
        int key = GetNumberKeyPressed();
        if (key >= 0) SelectItem(key);
    }

    private void CheckExitReached()
    {
        if (exitPoint == null || player == null) return;

        /// Floor distance only (ignore height), so jumping or stairs don't change the result
        Vector3 a = exitPoint.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;

        if (Vector3.Distance(a, b) <= exitRadius)
            CompleteLevel();
    }

    /// Shows the exit area in the Scene view when this object is selected (editor-only helper)
    private void OnDrawGizmosSelected()
    {
        if (exitPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(exitPoint.position, exitRadius);
    }

    ///  INTRO POPUP 

    private void ShowIntro()
    {
        introOpen = true;
        introPanel.SetActive(true);
        Time.timeScale = 0f;                       /// pause the game behind the popup
        Cursor.lockState = CursorLockMode.None;    /// free the mouse so the player can click Start
        Cursor.visible = true;
    }

    /// Also called by the Start button (On Click)
    public void CloseIntro()
    {
        introOpen = false;
        introSeen = true;                          /// remember, so restarts skip the intro
        if (introPanel != null) introPanel.SetActive(false);
        Time.timeScale = 1f;                       /// resume the game
        Cursor.lockState = CursorLockMode.Locked;  /// back to first-person mouse look
        Cursor.visible = false;
    }

    /// Works with both the old and the new Unity Input System.
    /// #if picks which code gets compiled, depending on the project's Input setting.
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

    /// Returns the inventory slot (0, 1 or 2) for keys 1/2/3, or -1 if none was pressed
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

    /// ITEM PUZZLE 

    /// Called by ItemPickUp when the player gets close to a skull.
    /// Returns true if the pickup succeeded, so the skull can hide itself.
    public bool TryPickUp(ItemPickUp item)
    {
        if (inventory.Contains(item)) return false;   /// already carrying it

        inventory.Add(item);
        selectedIndex = inventory.Count - 1;   /// newest item becomes the selected one
        UpdateHUD();
        ShowMessage("Collected " + item.ColoredName + " (" + CollectedCount + "/" + requiredItems + ")");
        return true;
    }

    /// Called by TableDeposit: removes the SELECTED item from the player's inventory
    public ItemPickUp TakeHeldItem()
    {
        if (inventory.Count == 0) return null;

        ItemPickUp item = inventory[selectedIndex];
        inventory.RemoveAt(selectedIndex);

        /// Keep selectedIndex valid after removal (e.g. removed the last slot -> step back one).
        /// Max(0, ...) protects against an empty list, where Count - 1 would be -1.
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, inventory.Count - 1));

        UpdateHUD();
        return item;
    }

    /// Switch the held item to the given inventory slot (ignores slots the player doesn't have)
    private void SelectItem(int index)
    {
        if (index < 0 || index >= inventory.Count) return;

        selectedIndex = index;
        UpdateHUD();
        ShowMessage("Selected: " + inventory[index].ColoredName);
    }

    /// Called by TableDeposit whenever the number of skulls on the table changes
    public void SetPlacedCount(int count)
    {
        placedCount = count;
        UpdateHUD();
    }

    /// Called by TableDeposit when the sequence is correct
    public void PuzzleSolved()
    {
        puzzleSolved = true;
        UpdateHUD();
        ShowMessage("Correct! The angels vanished. Find the exit door.", 3f);
        DisappearAngels();
    }

    /// Removes every weeping angel from the level
    private void DisappearAngels()
    {
        /// Finds all angels in the scene (None = we don't care about the order)
        WeepingAngelAI[] angels = FindObjectsByType<WeepingAngelAI>(FindObjectsSortMode.None);
        foreach (WeepingAngelAI angel in angels)
            angel.gameObject.SetActive(false);    /// deactivate rather than destroy

        if (angelsVanishSound != null && sfxSource != null)
            sfxSource.PlayOneShot(angelsVanishSound);
    }

    /// Shows a short message on screen, then clears it
    public void ShowMessage(string text, float seconds = 2f)
    {
        if (messageText == null) return;

        /// If a message is already showing, cancel its timer. Otherwise the old timer
        /// would clear the NEW message too early.
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    /// Coroutine = a function that can pause. Show text, wait, then clear it.
    private IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.text = text;
        yield return new WaitForSeconds(seconds);
        messageText.text = "";
    }

    /// ANGELS

    /// True when every angel has been "locked" (secured)
    public bool IsAllAngelsSecured()
    {
        return lockedCount >= totalAngels;
    }

    /// Called by an angel when it becomes locked
    public void RegisterLockedAngel()
    {
        lockedCount++;
        UpdateHUD();
    }

    /// GAME STATE 

    /// Called by WeepingAngelAI when it reaches the player while unseen
    public void PlayerCaught()
    {
        if (gameEnded) return;          /// guard: several angels could call this in the same frame
        gameEnded = true;
        Debug.Log("PLAYER CAUGHT! Game over starts now");
        AudioListener.pause = true;     /// silence the whole scene...

        if (sfxSource != null && screamClip != null)
            sfxSource.PlayOneShot(screamClip);   /// ...except the scream (ignoreListenerPause is on)

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;            /// freeze the game
        Cursor.lockState = CursorLockMode.None;   /// free the mouse for the Restart button
        Cursor.visible = true;
    }

    /// Hooked to the Restart button. Reloads the current scene from scratch.
    public void RestartLevel()
    {
        Time.timeScale = 1f;    /// un-pause first, or the reloaded scene would start frozen
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// Called when the player reaches the exit after solving the puzzle
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

    /// HUD 

    /// Rebuilds all HUD text. Called whenever something changes (not every frame).
    private void UpdateHUD()
    {
        UpdateHeldIndicator();

        if (progressText == null) return;

        /// After solving, the counters no longer matter, so show the next objective
        if (puzzleSolved)
        {
            progressText.text = "Find the exit door";
            return;
        }

        string text = "Skulls collected: " + CollectedCount + "/" + requiredItems +
                      "\nPlaced on table: " + placedCount + "/" + requiredItems;

        /// List carried skulls, with a marker on the selected one
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

    /// Shows which skull the player is holding right now
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
    }

    /// After a wrong order: the table is cleared and the skulls go back into the inventory
    public void ReturnItemsToInventory(List<ItemPickUp> items)
    {
        placedCount = 0;

        foreach (ItemPickUp item in items)
            if (item != null && !inventory.Contains(item)) inventory.Add(item);   /// skip nulls and duplicates

        selectedIndex = 0;
        UpdateHUD();
    }

    /// Hooked to a "Main Menu" button. Loads the start menu scene.
    public void GoToStartMenu()
    {
        /// Reset pause state first, or the menu scene would start frozen and silent
        Time.timeScale = 1f;
        AudioListener.pause = false;

        /// The menu needs a visible, free mouse so the player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(startMenuSceneName);
    }

    private void LateUpdate()
    {
        /// Runs after every other script's Update, so it wins over a look script
        /// that keeps re-locking the cursor
        if (introOpen || gameEnded)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}