using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// A collectable skull (puzzle item) in the weeping angels room.
///
/// Life cycle of a skull:
///  1. SPAWN   : ItemSpawner moves it to a random point (RespawnAt) and it drops onto the floor.
///  2. PICK UP : the player walks within pickupRadius, a "Press E" prompt appears,
///               pressing E asks Level5Manager.TryPickUp(); on success the skull hides.
///  3. PLACE   : TableDeposit -> SlotZone.Place() -> PlaceAt() shows it again on the table.
///  4. RESET   : after a wrong order, SlotZone calls ReturnToHands() and the skull hides again
///               (Level5Manager keeps it in the inventory).
///
/// The skull doesn't use colliders to detect the player. It just measures distance.
/// </summary>
public class ItemPickUp : MonoBehaviour
{
    /// Inspector settings


    [Header("Puzzle Info")]
    [SerializeField] private int itemID = 1;                      /// matched against SlotZone.requiredItemID
    [SerializeField] private string displayName = "Red Skull";    /// shown in the HUD

    [Header("Look & UI Prompt")]
    [SerializeField] private Color itemColor = Color.red;
    [SerializeField] private bool tintToColor = true;             /// paint the model with itemColor
    [Tooltip("The 'Press E to Collect' UI object. All skulls can share the same one.")]
    [SerializeField] private GameObject promptUI;

    [Header("Floor Snap")]
    [Tooltip("Layers the skull can rest on. Leave on Everything, or pick only your floor layer.")]
    [SerializeField] private LayerMask surfaceMask = ~0;          /// ~0 = every layer
    [Tooltip("Keeps the skull standing straight (it only turns left/right).")]
    [SerializeField] private bool keepUpright = true;
    [Tooltip("Also drop the skull onto the floor when the game starts.")]
    [SerializeField] private bool snapOnStart = true;
    [Tooltip("Tiny gap above the surface so the skull doesn't clip into it.")]
    [SerializeField] private float surfaceGap = 0.02f;

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 3f;             /// how close the player must be (metres)

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip placeSound;

    
   
    /// Read-only info for other scripts (SlotZone, Level5Manager)

  

    public int ItemID => itemID;
    public string DisplayName => displayName;
    public Color ItemColor => itemColor;

    /// Name wrapped in TextMeshPro rich text, so it appears in its own colour in the HUD.
    /// Result looks like: <color=#FF0000>Red Skull</color>
    public string ColoredName =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(itemColor) + ">" + displayName + "</color>";

   
    /// Private state
    /// "static" = shared by ALL skulls. Tracks which skull is currently showing the shared prompt,
    /// so one skull doesn't hide the prompt that another skull is still using.
    /// Private state
   
    private static ItemPickUp promptOwner;

    private bool isPlaced;       /// true while the skull sits on the table (stops pickup logic)
    private Transform player;

   

    /// Unity lifecycle
   
    private void Awake()
    {
        /// Prompt starts hidden
        if (promptUI != null) promptUI.SetActive(false);

        /// Paint every material on the skull with the item colour.
        /// "_BaseColor" is used by URP shaders, "_Color" by the built-in shaders,
        /// so we check which one exists (HasProperty) and set whichever is there.
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
        /// Cache the player once (cheaper than searching every frame)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        /// Drop the skull onto the floor at the start (ray starts 2 units above it)
        if (snapOnStart) SnapToSurface(2f);
    }

    private void Update()
    {
        /// Nothing to do if it's already on the table, or the setup is missing
        if (isPlaced || player == null || Level5Manager.Instance == null) return;

        /// Distance to the player (floor distance only, so height differences don't matter)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        bool inRange = Vector3.Distance(a, b) <= pickupRadius;

        /// Show or hide the "Press E" prompt depending on range
        UpdatePrompt(inRange);

        /// In range and E pressed -> collect
        if (inRange && InteractPressed())
        {
            /// Ask the manager to add it to the inventory. If it refuses, do nothing.
            if (!Level5Manager.Instance.TryPickUp(this)) return;

            /// PlayClipAtPoint makes a temporary sound object, so it still plays
            /// after this skull is hidden on the next line
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            gameObject.SetActive(false);   /// OnDisable hides the prompt
        }
    }

  
    /// Floor snapping
  
