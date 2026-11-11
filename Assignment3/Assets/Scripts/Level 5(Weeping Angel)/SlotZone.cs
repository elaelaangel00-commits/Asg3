using TMPro;
using UnityEngine;

/// <summary>
/// One slot on the table where the player places a skull.
///
/// Each slot knows:
///  - which item ID belongs in it (requiredItemID),
///  - where the item should sit (itemAnchor),
///  - which skull is currently in it (placedItem).
///
/// This script is "dumb" on purpose: it only stores state and reports it.
/// TableDeposit does the thinking (player distance, E key, checking if the order is right).
///
/// Setup: put this on each of the 3 slot objects on the table.
/// No collider needed. TableDeposit handles the player's distance and the E key.
/// </summary>
public class SlotZone : MonoBehaviour
{

    /// Inspector settings  

    [Tooltip("Which item belongs here. Slot 1 = item ID 1, and so on.")]
    [SerializeField] private int requiredItemID = 1;   // compared against ItemPickUp.ItemID

    [Tooltip("Where the item will sit on the table. If empty, this object's position is used.")]
    [SerializeField] private Transform itemAnchor;

    [Header("Label (optional)")]
    [SerializeField] private TMP_Text label;                  /// 3D text above the slot
    [SerializeField] private string labelText = "Red Skull";  /// which skull belongs here
    [SerializeField] private Color labelColor = Color.red;    /// match the skull's colour
    [SerializeField] private bool faceCamera = true;          /// label turns toward the player

    
    /// Private state

    private ItemPickUp placedItem;   /// the skull currently in this slot (null = empty)
    private Camera cam;              /// cached, used to rotate the label

    
    /// Read-only status, used by TableDeposit
    
    /// True if any skull is in this slot
    public bool IsOccupied => placedItem != null;

    /// True only if a skull is in the slot AND it is the right one.
    /// The null check comes first, so we never read .ItemID from a missing item.
    public bool IsCorrect => placedItem != null && placedItem.ItemID == requiredItemID;

   
    /// Unity lifecycle
 
    private void Start()
    {
        /// Find the camera: the MainCamera tag first, then any camera in the scene (fallback)
        cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        /// Apply the label text and colour from the Inspector
        if (label != null)
        {
            label.text = labelText;
            label.color = labelColor;
        }
    }

    private void LateUpdate()
    {
        /// Keep the text readable from wherever the player stands.
        /// LateUpdate runs after the camera has moved this frame, so the label never lags behind.
        if (faceCamera && label != null && cam != null)
        {
            /// Direction from the camera TO the label, then rotate the label to point that way.
            /// (Pointing away from the camera, because TMP 3D text reads correctly from behind.)
            label.transform.rotation = Quaternion.LookRotation(
                label.transform.position - cam.transform.position);
        }
    }

  
    /// Actions called by TableDeposit
    /// Puts a skull in this slot and moves it onto the table
    public void Place(ItemPickUp item)
    {
        placedItem = item;
        /// Use the anchor if one is assigned, otherwise fall back to this object's own transform
        item.PlaceAt(itemAnchor != null ? itemAnchor : transform);
    }

    /// Used after a wrong sequence: empties the slot and gives back the skull that was on it
    public ItemPickUp ClearAndTakeItem()
    {
        ItemPickUp item = placedItem;   /// remember it before clearing
        placedItem = null;              /// slot is now empty

        /// The skull handles its own "go back to the player" behaviour
        if (item != null) item.ReturnToHands();
        return item;   /// returned so the caller can give it back to the inventory (null if the slot was empty)
    }
}