using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backrooms.Core
{
    /// <summary>
    /// Trigger zone that loads the next scene when the player enters it,
    /// but only once the required puzzle is solved.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExit : MonoBehaviour
    {
        [SerializeField] private Puzzle requiredPuzzle;          // leave empty = always open
        [SerializeField] private string sceneToLoad = "Level2";  // must match the Scene List name
        [SerializeField] private string playerTag = "Player";

        private bool loading;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;           // auto-ticks Is Trigger when added
        }

        private void OnTriggerEnter(Collider other) => TryExit(other);
        private void OnTriggerStay(Collider other) => TryExit(other);   // in case the player is already inside when it unlocks

        private void TryExit(Collider other)
        {
            if (loading) return;
            if (requiredPuzzle != null && !requiredPuzzle.IsSolved) return;

            // The player's collider is on a child (PlayerObj), so check the object with the Rigidbody
            Transform player = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            if (!player.CompareTag(playerTag)) return;

            loading = true;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}