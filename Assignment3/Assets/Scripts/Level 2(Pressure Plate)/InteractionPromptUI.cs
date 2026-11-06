using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ObjectPickup objectPickup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float interactionRange = 3f;

    [SerializeField] private LayerMask detectionLayers = ~0;

    [SerializeField] private string pickupInstruction = "[E] Pick up";
    [SerializeField] private string dropInstruction = "[E] Place";

    private void Update()
    {
        if (promptText == null)
        {
            return;
        }

        if (Time.timeScale == 0f)
        {
            promptText.text = string.Empty;
            return;
        }

        if (objectPickup != null && objectPickup.CarriedObject != null)
        {
            promptText.text = $"Carrying: {objectPickup.CarriedObject.Weight} kg\n" + dropInstruction;
            return;
        }

        promptText.text = string.Empty;

        if (playerCamera == null)
        {
            return;
        }

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactionRange, detectionLayers, QueryTriggerInteraction.Ignore))
        {
            WeightedObject weight = hit.collider.GetComponentInParent<WeightedObject>();

            if (weight != null && !weight.IsCarried)
            {
                promptText.text = $"Weight: {weight.Weight} kg\n" + pickupInstruction;
            }
        }
    }
}