    /// Drops the skull straight down until its lowest point touches the surface below it
    private void SnapToSurface(float rayLift)
    {
        /// If the mask is empty (value 0), treat it as "everything" so the ray still hits something
        int mask = surfaceMask.value == 0 ? ~0 : surfaceMask.value;

        /// Start the ray a little ABOVE the skull and shoot it downward.
        /// RaycastAll returns every hit, not just the first, so we can skip unwanted ones below.
        Vector3 origin = transform.position + Vector3.up * rayLift;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, rayLift + 5f,
                                               mask, QueryTriggerInteraction.Ignore);

        /// RaycastAll results are NOT guaranteed to be in order, so sort nearest first
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;          /// our own collider
            if (hit.collider.CompareTag("Player")) continue;                    /// the player
            if (hit.collider.GetComponentInParent<ItemPickUp>() != null) continue; /// other skulls
            if (hit.normal.y < 0.5f) continue;                                  /// only flat, upward-facing surfaces

            /// First valid surface found: move the skull so its BOTTOM touches that surface
            if (GetVisualBounds(out Bounds b))
            {
                /// Move up/down so the bottom of the model sits on the surface
                /// (b.min.y = the lowest point of the skull right now)
                float move = (hit.point.y + surfaceGap) - b.min.y;
                transform.position += Vector3.up * move;
            }
            return;   /// done, ignore the remaining hits
        }

        /// The loop finished without finding any valid surface
        Debug.LogWarning(name + ": no floor found below it, left where it was.");
    }

    /// A box around the skull's visible meshes (ignores lights and 3D text)
    private bool GetVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;   /// only real meshes
            if (r.GetComponent<TMP_Text>() != null) continue;                 /// skip 3D text labels

            if (!found) { bounds = r.bounds; found = true; }   /// first mesh starts the box
            else bounds.Encapsulate(r.bounds);                 /// later meshes grow the box
        }
        return found;
    }

    
    /// Prompt ("Press E to Collect")
    /// Only the skull that showed the prompt may hide it
    private void UpdatePrompt(bool inRange)
    {
        if (promptUI == null) return;

        if (inRange)
        {
            promptOwner = this;           /// claim the prompt
            promptUI.SetActive(true);
        }
        else if (promptOwner == this)     /// out of range, AND we were the one showing it
        {
            promptOwner = null;
            promptUI.SetActive(false);
        }
    }


    /// Hide the prompt, but only if THIS skull is the one currently showing it
    private void HidePrompt()
    {
        if (promptOwner == this)
        {
            promptOwner = null;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    /// Runs when the skull is collected, hidden, or destroyed
    private void OnDisable()
    {
        HidePrompt();
    }

 
    /// Works with both the old and the new Unity Input System
    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

  
    /// Called by other scripts
    /// Called when placed on the table (by SlotZone.Place)
    public void PlaceAt(Transform anchor)
    {
        isPlaced = true;      /// stop the pickup logic in Update
        HidePrompt();

        /// Keep upright: copy only the left/right turn (Y) from the anchor, ignore tilt
        Quaternion rot = keepUpright ? Quaternion.Euler(0f, anchor.eulerAngles.y, 0f) : anchor.rotation;
        transform.SetPositionAndRotation(anchor.position, rot);
        gameObject.SetActive(true);   /// it was hidden while in the inventory
        SnapToSurface(0.5f);   /// rest on the table top

        if (placeSound != null)
            AudioSource.PlayClipAtPoint(placeSound, transform.position);
    }

    /// Called by ItemSpawner to set a random position
    public void RespawnAt(Vector3 newPos, Quaternion newRot)
    {
        isPlaced = false;     /// make it collectable again

        Quaternion rot = keepUpright ? Quaternion.Euler(0f, newRot.eulerAngles.y, 0f) : newRot;
        transform.SetPositionAndRotation(newPos, rot);
        gameObject.SetActive(true);
        SnapToSurface(2f);     /// drop onto the floor
    }

    /// After a wrong order: the skull goes back into the player's hands (hidden)
    public void ReturnToHands()
    {
        isPlaced = false;
        HidePrompt();
        gameObject.SetActive(false);   /// hidden, Level5Manager holds it in the inventory list
    }
}