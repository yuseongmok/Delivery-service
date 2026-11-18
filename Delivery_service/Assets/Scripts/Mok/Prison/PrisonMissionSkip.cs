using UnityEngine;

namespace DeliveryService.Mok
{
    // 도시 치트와 감옥 진행의 연결. 현재 미션 하나만 완료한다.
    public static class PrisonMissionSkip
    {
        public static string Target {
            get {
                var intro=Object.FindFirstObjectByType<PrisonIntroduction>();
                if(!intro)return null;
                if(intro.State!=PrisonIntroduction.Stage.Complete)return "정보거래";
                var chicken=Object.FindFirstObjectByType<PrisonChickenMission>();
                if(chicken&&chicken.State!=PrisonChickenMission.Stage.Complete)return "갓 튀긴 치킨";
                var medicine=Object.FindFirstObjectByType<PrisonMedicineMission>();
                if(medicine&&medicine.State!=PrisonMedicineMission.Stage.Complete)return "강탈";
                var keys=Object.FindFirstObjectByType<PrisonKeysMission>();
                if(keys&&keys.State!=PrisonKeysMission.Stage.Complete)return "소매치기와 배식";
                var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();
                if(office&&!office.Complete)return "간수장실 조사";
                var cctv=Object.FindFirstObjectByType<PrisonCctvMission>();
                if(cctv&&cctv.State!=PrisonCctvMission.Stage.Complete)return "CCTV 무력화";
                var night=Object.FindFirstObjectByType<PrisonNightMission>();
                if(night&&!night.Complete)return "야간 잠입";
                var transport=Object.FindFirstObjectByType<PrisonTransportEscape>();
                if(transport)return transport.Complete?"스킵할 미션 없음":"호송차량 탈출";
                if(night)return "스킵할 미션 없음";
                if(cctv)return "스킵할 미션 없음";
                var escape=Object.FindFirstObjectByType<PrisonEscapeMission>();
                return escape&&escape.State!=PrisonEscapeMission.Stage.Complete?"탈옥":"스킵할 미션 없음";
            }
        }
        public static bool Complete()
        {
            var prison=Object.FindFirstObjectByType<PrisonMission>();
            var ragdoll=prison.player.GetComponentInChildren<PlayerRagdoll>();
            if(ragdoll&&ragdoll.IsActive)return false;
            switch(Target){
                case "정보거래":prison.introduction.DeveloperComplete();return true;
                case "갓 튀긴 치킨":
                    var chicken=Object.FindFirstObjectByType<PrisonChickenMission>();
                    if(chicken.Failing)return false;chicken.DeveloperComplete();return true;
                case "강탈":
                    var medicine=Object.FindFirstObjectByType<PrisonMedicineMission>();
                    if(!medicine.CanDeveloperComplete)return false;medicine.DeveloperComplete();return true;
                case "소매치기와 배식":
                    var keys=Object.FindFirstObjectByType<PrisonKeysMission>();
                    if(!keys.CanDeveloperComplete)return false;keys.DeveloperComplete();return true;
                case "간수장실 조사":
                    var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();
                    if(office.GetComponent<PrisonOfficePickup>().Busy)return false;
                    if(office.evidence)office.evidence.DeveloperComplete();
                    office.Collect(PrisonOfficePoint.Kind.Uniforms);office.Collect(PrisonOfficePoint.Kind.VehicleKey);office.Collect(PrisonOfficePoint.Kind.Password);return true;
                case "CCTV 무력화":Object.FindFirstObjectByType<PrisonCctvMission>().DeveloperComplete();return true;
                case "야간 잠입":Object.FindFirstObjectByType<PrisonNightMission>().DeveloperComplete();return true;
                case "호송차량 탈출":Object.FindFirstObjectByType<PrisonTransportEscape>().DeveloperComplete();return true;
                case "탈옥":Object.FindFirstObjectByType<PrisonEscapeMission>().DeveloperComplete();return true;
                default:return false;
            }
        }
    }
}
