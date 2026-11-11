using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif

/// <summary>
/// The "brain" of the table puzzle.
///
/// What it does:
///  1. Finds the slot nearest to the player (within interactRadius).
///  2. When the player presses E, takes the selected skull from the inventory
///     and places it in that slot.
///  3. Once every slot is filled, checks whether each skull is in its correct slot.
///       - All correct -> puzzle solved: angels vanish and the exit door unlocks.
///       - Any wrong   -> short delay, then all skulls return to the player's inventory.
///
/// Division of work between the puzzle scripts:
///  - SlotZone        : stores what is in ONE slot (dumb data holder)
///  - ItemPickUp      : the skull itself (moves, hides, plays sounds)
///  - Level5Manager   : inventory, HUD and game state
///  - TableDeposit    : the rules (this script)
///
/// Setup: put this on ONE empty GameObject (e.g. "TableManager"). It needs no collider.
/// When the player stands near a slot and presses E, the selected item is placed there.
/// Once all 3 slots are filled, it checks the sequence.
/// </summary>
public class TableDeposit : MonoBehaviour
{
    
    /// Inspector settings
    [Tooltip("The 3 slots, in order: slot 1, slot 2, slot 3.")]
    [SerializeField] private SlotZone[] slots;

    [SerializeField] private ExitDoor exitDoor;   /// unlocked when the puzzle is solved

    [Tooltip("How close the player must be to a slot to use it.")]
    [SerializeField] private float interactRadius = 2.5f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip placeSound;     /// a skull is placed
    [SerializeField] private AudioClip correctSound;   /// puzzle solved
    [SerializeField] private AudioClip wrongSound;     /// wrong order

    [SerializeField] private float wrongResetDelay = 2f;   /// seconds the player sees the wrong result

 
    /// Private state
 
    private Transform player;
    private SlotZone lastNearSlot;   /// remembers the previous nearest slot, so the hint shows only once
    private bool solved;             /// puzzle finished, stop all input
    private bool resetting;          /// true during the wrong-order delay, blocks input

   
    // Unity lifecycle
   
    private void Start()
    {
        /// Cache the player once. Log an error if the tag is missing, because nothing would work.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("TableDeposit: no object with the tag 'Player' found!");
    }

    private void Update()
    {
        /// Do nothing if the puzzle is finished, a reset is in progress, or the setup is missing
        if (solved || resetting || player == null || Level5Manager.Instance == null) return;

        SlotZone nearest = GetNearestSlot();

        /// Player is not close to any slot
        if (nearest == null)
        {
            lastNearSlot = null;   // forget it, so the hint shows again next time the player returns
            return;
        }

        /// Show a hint once when the player walks up to a slot
        /// (only when we have something to place and the slot is free)
        if (nearest != lastNearSlot)
        {
            lastNearSlot = nearest;
            if (Level5Manager.Instance.HasHeldItem && !nearest.IsOccupied)
                Level5Manager.Instance.ShowMessage("Press E to place " + Level5Manager.Instance.HeldItem.DisplayName);
        }

        /// E pressed while near a slot -> try to place the selected skull there
        if (InteractPressed()) TryPlace(nearest);
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

  
    /// Finding the slot
    /// The closest slot within range, or null if none is close enough
    private SlotZone GetNearestSlot()
    {
        SlotZone best = null;
        float bestDist = interactRadius;   /// start at the max range: anything further is ignored

        foreach (SlotZone s in slots)
        {
            /// Floor distance only (ignore height)
            Vector3 a = s.transform.position; a.y = 0f;
            Vector3 b = player.position; b.y = 0f;
            float d = Vector3.Distance(a, b);

            /// "Closest so far" pattern: keep the slot with the smallest distance
            if (d <= bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        return best;
    }

   
    /// Placing a skull
    public void TryPlace(SlotZone slot)
    {
        Level5Manager manager = Level5Manager.Instance;
        if (manager == null || solved || resetting) return;

        /// Guard 1: nothing to place
        if (!manager.HasHeldItem)
        {
            manager.ShowMessage("You aren't carrying anything.");
            return;
        }

        /// Guard 2: slot already has a skull
        if (slot.IsOccupied)
        {
            manager.ShowMessage("This slot is already taken.");
            return;
        }

        /// Take the selected item out of the player's inventory and put it on the table
        ItemPickUp item = manager.TakeHeldItem();
        slot.Place(item);
        PlaySound(placeSound, slot.transform.position);

       /// Update the HUD counter
        int placed = CountPlaced();
        manager.SetPlacedCount(placed);

        /// Only judge the sequence when every slot is filled
        if (placed >= slots.Length) CheckSequence();
    }

    /// How many slots currently have a skull in them
    private int CountPlaced()
    {
        int count = 0;
        foreach (SlotZone s in slots)
            if (s.IsOccupied) count++;
        return count;
    }

    
    /// Checking the result
   
    private void CheckSequence()
    {
        /// Every slot must hold ITS OWN skull (SlotZone.IsCorrect compares the item ID)
        bool allCorrect = true;
        foreach (SlotZone s in slots)
            if (!s.IsCorrect) allCorrect = false;

        if (allCorrect)
        {
            solved = true;   /// blocks further input from now on
            PlaySound(correctSound, transform.position);
            Level5Manager.Instance.PuzzleSolved();   /// removes the angels, updates the HUD

            if (exitDoor != null) exitDoor.Unlock();
            else Debug.LogError("ExitDoor is not assigned on TableDeposit!");
        }
        else
        {
            /// Wrong order: run the reset as a coroutine so it can wait before clearing the table
            StartCoroutine(WrongSequenceRoutine());
        }
    }

    /// Coroutine = a function that can pause. Show the result, wait, then reset the table.
    private IEnumerator WrongSequenceRoutine()
    {
        resetting = true;   /// block input during the delay, so the player can't place more skulls
        PlaySound(wrongSound, transform.position);
        Level5Manager.Instance.ShowMessage("Wrong order! The skulls return to your hands...", wrongResetDelay);

        /// Pause here for wrongResetDelay seconds, then continue
        yield return new WaitForSeconds(wrongResetDelay);

        /// Clear every slot and collect the skulls that were on them
        List<ItemPickUp> returned = new List<ItemPickUp>();
        foreach (SlotZone s in slots)
        {
            ItemPickUp item = s.ClearAndTakeItem();
            if (item != null) returned.Add(item);
        }

        /// Put them back in the player's inventory (nothing respawns in the maze)
        Level5Manager.Instance.ReturnItemsToInventory(returned);

        resetting = false;   /// the player can try again
    }

    /// Small helper: plays a sound at a position if a clip was assigned
    private void PlaySound(AudioClip clip, Vector3 position)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position);
    }
}