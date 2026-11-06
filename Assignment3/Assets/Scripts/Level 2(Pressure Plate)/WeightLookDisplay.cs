using UnityEngine;

public class WeightLookDisplay : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PuzzleStatusUI statusUI;

    [SerializeField, Min(0.1f)]
    private float displayRange = 3f;

    [SerializeField] private LayerMask detectionLayers = ~0;

    private void Update()
    {
        if (playerCamera == null || statusUI == null)
        {
            return;
        }

        string message = string.Empty;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, displayRange, detectionLayers, QueryTriggerInteraction.Ignore))
        {
            WeightedObject weight = hit.collider.GetComponentInParent<WeightedObject>();

            if (weight != null && !weight.IsCarried)
            {
                message = $"Weight: {weight.Weight} kg";
            }
        }

        statusUI.SetObjectInfo(message);
    }

    private void OnDisable()
    {
        if (statusUI != null)
        {
            statusUI.SetObjectInfo(string.Empty);
        }
    }
}
