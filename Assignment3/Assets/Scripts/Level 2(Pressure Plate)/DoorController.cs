using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private GameObject doorObject;
    [SerializeField] private PressurePlatePuzzle pressurePlatePuzzle;
    [SerializeField] private AudioSource doorOpenAudio;

    private void OnEnable()
    {
        pressurePlatePuzzle.PuzzleSolved += OpenDoor;
    }

    private void OnDisable()
    {
        pressurePlatePuzzle.PuzzleSolved -= OpenDoor;
    }

    private void OpenDoor()
    {
        if (doorObject != null)
        {
            doorObject.SetActive(false);
        }

        if (doorOpenAudio != null)
        {
            doorOpenAudio.Play();
        }
    }
}
