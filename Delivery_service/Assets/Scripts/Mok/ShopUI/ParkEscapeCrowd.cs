using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Mok
{
    // 매장 습격 동안 일반 시민만 숨기고, 직접 숨긴 오브젝트만 복원한다.
    internal sealed class ParkEscapeCrowd
    {
        private readonly ShopEscapeChapter chapter;
        private readonly HashSet<GameObject> hiddenCitizens = new();

        public ParkEscapeCrowd(ShopEscapeChapter chapter)
        {
            this.chapter = chapter;
        }

        public void Suppress()
        {
            foreach (var npc in Object.FindObjectsByType<NPCControl>(FindObjectsSortMode.None))
            {
                if (!npc.gameObject.activeSelf || IsStoryActor(npc)) continue;
                hiddenCitizens.Add(npc.gameObject);
                npc.gameObject.SetActive(false);
            }
        }

        private bool IsStoryActor(NPCControl npc)
        {
            var actor = npc.transform;
            if (actor == chapter.mingi || actor.IsChildOf(chapter.mingi)
                || actor.IsChildOf(chapter.transform) || actor.IsChildOf(chapter.mission.transform)) return true;
            var combat = npc.GetComponent<CitizenCombat>();
            return (combat && combat.missionEnemy) || npc.GetComponent<PoliceControl>()
                || npc.GetComponent<RobberAI>();
        }

        public void Restore()
        {
            foreach (var citizen in hiddenCitizens)
            {
                if (citizen) citizen.SetActive(true);
            }
            hiddenCitizens.Clear();
        }
    }
}
