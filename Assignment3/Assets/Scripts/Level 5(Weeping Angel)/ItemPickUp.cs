using UnityEngine;

// Put this on each of the 3 skulls scattered in the maze.
// Walk close to the skull to collect it. No collider needed.
// The player object must have the tag "Player".
public class ItemPickUp : MonoBehaviour
{
    [Header("Puzzle Info")]
    [Tooltip("Which slot this item belongs in. Slot 1 needs ID 1, slot 2 needs ID 2, etc.")]
    [SerializeField] private int itemID = 1;
    [SerializeField] private string displayName = "Red Skull";

    [Header("Look")]
    [SerializeField] private Color itemColor = Color.red;
    [Tooltip("Tick to paint the skull model with the colour above.")]
    [SerializeField] private bool tintToColor = true;

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 2f;   // how close the player must be

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    public int ItemID => itemID;
    public string DisplayName => displayName;
    public Color ItemColor => itemColor;

    // The name wrapped in its colour, for UI text (TextMeshPro rich text)
    public string ColoredName =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(itemColor) + ">" + displayName + "</color>";

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isPlaced;   // true while sitting on the table
    private Transform player;

    private void Awake()
    {
        // Remember where we started, so a wrong answer can send us back here
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Paint the model with the item colour
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
        else Debug.LogError("ItemPickup: no object with the tag 'Player' found!");
    }

    private void Update()
    {
        if (isPlaced || player == null || Level5Manager.Instance == null) return;

        // Distance on the floor only (ignore height differences)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        if (Vector3.Distance(a, b) > pickupRadius) return;

        if (!Level5Manager.Instance.TryPickUp(this)) return;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // Hide the item from the floor
        gameObject.SetActive(false);
    }

    // Called by SlotZone when the player puts this item on the table
    public void PlaceAt(Transform anchor)
    {
        isPlaced = true;
        transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        gameObject.SetActive(true);
    }

    // Called after a wrong sequence: send the item back to where it was in the maze
    public void ResetToStart()
    {
        isPlaced = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        gameObject.SetActive(true);
    }
}