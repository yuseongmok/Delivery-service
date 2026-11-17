using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class ChickenAmbusher : PrisonBrawler
    {
        public PrisonChickenMission mission;
        public bool huntsMingi;
        public Transform approach;
        protected override PlayerCombat Player => mission.prison.player;
        protected override bool Paused => mission.Failing;
        protected override Transform TargetActor => mission.defense && !mission.defense.Ready && approach ? approach :
            huntsMingi ? mission.prison.introduction.mingi.transform : Player.transform;
        protected override bool AttackAllowed => mission.defense && mission.defense.Ready &&
            (!huntsMingi || !mission.defense.MingiDown);
        protected override Vector3 TargetPoint => huntsMingi ? TargetActor.position + Vector3.up * 1.3f : Player.View.transform.position;
        protected override void Hit(Vector3 direction)
        {
            if(huntsMingi) mission.defense.HitMingi(direction);
            else mission.Hit(direction);
        }
    }
}
