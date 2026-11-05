using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Slides open when its puzzle is solved, and closes again if the puzzle becomes unsolved.
    /// Works with ANY Puzzle subclass, so every room can reuse it.
    /// </summary>
    public class PuzzleDoor : MonoBehaviour
    {
        [SerializeField] private Puzzle puzzle;
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float speed = 1.5f;          // units per second
        [SerializeField] private AudioSource doorAudio;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen;

        private void Awake()
        {
            closedPosition = transform.localPosition;
            openPosition = closedPosition + openOffset;
        }

        private void OnEnable()
        {
            if (puzzle == null) return;
            puzzle.Solved += HandleSolved;
            puzzle.Unsolved += HandleUnsolved;
        }

        private void OnDisable()
        {
            if (puzzle == null) return;
            puzzle.Solved -= HandleSolved;
            puzzle.Unsolved -= HandleUnsolved;
        }

        private void HandleSolved(Puzzle p)
        {
            isOpen = true;
            if (doorAudio != null) doorAudio.Play();
        }

        private void HandleUnsolved(Puzzle p)
        {
            isOpen = false;
            if (doorAudio != null) doorAudio.Play();
        }

        private void Update()
        {
            Vector3 target = isOpen ? openPosition : closedPosition;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
        }
    }
}
