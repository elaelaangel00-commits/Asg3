using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ObjectPickup objectPickup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private DoorController doorController;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask detectionLayers = ~0;

    [Header("Instructions")]
    [SerializeField] private string pickupInstruction = "[E] Pick up";
    [SerializeField] private string dropInstruction = "[E] Place";
    [SerializeField] private string doorInstruction = "[E] Go to next room";

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
                return;
            }

            if (doorController != null && doorController.CanInteract && doorController.ContainsCollider(hit.collider))
            {
                promptText.text = doorInstruction;
            }
        }
    }
}
