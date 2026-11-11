using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomly places the skulls (ItemPickUp objects) on the spawn points at the start
/// of the level, so the puzzle is different every time the player plays.
///
/// How it works:
///  1. Collect the skulls (from the Inspector list, or find them automatically).
///  2. For each skull, pick a random spawn point that hasn't been used yet.
///  3. Move the skull there by calling ItemPickUp.RespawnAt().
///
/// Setup: add this to an empty GameObject, assign the spawn point Transforms,
/// and make sure there are at least as many spawn points as skulls.
/// </summary>


public class ItemSpawner : MonoBehaviour
{
    /// Singleton: other scripts can call ItemSpawner.Instance.RespawnAllItemsRandomly()
    public static ItemSpawner Instance;

    /// Inspector settings
  
    [Header("Spawner Setup")]
    [Tooltip("Optional. If any slot is empty, all skulls in the scene are found automatically.")]
    [SerializeField] private List<ItemPickUp> itemsToSpawn;   /// the skulls to shuffle
    [SerializeField] private List<Transform> spawnPoints;     /// possible locations (empty objects)

    [Header("Options")]
    [SerializeField] private float heightOffset = 0f;         /// lifts skulls above the point, so they don't clip into the floor

    
    // Unity lifecycle
  

    private void Awake()
    {
        // Keep only ONE spawner. If another already exists, destroy this duplicate.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// Start can be a coroutine (IEnumerator) so it is allowed to "wait".
    private IEnumerator Start()
    {
        /// Wait one frame so no other script can move the skulls back afterwards
        /// (for example, a script that sets the skulls' starting position in its own Start)
        yield return null;
        RespawnAllItemsRandomly();
    }

  
    // Main logic
   
    /// Shuffles every skull onto a different random spawn point.
    /// Public, so it can be called again later (for example, on a puzzle reset).
 
    public void RespawnAllItemsRandomly()
    {
        /// STEP 1: build the list of skulls to move.
        /// Use the Inspector list if every slot is filled, otherwise find the skulls automatically.
        List<ItemPickUp> items = new List<ItemPickUp>();
        foreach (ItemPickUp i in itemsToSpawn)
            if (i != null) items.Add(i);   // skip empty slots

        /// Fall back to auto-find if the list is empty, or if some slots were empty
        /// (fewer valid items than slots means something was not assigned).
        if (items.Count == 0 || items.Count < itemsToSpawn.Count)
        {
            // NOTE: this only finds ACTIVE objects, so skulls that are hidden are not found.
            items = new List<ItemPickUp>(FindObjectsByType<ItemPickUp>(FindObjectsSortMode.None));
            Debug.Log("ItemSpawner: list had empty slots, found " + items.Count + " skulls automatically");
        }

        /// STEP 2: safety check. We need one unique spawn point per skull.
        if (spawnPoints == null || spawnPoints.Count < items.Count)
        {
            Debug.LogError("ItemSpawner: Not enough spawn points! Need at least " + items.Count);
            return;   /// stop here, the skulls stay where they were placed in the editor
        }

        /// STEP 3: work on a COPY of the spawn points, so we can remove used ones
        /// without changing the original list in the Inspector.
        List<Transform> available = new List<Transform>(spawnPoints);

        foreach (ItemPickUp item in items)
        {
            /// Pick a random spawn point from the ones still free.
            /// (Random.Range with ints: the max is exclusive, so this is always a valid index.)
            int index = Random.Range(0, available.Count);
            Transform point = available[index];

            /// Move the skull there (position slightly raised, and the same rotation as the point)
            item.RespawnAt(point.position + Vector3.up * heightOffset, point.rotation);
            Debug.Log("ItemSpawner: " + item.name + " -> " + point.name);

            /// Remove the used point so no two skulls share the same spot
            available.RemoveAt(index);
        }
    }

    
    /// Editor helper

    /// Draws a yellow circle on every spawn point in the Scene view (editor only, not in the build)
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.yellow;
        foreach (Transform p in spawnPoints)
            if (p != null) Gizmos.DrawWireSphere(p.position + Vector3.up * heightOffset, 0.5f);
    }
}