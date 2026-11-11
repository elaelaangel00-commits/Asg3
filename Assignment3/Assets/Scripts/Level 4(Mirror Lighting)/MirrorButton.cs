
using System.Collections;
using UnityEngine;

public class MirrorButton : MonoBehaviour
{
    public enum ButtonAction
    {
        RotateMirror,
        ActivateLaser
    }

    [Header("Button Action")]
    public ButtonAction buttonAction = ButtonAction.RotateMirror;

    [Header("Mirror Settings")]
    public Transform mirror;
    public float rotateAngle = 45f;
    public float rotationDuration = 0.2f;

    [Header("Laser Settings")]
    public LaserPuzzle laserPuzzle;

    private Transform trackedMirror;
    private Quaternion targetRotation;
    private Coroutine rotationCoroutine;

    public string PromptText
    {
        get
        {
            if (buttonAction == ButtonAction.RotateMirror)
                return "Press E to rotate mirror";

            if (laserPuzzle != null && laserPuzzle.IsLaserOn)
                return "Laser activated";

            return "Press E to activate laser";
        }
    }

    public void Interact()
    {
        if (buttonAction == ButtonAction.RotateMirror)
        {
            RotateMirror();
        }
        else if (laserPuzzle != null)
        {
            laserPuzzle.ActivateLaser();
        }
        else
        {
            Debug.LogWarning("LaserPuzzle not assigned!");
        }
    }

    public void RotateMirror()
    {
        if (mirror == null)
        {
            Debug.LogWarning("Mirror not assigned to " + name);
            return;
        }

        if (trackedMirror != mirror)
        {
            trackedMirror = mirror;
            targetRotation = mirror.rotation;
        }

        // Rotate 45 degrees around WORLD Y.
        // Repeated presses continue in the same direction.
        targetRotation =
            Quaternion.AngleAxis(rotateAngle, Vector3.up)
            * targetRotation;

        if (rotationCoroutine != null)
            StopCoroutine(rotationCoroutine);

        rotationCoroutine = StartCoroutine(
            AnimateRotation()
        );

        Debug.Log(name + " rotating " + mirror.name);
    }

    private IEnumerator AnimateRotation()
    {
        Quaternion start = mirror.rotation;
        Quaternion end = targetRotation;

        float duration = Mathf.Max(
            0.01f,
            rotationDuration
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            t = t * t * (3f - 2f * t);

            mirror.rotation = Quaternion.Slerp(
                start,
                end,
                t
            );

            yield return null;
        }

        mirror.rotation = end;

        Physics.SyncTransforms();

        rotationCoroutine = null;
    }
}
