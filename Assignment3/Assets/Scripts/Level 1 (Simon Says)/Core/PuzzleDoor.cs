using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Opens when its puzzle is solved and closes if it becomes unsolved.
    /// Slide = moves by an offset. Swing = rotates around its local Y axis (put it on a hinge pivot).
    /// Works with ANY Puzzle subclass.
    /// </summary>
    public class PuzzleDoor : MonoBehaviour
    {
        public enum OpenMode { Slide, Swing }

        [SerializeField] private Puzzle puzzle;
        [SerializeField] private OpenMode mode = OpenMode.Slide;

        [Header("Slide")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float slideSpeed = 1.5f;        // units per second

        [Header("Swing")]
        [SerializeField] private float swingAngle = 90f;         // negative = swings the other way
        [SerializeField] private float swingSpeed = 120f;        // degrees per second

        [Header("Audio")]
        [SerializeField] private AudioSource doorAudio;

        private Vector3 closedPosition, openPosition;
        private Quaternion closedRotation, openRotation;
        private bool isOpen;

        private void Awake()
        {
            closedPosition = transform.localPosition;
            openPosition = closedPosition + openOffset;
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, swingAngle, 0f);
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

        private void HandleSolved(Puzzle p) => SetOpen(true);
        private void HandleUnsolved(Puzzle p) => SetOpen(false);

        private void SetOpen(bool open)
        {
            if (isOpen == open) return;
            isOpen = open;
            if (doorAudio != null) doorAudio.Play();
        }

        private void Update()
        {
            if (mode == OpenMode.Slide)
            {
                Vector3 target = isOpen ? openPosition : closedPosition;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, slideSpeed * Time.deltaTime);
            }
            else
            {
                Quaternion target = isOpen ? openRotation : closedRotation;
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, swingSpeed * Time.deltaTime);
            }
        }
    }
}