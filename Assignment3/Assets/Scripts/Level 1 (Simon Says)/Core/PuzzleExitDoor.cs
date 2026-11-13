using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backrooms.Core
{
    // the level exit door.
    // once the puzzle is solved, the player looks at the door (within range)
    // and presses E to load the next level.
    // works with any puzzle that inherits from Puzzle.
    public class PuzzleExitDoor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Puzzle puzzle;
        [SerializeField] private Camera playerCamera;            // left empty = uses Camera.main

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField, Min(0f)] private float interactRange = 3f;   // must be this close to use the door
        [SerializeField, Min(0f)] private float loadDelay = 0.5f;     // small wait so the door sound can play

        [Header("Prompt")]
        [SerializeField] private TMP_Text promptText;                // left empty = created automatically on the HUD canvas
        [SerializeField] private bool autoCreatePrompt = true;
        [SerializeField] private string lockedMessage = "The door is locked";   // leave blank to show nothing while locked
        [SerializeField] private string openMessage = "Press E to exit";

        [Header("Next Level")]
        [SerializeField] private string nextSceneName = "Level 2";

        [Header("Audio")]
        [SerializeField] private AudioSource doorOpenAudio;

        private bool hasStartedLoading;   // stops the level loading twice

        // the door can only be used when the puzzle is solved and we're not already leaving
        public bool CanInteract => puzzle != null && puzzle.IsSolved && !hasStartedLoading;

        private void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (promptText == null && autoCreatePrompt) promptText = CreatePrompt();
            ShowPrompt(false, null);
        }

        private void Update()
        {
            if (Time.timeScale == 0f || hasStartedLoading)          // paused or already leaving
            {
                ShowPrompt(false, null);
                return;
            }

            // show the right message only while the player is looking at the door
            bool lookingAtDoor = IsPlayerLookingAtDoor();
            string message = CanInteract ? openMessage : lockedMessage;
            ShowPrompt(lookingAtDoor && !string.IsNullOrEmpty(message), message);

            // all 3 must be true: looking at it, puzzle solved, E pressed
            if (lookingAtDoor && CanInteract && Input.GetKeyDown(interactKey))
                StartCoroutine(LoadNextLevel());
        }

        // raycast from the crosshair, only as far as interactRange
        private bool IsPlayerLookingAtDoor()
        {
            if (playerCamera == null) return false;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            return ContainsCollider(hit.collider);
        }

        // true if the collider is the door itself or one of its children (frame, handle...)
        public bool ContainsCollider(Collider collider)
        {
            return collider.transform == transform || collider.transform.IsChildOf(transform);
        }

        // makes a text prompt just under the crosshair if none was assigned
        private TMP_Text CreatePrompt()
        {
            // find the screen ui canvas (not a world-space one)
            Canvas hudCanvas = null;
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                {
                    hudCanvas = canvas;
                    break;
                }
            }

            if (hudCanvas == null)
            {
                Debug.LogWarning("PuzzleExitDoor: no screen-space Canvas found, so no prompt can be shown.", this);
                return null;
            }

            GameObject promptObject = new GameObject("ExitPrompt (auto)", typeof(RectTransform));
            promptObject.transform.SetParent(hudCanvas.transform, false);

            // centre of the screen, 80 pixels down
            RectTransform rect = (RectTransform)promptObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI text = promptObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 32;
            text.color = Color.white;
            text.raycastTarget = false;   // so the text never blocks mouse clicks
            return text;
        }

        private void ShowPrompt(bool visible, string message)
        {
            if (promptText == null) return;
            promptText.gameObject.SetActive(visible);
            if (visible) promptText.text = message;
        }

        // coroutine: play the sound, wait a bit, then load the next level
        private IEnumerator LoadNextLevel()
        {
            hasStartedLoading = true;
            ShowPrompt(false, null);

            if (doorOpenAudio != null) doorOpenAudio.Play();

            yield return new WaitForSeconds(loadDelay);

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
