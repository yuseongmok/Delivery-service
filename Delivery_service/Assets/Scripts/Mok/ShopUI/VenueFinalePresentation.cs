using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Phase = DeliveryService.Mok.VenueFinale.Phase;

namespace DeliveryService.Mok
{
    // 목표 문구와 목적지 표시, 자막 노출 시간을 담당한다.
    internal sealed class VenueFinalePresentation
    {
        private readonly VenueFinale mission;
        public VenueFinalePresentation(VenueFinale mission)
        {
            this.mission = mission;
        }

        private readonly EscapeRouteGuide escapeGuide = new();
        private float noticeUntil;
        public void ResetRoute()
        {
            escapeGuide.Reset();
        }

        public void HideExpiredSubtitle()
        {
            if (Time.time > noticeUntil)
            {
                mission.subtitle.text = "";
            }
        }

        public void RefreshIdle()
        {
            mission.marker.gameObject.SetActive(false);
            if (Time.time > noticeUntil)
            {
                mission.ui.SetActive(false);
            }
        }

        public void Notice(string message, float seconds = 7)
        {
            mission.subtitle.text = message;
            noticeUntil = Time.time + seconds;
        }

        public void RefreshGameplay(bool turned)
        {
            Transform goal = GetGoal();
            string label = GetGoalLabel(goal, turned);
            mission.objective.text = BuildObjective(goal, label, turned);
            mission.marker.gameObject.SetActive(true);
            StoryTargetDiamond.Place(mission.marker, mission.Player.View, GetMarkerPosition(goal), mission.Player.transform.position, label);
            if (mission.State == Phase.Conversation)
            {
                mission.marker.gameObject.SetActive(false);
            }

            if (mission.State != Phase.Conversation && Time.time > noticeUntil)
            {
                mission.subtitle.text = "";
            }
        }

        private Transform GetGoal()
        {
            switch (mission.State)
            {
                case Phase.Entrance:
                    return mission.entry;
                case Phase.Deposit:
                    return mission.deliveryTable;
                case Phase.Observe:
                case Phase.Conversation:
                    return mission.stealth ? mission.stealth.Guidance : mission.observeStand;
                default:
                    return mission.EscapeGoal;
            }
        }

        private string GetGoalLabel(Transform goal, bool turned)
        {
            switch (mission.State)
            {
                case Phase.Entrance:
                    return "납품 출입구";
                case Phase.Deposit:
                    return "서비스 테이블";
                case Phase.Observe:
                case Phase.Conversation:
                    return mission.stealth && !mission.stealth.AtListeningPoint ? "순찰을 피해 관찰 지점으로" : "박진웅 대화 엿듣기";
                case Phase.InteriorEscape:
                    return goal == mission.interiorExit ? "들어왔던 입구로 나가기" : "접수 복도를 따라 입구로";
                default:
                    return turned ? "정비사 차량 탑승" : "뒷골목으로 탈출";
            }
        }

        private string BuildObjective(Transform goal, string label, bool turned)
        {
            string message = "마지막 배달 · " + label;
            if (mission.State == Phase.InteriorEscape && !mission.ExitUnlocked)
            {
                message += "\n출구 봉쇄 · 경비 제압 " + mission.SubduedCount + " / 2";
            }

            if (mission.State == Phase.Observe)
            {
                return message + "\n엄폐 옆에서 박진웅을 바라보고 [F] 길게 · " + Mathf.RoundToInt(mission.ObservationProgress * 100) + "%";
            }

            bool followingRoute = (mission.State == Phase.InteriorEscape && goal != mission.interiorExit) || (mission.State == Phase.StreetEscape && !turned);
            if (mission.State != Phase.Conversation && !followingRoute)
            {
                message += "\n[F] 상호작용";
            }

            return message;
        }

        private Vector3 GetMarkerPosition(Transform goal)
        {
            bool nearObservation = mission.State == Phase.Observe && Vector3.Distance(mission.Player.transform.position, mission.observeStand.position) < 3.5f;
            if (nearObservation)
            {
                return mission.target.position + Vector3.up * 2;
            }

            bool escaping = mission.State == Phase.InteriorEscape || mission.State == Phase.StreetEscape;
            Vector3 position = escaping ? escapeGuide.Marker(mission.Player.transform.position, goal.position, false) : goal.position;
            return position + Vector3.up;
        }
    }
}
