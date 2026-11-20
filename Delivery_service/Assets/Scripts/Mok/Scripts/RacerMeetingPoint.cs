using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class RacerMeetingPoint : MonoBehaviour
    {
        public GameObject prefab;
        public RacerNPCInteraction actor;
        float respawnAt;
        void Update()
        {
            if (actor) { respawnAt = Time.time + 5; return; }
            if (Time.time < respawnAt || !prefab || (RaceManager.Instance && RaceManager.Instance.IsRaceActive)) return;
            actor = Instantiate(prefab, transform.position, transform.rotation, transform).GetComponent<RacerNPCInteraction>();
            if (MechanicMission.Current) MechanicMission.Current.racerNpc = actor.transform;
        }
    }
}
