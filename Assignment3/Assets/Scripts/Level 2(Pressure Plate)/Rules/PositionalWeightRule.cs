using UnityEngine;

public enum PlateRelationship
{
    Adjacent,
    NotAdjacent,
    Opposite
}

public class PositionalWeightRule : PressurePlateRule
{
    [SerializeField] private PlateRelationship relationship;

    [SerializeField] private PressurePlate plateA;
    [SerializeField] private int weightA;

    [SerializeField] private PressurePlate plateB;
    [SerializeField] private int weightB;

    public override bool IsValid()
    {
        if (plateA == null || plateB == null)
        {
            return true;
        }

        bool matchesNormal = plateA.CurrentWeight == weightA && plateB.CurrentWeight == weightB;

        bool matchesReversed = plateA.CurrentWeight == weightB && plateB.CurrentWeight == weightA;

        bool combinationExists = matchesNormal || matchesReversed;

        return relationship switch
        {
           PlateRelationship.Adjacent => combinationExists,
           PlateRelationship.NotAdjacent => !combinationExists,
           PlateRelationship.Opposite => combinationExists,
           _ => true
        };
    }

    public override string GetFailureMessage()
    {
        return relationship switch
        {
            PlateRelationship.Adjacent => $"The {weightA} kg and {weightB} kg loads must be positioned correctly.",
            PlateRelationship.NotAdjacent => $"The {weightA} kg and {weightB} kg loads cannot be beside each other.",
            PlateRelationship.Opposite => $"The {weightA} kg and {weightB} kg loads must face each other.",
            _ => "The weight placement is incorrect."
        };
    }
}
