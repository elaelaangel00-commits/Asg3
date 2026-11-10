using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif

// Put this on ONE empty GameObject (e.g. "TableManager"). It needs no collider.
// When the player stands near a slot and presses E, the selected item is placed there.
// Once all 3 slots are filled, it checks the sequence.
public class TableDeposit : MonoBehaviour
{
    [Tooltip("The 3 slots, in order: slot 1, slot 2, slot 3.")]
    [SerializeField] private SlotZone[] slots;

    [SerializeField] private ExitDoor exitDoor;

    [Tooltip("How close the player must be to a slot to use it.")]
    [SerializeField] private float interactRadius = 2.5f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;

    [SerializeField] private float wrongResetDelay = 2f;

    private Transform player;
    private SlotZone lastNearSlot;
    private bool solved;
    private bool resetting;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("TableDeposit: no object with the tag 'Player' found!");
    }

    private void Update()
    {
        if (solved || resetting || player == null || Level5Manager.Instance == null) return;

        SlotZone nearest = GetNearestSlot();

        if (nearest == null)
        {
            lastNearSlot = null;
            return;
        }

        // Show a hint once when the player walks up to a slot
        if (nearest != lastNearSlot)
        {
            lastNearSlot = nearest;
            if (Level5Manager.Instance.HasHeldItem && !nearest.IsOccupied)
                Level5Manager.Instance.ShowMessage("Press E to place " + Level5Manager.Instance.HeldItem.DisplayName);
        }

        if (InteractPressed()) TryPlace(nearest);
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

    // The closest slot within range, or null if none is close enough
    private SlotZone GetNearestSlot()
    {
        SlotZone best = null;
        float bestDist = interactRadius;

        foreach (SlotZone s in slots)
        {
            Vector3 a = s.transform.position; a.y = 0f;
            Vector3 b = player.position; b.y = 0f;
            float d = Vector3.Distance(a, b);
            if (d <= bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        return best;
    }

    public void TryPlace(SlotZone slot)
    {
        Level5Manager manager = Level5Manager.Instance;
        if (manager == null || solved || resetting) return;

        if (!manager.HasHeldItem)
        {
            manager.ShowMessage("You aren't carrying anything.");
            return;
        }

        if (slot.IsOccupied)
        {
            manager.ShowMessage("This slot is already taken.");
            return;
        }

        // Take the selected item out of the player's inventory and put it on the table
        ItemPickUp item = manager.TakeHeldItem();
        slot.Place(item);
        PlaySound(placeSound, slot.transform.position);

        int placed = CountPlaced();
        manager.SetPlacedCount(placed);

        // Only judge the sequence when every slot is filled
        if (placed >= slots.Length) CheckSequence();
    }

    private int CountPlaced()
    {
        int count = 0;
        foreach (SlotZone s in slots)
            if (s.IsOccupied) count++;
        return count;
    }

    private void CheckSequence()
    {
        bool allCorrect = true;
        foreach (SlotZone s in slots)
            if (!s.IsCorrect) allCorrect = false;

        if (allCorrect)
        {
            solved = true;
            PlaySound(correctSound, transform.position);
            Level5Manager.Instance.PuzzleSolved();

            if (exitDoor != null) exitDoor.Unlock();
            else Debug.LogError("ExitDoor is not assigned on TableDeposit!");
        }
        else
        {
            StartCoroutine(WrongSequenceRoutine());
        }
    }

    private IEnumerator WrongSequenceRoutine()
    {
        resetting = true;
        PlaySound(wrongSound, transform.position);
        Level5Manager.Instance.ShowMessage("Wrong order! The skulls return to your hands...", wrongResetDelay);

        yield return new WaitForSeconds(wrongResetDelay);

        // Clear every slot and collect the skulls that were on them
        List<ItemPickUp> returned = new List<ItemPickUp>();
        foreach (SlotZone s in slots)
        {
            ItemPickUp item = s.ClearAndTakeItem();
            if (item != null) returned.Add(item);
        }

        // Put them back in the player's inventory (nothing respawns in the maze)
        Level5Manager.Instance.ReturnItemsToInventory(returned);

        resetting = false;
    }

    private void PlaySound(AudioClip clip, Vector3 position)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position);
    }
}