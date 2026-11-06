using TMPro;
using UnityEngine;

// Put this on each of the 3 slot objects on the table.
// No collider needed. TableDeposit handles the player's distance and the E key.
public class SlotZone : MonoBehaviour
{
    [Tooltip("Which item belongs here. Slot 1 = item ID 1, and so on.")]
    [SerializeField] private int requiredItemID = 1;

    [Tooltip("Where the item will sit on the table. If empty, this object's position is used.")]
    [SerializeField] private Transform itemAnchor;

    [Header("Label (optional)")]
    [SerializeField] private TMP_Text label;                  // 3D text above the slot
    [SerializeField] private string labelText = "Red Skull";  // which skull belongs here
    [SerializeField] private Color labelColor = Color.red;    // match the skull's colour
    [SerializeField] private bool faceCamera = true;          // label turns toward the player

    private ItemPickUp placedItem;
    private Camera cam;

    public bool IsOccupied => placedItem != null;
    public bool IsCorrect => placedItem != null && placedItem.ItemID == requiredItemID;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        if (label != null)
        {
            label.text = labelText;
            label.color = labelColor;
        }
    }

    private void LateUpdate()
    {
        // Keep the text readable from wherever the player stands
        if (faceCamera && label != null && cam != null)
        {
            label.transform.rotation = Quaternion.LookRotation(
                label.transform.position - cam.transform.position);
        }
    }

    public void Place(ItemPickUp item)
    {
        placedItem = item;
        item.PlaceAt(itemAnchor != null ? itemAnchor : transform);
    }

    // Used after a wrong sequence
    public void ClearAndReturnItem()
    {
        if (placedItem != null) placedItem.ResetToStart();
        placedItem = null;
    }
}