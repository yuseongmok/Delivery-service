using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class MedicineBrawler : PrisonBrawler
    {
        public PrisonMedicineMission mission;
        protected override PlayerCombat Player => mission.prison.player;
        protected override bool Paused => mission.State!=PrisonMedicineMission.Stage.Fight||mission.Failing;
        protected override void Hit(Vector3 direction){mission.Hit(direction);}
    }
}
