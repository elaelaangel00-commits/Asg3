using UnityEngine;

public abstract class PressurePlateRule : MonoBehaviour
{
    public abstract bool IsValid();
    public abstract string GetFailureMessage();
}
