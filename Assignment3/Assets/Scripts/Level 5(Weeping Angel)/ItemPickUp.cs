using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ItemPickUp : MonoBehaviour
{
    [Header("Puzzle Info")]
    [SerializeField] private int itemID = 1;
    [SerializeField] private string displayName = "Red Skull";

    [Header("Look & UI Prompt")]
    [SerializeField] private Color itemColor = Color.red;
    [SerializeField] private bool tintToColor = true;
    [Tooltip("The 'Press E to Collect' UI object. All skulls can share the same one.")]
    [SerializeField] private GameObject promptUI;

    [Header("Floor Snap")]
    [Tooltip("Layers the skull can rest on. Leave on Everything, or pick only your floor layer.")]
    [SerializeField] private LayerMask surfaceMask = ~0;
    [Tooltip("Keeps the skull standing straight (it only turns left/right).")]
    [SerializeField] private bool keepUpright = true;
    [Tooltip("Also drop the skull onto the floor when the game starts.")]
    [SerializeField] private bool snapOnStart = true;
    [Tooltip("Tiny gap above the surface so the skull doesn't clip into it.")]
    [SerializeField] private float surfaceGap = 0.02f;

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip placeSound;

    public int ItemID => itemID;
    public string DisplayName => displayName;
    public Color ItemColor => itemColor;

    public string ColoredName =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(itemColor) + ">" + displayName + "</color>";

    // The skull currently controlling the shared prompt
    private static ItemPickUp promptOwner;

    private bool isPlaced;
    private Transform player;

    private void Awake()
    {
        if (promptUI != null) promptUI.SetActive(false);

        if (tintToColor)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                foreach (Material m in r.materials)
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", itemColor);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", itemColor);
                }
            }
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (snapOnStart) SnapToSurface(2f);
    }

    private void Update()
    {
        if (isPlaced || player == null || Level5Manager.Instance == null) return;

        // Distance to the player (floor distance only)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        bool inRange = Vector3.Distance(a, b) <= pickupRadius;

        UpdatePrompt(inRange);

        // In range and E pressed -> collect
        if (inRange && InteractPressed())
        {
            if (!Level5Manager.Instance.TryPickUp(this)) return;

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            gameObject.SetActive(false);   // OnDisable hides the prompt
        }
    }

    // ---------- Floor snapping ----------

    // Drops the skull straight down until its lowest point touches the surface below it
    private void SnapToSurface(float rayLift)
    {
        int mask = surfaceMask.value == 0 ? ~0 : surfaceMask.value;

        Vector3 origin = transform.position + Vector3.up * rayLift;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, rayLift + 5f,
                                               mask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;          // our own collider
            if (hit.collider.CompareTag("Player")) continue;                    // the player
            if (hit.collider.GetComponentInParent<ItemPickUp>() != null) continue; // other skulls
            if (hit.normal.y < 0.5f) continue;                                  // only flat, upward-facing surfaces

            if (GetVisualBounds(out Bounds b))
            {
                // Move up/down so the bottom of the model sits on the surface
                float move = (hit.point.y + surfaceGap) - b.min.y;
                transform.position += Vector3.up * move;
            }
            return;
        }

        Debug.LogWarning(name + ": no floor found below it, left where it was.");
    }

    // A box around the skull's visible meshes (ignores lights and 3D text)
    private bool GetVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (r.GetComponent<TMP_Text>() != null) continue;

            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    // ---------- Prompt ----------

    // Only the skull that showed the prompt may hide it
    private void UpdatePrompt(bool inRange)
    {
        if (promptUI == null) return;

        if (inRange)
        {
            promptOwner = this;
            promptUI.SetActive(true);
        }
        else if (promptOwner == this)
        {
            promptOwner = null;
            promptUI.SetActive(false);
        }
    }

    private void HidePrompt()
    {
        if (promptOwner == this)
        {
            promptOwner = null;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    // Runs when the skull is collected, hidden, or destroyed
    private void OnDisable()
    {
        HidePrompt();
    }

    // Works with both the old and the new Unity Input System
    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // ---------- Called by other scripts ----------

    // Called when placed on the table
    public void PlaceAt(Transform anchor)
    {
        isPlaced = true;
        HidePrompt();

        Quaternion rot = keepUpright ? Quaternion.Euler(0f, anchor.eulerAngles.y, 0f) : anchor.rotation;
        transform.SetPositionAndRotation(anchor.position, rot);
        gameObject.SetActive(true);
        SnapToSurface(0.5f);   // rest on the table top

        if (placeSound != null)
            AudioSource.PlayClipAtPoint(placeSound, transform.position);
    }

    // Called by ItemSpawner to set a random position
    public void RespawnAt(Vector3 newPos, Quaternion newRot)
    {
        isPlaced = false;

        Quaternion rot = keepUpright ? Quaternion.Euler(0f, newRot.eulerAngles.y, 0f) : newRot;
        transform.SetPositionAndRotation(newPos, rot);
        gameObject.SetActive(true);
        SnapToSurface(2f);     // drop onto the floor
    }

    // After a wrong order: the skull goes back into the player's hands (hidden)
    public void ReturnToHands()
    {
        isPlaced = false;
        HidePrompt();
        gameObject.SetActive(false);
    }
}