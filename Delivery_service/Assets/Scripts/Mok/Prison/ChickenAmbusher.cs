using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class ChickenAmbusher : PrisonBrawler
    {
        public PrisonChickenMission mission;
        protected override PlayerCombat Player => mission.prison.player;
        protected override bool Paused => mission.Failing;
        protected override void Hit(Vector3 direction) { mission.Hit(direction); }
    }
}
