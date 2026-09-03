using UnityEngine;

public class CrosswalkZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        NPCControl npc = other.GetComponentInParent<NPCControl>();
        if (npc != null)
        {
            npc.isCrossing = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NPCControl npc = other.GetComponentInParent<NPCControl>();
        if (npc != null)
        {
            npc.isCrossing = false;
        }
    }
}
