using UnityEngine;

public class ExactPlateWeightRule : PressurePlateRule
{
    [SerializeField] private PressurePlate targetPlate;

    public override bool IsValid()
    {
        return targetPlate != null && targetPlate.HasCorrectWeight;
    }

    public override string GetFailureMessage()
    {
        return $"{targetPlate.name} must hold its required weight.";
    }
}
