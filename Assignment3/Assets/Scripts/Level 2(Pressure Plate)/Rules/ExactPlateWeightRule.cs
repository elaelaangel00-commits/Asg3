using UnityEngine;

public class ExactPlateWeightRule : PressurePlateRule
{
    [SerializeField] private PressurePlate targetPlate;

    public override bool IsValid()
    {
        return targetPlate != null && targetPlate.HasCorrectWeight;
    }
}
