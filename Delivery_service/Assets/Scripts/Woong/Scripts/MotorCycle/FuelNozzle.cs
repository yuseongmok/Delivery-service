using UnityEngine;

public class FuelNozzle : MonoBehaviour
{
    public enum NozzleState { OnPump, InHand, Refueling }
    public NozzleState currentState = NozzleState.OnPump;

    public Transform pumpResetPoint;
    public Transform playerHandPoint;
    public FuelCap currentTargetCap;

    public LineRenderer hoseRender;
    public Transform hoseStartPoint;
    public Transform hoseEndPoint;
    public int hoseResoulution = 20;
    public float hoseSag = 2f;
    public float refuelSpeed = 15f;

    void Start()
    {
        if (hoseRender != null)
        {
            hoseRender.positionCount = hoseResoulution;
            hoseRender.useWorldSpace = true;
        }
        ReturnPump();
    }

    private void Update()
    {
        DrawHose();

        if (currentState == NozzleState.Refueling && currentTargetCap != null)
        {
            if (currentTargetCap.bikeFuelSystem.currentFuel < currentTargetCap.bikeFuelSystem.maxFuel)
            {
                currentTargetCap.bikeFuelSystem.AddFuel(refuelSpeed * Time.deltaTime);
            }
        }
    }

    private void DrawHose()
    {
        if (hoseRender == null || hoseStartPoint == null || hoseEndPoint == null) return;
        Vector3 start = hoseStartPoint.position;
        Vector3 end = hoseEndPoint.position;
        Vector3 middle = (start + end) / 2f + Vector3.down * hoseSag;

        for (int i = 0; i < hoseResoulution; i++)
        {
            float t = i / (float)(hoseResoulution - 1);
            Vector3 point = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * middle + Mathf.Pow(t, 2) * end;
            hoseRender.SetPosition(i, point);
        }
    }

    public void PickUpNozzle()
    {
        if (currentTargetCap != null)
        {
            MotorcycleController bike = currentTargetCap.bikeFuelSystem.GetComponent<MotorcycleController>();
            if (bike != null)
            {
                bike.isRefueling = false;
                bike.UpdateUIVisibility();
            }
        }

        currentState = NozzleState.InHand;
        currentTargetCap = null;
        transform.SetParent(playerHandPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.layer = 2;
    }

    public void AttachToBike(FuelCap cap)
    {
        currentState = NozzleState.Refueling;
        currentTargetCap = cap;
        transform.SetParent(cap.NozzleAttachPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.layer = LayerMask.NameToLayer("Motorcycle");

        MotorcycleController bike = currentTargetCap.bikeFuelSystem.GetComponent<MotorcycleController>();
        if (bike != null)
        {
            bike.isRefueling = true;
            bike.UpdateUIVisibility();
        }
    }

    public void ReturnPump()
    {
        currentState = NozzleState.OnPump;
        currentTargetCap = null;
        transform.SetParent(pumpResetPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.layer = LayerMask.NameToLayer("Motorcycle");
    }
}