using UnityEngine;

public class PizzaPackage : MonoBehaviour
{
    public bool IsPackaged { get; private set; }

    public void SetPackaged(bool value)
    {
        IsPackaged = value;
    }
}
