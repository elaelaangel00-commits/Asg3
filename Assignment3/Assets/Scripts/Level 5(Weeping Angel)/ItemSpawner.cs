using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    [Header("Spawner Setup")]
    [Tooltip("Optional. If any slot is empty, all skulls in the scene are found automatically.")]
    [SerializeField] private List<ItemPickUp> itemsToSpawn;
    [SerializeField] private List<Transform> spawnPoints;

    [Header("Options")]
    [SerializeField] private float heightOffset = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait one frame so no other script can move the skulls back afterwards
        yield return null;
        RespawnAllItemsRandomly();
    }

    public void RespawnAllItemsRandomly()
    {
        // Use the list if every slot is filled, otherwise find the skulls automatically
        List<ItemPickUp> items = new List<ItemPickUp>();
        foreach (ItemPickUp i in itemsToSpawn)
            if (i != null) items.Add(i);

        if (items.Count == 0 || items.Count < itemsToSpawn.Count)
        {
            items = new List<ItemPickUp>(FindObjectsByType<ItemPickUp>(FindObjectsSortMode.None));
            Debug.Log("ItemSpawner: list had empty slots, found " + items.Count + " skulls automatically");
        }

        if (spawnPoints == null || spawnPoints.Count < items.Count)
        {
            Debug.LogError("ItemSpawner: Not enough spawn points! Need at least " + items.Count);
            return;
        }

        List<Transform> available = new List<Transform>(spawnPoints);

        foreach (ItemPickUp item in items)
        {
            int index = Random.Range(0, available.Count);
            Transform point = available[index];

            item.RespawnAt(point.position + Vector3.up * heightOffset, point.rotation);
            Debug.Log("ItemSpawner: " + item.name + " -> " + point.name);

            available.RemoveAt(index);
        }
    }

    // Draws a yellow circle on every spawn point in the Scene view
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.yellow;
        foreach (Transform p in spawnPoints)
            if (p != null) Gizmos.DrawWireSphere(p.position + Vector3.up * heightOffset, 0.5f);
    }
}