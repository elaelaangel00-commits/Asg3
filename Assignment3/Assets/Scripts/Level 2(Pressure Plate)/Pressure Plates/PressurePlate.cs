using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField] private int requiredWeight;
    [SerializeField] private Renderer plateRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color correctColor = Color.green;

    [SerializeField] private AudioSource plateAudio;
    [SerializeField] private AudioClip plateActivateClip;
    [SerializeField] private AudioClip plateDeactivateClip;

    private readonly List<WeightedObject> objectsOnPlate = new List<WeightedObject>();

    public int CurrentWeight { get; private set; }
    public bool HasCorrectWeight => CurrentWeight == requiredWeight;

    public event System.Action<PressurePlate> StateChanged;

    private void Start()
    {
        RefreshVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out WeightedObject weightedObject))
        {
            return;
        }

        if (weightedObject.IsCarried)
        {
            return;
        }

        if (objectsOnPlate.Contains(weightedObject))
        {
            return;
        }

        objectsOnPlate.Add(weightedObject);
        RecalculateWeight();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out WeightedObject weightedObject))
        {
            return;
        }

        if (objectsOnPlate.Remove(weightedObject))
        {
            RecalculateWeight();
        }
    }

    private void RecalculateWeight()
    {
        CurrentWeight = 0;
        
        foreach (WeightedObject weightedObject in objectsOnPlate)
        {
            CurrentWeight += weightedObject.Weight;
        }

        RefreshVisual();
        StateChanged?.Invoke(this);
    }

    private void RefreshVisual()
    {
        if (plateRenderer == null)
        {
            plateRenderer.material.color = HasCorrectWeight ? correctColor : inactiveColor;
        }

        PlayPlateSound();
    }

    private void PlayPlateSound()
    {
        if (plateAudio == null)
        {
            return;
        }

        AudioClip clip = HasCorrectWeight ? plateActivateClip : plateDeactivateClip;

        if (clip != null)
        {
            plateAudio.PlayOneShot(clip);
        }
    }
}
