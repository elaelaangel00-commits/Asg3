using System;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Weight")]
    [SerializeField, Min(1)] private int requiredWeight = 1;

    [Header("Feedback Surface")]
    [SerializeField] private Renderer plateRenderer;

    [Header("Feedback Colours")]
    [SerializeField] private Color neutralColor = Color.gray;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color waitingColor = Color.yellow;
    [SerializeField] private Color correctColor = Color.green;

    private readonly HashSet<Collider> overlappingColliders = new();
    private readonly HashSet<WeightedObject> countedObjects = new();

    private Material feedbackMaterial;
    private Color displayedColor;
    private bool hasDisplayedColor;

    public int CurrentWeight { get; private set; }
    public int ObjectCount { get; private set; }

    public bool IsOccupied => ObjectCount > 0;

    public bool HasCorrectWeight => ObjectCount == 1 && CurrentWeight == requiredWeight;

    public event Action<PressurePlate> StateChanged;

    private void Awake()
    {
        if (plateRenderer != null)
        {
            feedbackMaterial = plateRenderer.material;
        }

        SetFeedback(false, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        overlappingColliders.Add(other);
    }

    private void OnTriggerStay(Collider other)
    {
        overlappingColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        overlappingColliders.Remove(other);
    }

    private void FixedUpdate()
    {
        RecalculateWeight();
    }

    private void RecalculateWeight()
    {
        overlappingColliders.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);

        countedObjects.Clear();

        int newWeight = 0;

        foreach (Collider other in overlappingColliders)
        {
            WeightedObject weight = other.GetComponentInParent<WeightedObject>();

            if (weight == null || weight.IsCarried)
            {
                continue;
            }

            if (countedObjects.Add(weight))
            {
                newWeight += weight.Weight;
            }
        }

        int newCount = countedObjects.Count;

        if (newWeight == CurrentWeight && newCount == ObjectCount)
        {
            return;
        }

        CurrentWeight = newWeight;
        ObjectCount = newCount;

        StateChanged?.Invoke(this);
    }

    public void SetFeedback(bool allPlatesOccupied, bool puzzleSolved)
    {
        Color nextColor;

        if (puzzleSolved)
        {
            nextColor = correctColor;
        }
        else if (!allPlatesOccupied)
        {
            nextColor = neutralColor;
        }
        else
        {
            nextColor = HasCorrectWeight ? waitingColor : incorrectColor;
        }

        if (feedbackMaterial == null)
        {
            return;
        }

        if (hasDisplayedColor && displayedColor == nextColor)
        {
            return;
        }

        displayedColor = nextColor;
        hasDisplayedColor = true;
        feedbackMaterial.color = nextColor;
    }

    private void OnDestroy()
    {
        if (feedbackMaterial != null)
        {
            Destroy(feedbackMaterial);
        }
    }
}
