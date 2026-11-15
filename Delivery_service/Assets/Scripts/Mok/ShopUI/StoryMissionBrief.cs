using UnityEngine.UI;
namespace DeliveryService.Mok
{
    // 휴대폰은 미션 상태를 읽기만 한다. 시작/중단/해금은 기존 미션이 담당한다.
    public static class StoryMissionBrief
    {
        public static string Goal(NightPreparationPlan.Mission mission)
        {
            Text live=null;
            switch(mission.id)
            {
                case "special_order":if(SpecialOrderMission.Current&&SpecialOrderMission.Current.Running)live=SpecialOrderMission.Current.objective;break;
                case "backdoor":if(BackdoorMission.Current&&BackdoorMission.Current.Running)live=BackdoorMission.Current.objective;break;
                case "mechanic":if(MechanicMission.Current&&MechanicMission.Current.Running)live=MechanicMission.Current.objective;break;
                case "finale":if(VenueFinale.Current&&VenueFinale.Current.Running)live=VenueFinale.Current.objective;break;
                default:if(BrokerMission.Current&&BrokerMission.Current.Running)live=BrokerMission.Current.objective;break;
            }
            if(live&&!string.IsNullOrWhiteSpace(live.text))
            {
                StoryObjectiveCard.Split(live.text,mission.title,out _,out string goal);return goal;
            }
            return string.IsNullOrWhiteSpace(mission.startingObjective)?"미션을 시작하고 안내된 목적지로 이동하세요.":mission.startingObjective;
        }
        public static string Detail(NightPreparationPlan.Mission mission,bool complete)
        {
            if(complete)return "<color=#317260><b>완료한 미션</b></color>\n\n<b>확보한 결과</b>\n"+mission.result+"\n\n이 미션은 완료했습니다. 목록에서 다음 미션을 확인하세요.";
            string reason=string.IsNullOrWhiteSpace(mission.purpose)?mission.description:mission.purpose;
            return "<color=#A64E1E><b>현재 목표</b></color>\n"+Goal(mission)+"\n\n<b>이 미션을 하는 이유</b>\n"+reason+"\n\n<b>완료하면</b>\n"+mission.result;
        }
    }
}
