#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DeliveryService.Mok.Editor
{
    public static class MechanicCheck
    {
        static string log;

        static void Check(bool ok, string text)
        {
            log += (ok ? "PASS " : "FAIL ") + text + "\n";
            File.WriteAllText("Logs/Mechanic-check.txt", log);
            if (!ok) throw new Exception("Mechanic: " + text);
        }

        static IEnumerator Wait(float t)
        {
            float until = Time.time + t;
            while (Time.time < until) yield return null;
        }

        static IEnumerator Key(Key key)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return null;
        }

        static void Place(PlayerCombat p, Vector3 at)
        {
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false;
            p.transform.position = at + Vector3.up * (cc.height * .5f - cc.center.y + .12f);
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        static IEnumerator Walk(PlayerCombat p, Vector3 at)
        {
            var path = new NavMeshPath();
            Check(NavMesh.CalculatePath(p.transform.position, at, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "connected walking route");
            var cc = p.GetComponent<CharacterController>();
            foreach (var corner in path.corners.Skip(1))
            {
                float end = Time.time + 45;
                while (Vector2.Distance(new(p.transform.position.x, p.transform.position.z), new(corner.x, corner.z)) > .35f)
                {
                    if (Time.time > end) throw new Exception("blocked " + p.transform.position + " -> " + corner);
                    var d = corner - p.transform.position;
                    d.y = 0;
                    p.View.transform.rotation = Quaternion.LookRotation(d);
                    cc.Move((d.normalized * 6 + Vector3.down * 3) * Time.deltaTime);
                    yield return null;
                }
            }
        }

        static Vector3 SampleNavPoint(Vector3 origin, float maxDistance = 2.5f)
        {
            return NavMesh.SamplePosition(origin, out NavMeshHit hit, maxDistance, NavMesh.AllAreas) ? hit.position : origin;
        }

        public static IEnumerator Verify(bool isolated = false)
        {
            log = "Mechanic mission chain\n";
            yield return Wait(.5f);

            var m = MechanicMission.Current;
            Check(m && m.supportVehicle && m.rendezvous && m.targetSpot && m.racerNpc, "saved actor props, target spot, racer, and support car");
            Check(!m.Begin(), "locked before backdoor completion");

            if (isolated)
            {
                typeof(BrokerMission).GetProperty("State").SetValue(m.preparation.delivery.broker, BrokerMission.Phase.Complete);
                typeof(SpecialOrderMission).GetProperty("State").SetValue(m.preparation.delivery, SpecialOrderMission.Phase.Complete);
                typeof(BackdoorMission).GetProperty("State").SetValue(m.preparation, BackdoorMission.Phase.Complete);
                m.preparation.delivery.broker.day.EnterNightPreparation();
                StoryPhoneInbox.Current.AutoOpenEnabled = false;
            }
            else
            {
                yield return BackdoorCheck.Verify(true);
            }

            var b = m.preparation.delivery.broker;
            var p = m.Player;
            b.day.currentTime = 9;
            b.day.isShopOpen = false;
            yield return Wait(.3f);

            Check(m.CanStart, "starts without day or business conditions");
            Check(StoryPhoneInbox.Current.Messages.Count(msg => msg.title == "다음 준비 · 빚진 정비사") == 1, "single mechanic invitation");

            var app = UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);
            app.Open(false);
            app.Move(3);
            app.Confirm();
            app.Refresh();
            Check(app.action.text == "미션 시작", "backdoor completion unlocks phone mission");
            app.Confirm();
            yield return null;
            Check(m.State == MechanicMission.Phase.Meet && !m.parts.activeSelf, "phone starts mechanic mission with parts hidden");

            b.day.OnClick_ConfirmClose();
            Check(!b.day.IsTransitioning, "mission blocks settlement");
            int originalDay = b.day.currentDay;
            b.day.currentDay++;
            yield return null;
            Check(m.State == MechanicMission.Phase.Meet, "calendar change does not cancel mission");
            b.day.currentDay = originalDay;

            // 1. 정비사 첫 대면 및 브리핑
            var move = p.GetComponent<PlayerMove>();
            move.enabled = false;
            var look = p.View.GetComponent<CameraMove>();
            look.enabled = false;

            Place(p, m.approach.position);
            p.View.transform.LookAt(m.mechanic.position + Vector3.up * 1.2f);
            yield return Wait(.2f);
            yield return Key(UnityEngine.InputSystem.Key.F);
            Check(m.State == MechanicMission.Phase.Briefing, "F starts mechanic briefing");
            BrokerMissionCheck.Capture(b, "Mechanic-meeting");

            Time.timeScale = 0;
            m.AdvanceDialogue(true);
            Check(m.State == MechanicMission.Phase.Briefing, "pause preserves briefing");
            Time.timeScale = 1;

            yield return Key(UnityEngine.InputSystem.Key.Space);
            Check(m.State == MechanicMission.Phase.SpotMove, "skip moves to target spot phase");

            // 2. 지정된 장소 이동 및 상호작용
            yield return Walk(p, SampleNavPoint(m.targetSpot.position));
            p.View.transform.LookAt(m.targetSpot.position + Vector3.up * 1.1f);
            yield return Wait(.2f);
            yield return Key(UnityEngine.InputSystem.Key.F);
            if (m.State == MechanicMission.Phase.SpotDialogue)
            {
                yield return Key(UnityEngine.InputSystem.Key.Space);
            }
            Check(m.State == MechanicMission.Phase.RacerDeal, "target spot interaction advances to racer deal");

            // 3. 레이서 대면 및 거래/레이스 승리 처리
            yield return Walk(p, SampleNavPoint(m.racerNpc.position));
            p.View.transform.LookAt(m.racerNpc.position + Vector3.up * 1.2f);
            yield return Wait(.2f);
            yield return Key(UnityEngine.InputSystem.Key.F);
            if (m.State == MechanicMission.Phase.RacerDialogue)
            {
                yield return Key(UnityEngine.InputSystem.Key.Space);
            }
            Check(m.State == MechanicMission.Phase.Racing, "racer dialogue starts race phase");

            // 레이스 진행 정리 및 플레이어 승리 처리
            var bike = UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
            if (bike != null && bike.isDriven)
            {
                bike.ExitBike();
            }
            if (RaceManager.Instance != null && RaceManager.Instance.IsRaceActive)
            {
                RaceManager.Instance.EndRace();
            }
            if (m.racerNpc != null)
            {
                m.racerNpc.gameObject.SetActive(true);
            }

            m.OnRaceFinished(true);
            move.enabled = false;
            look.enabled = false;
            yield return null;

            Check(m.PartRecovered && m.State == MechanicMission.Phase.Return, "winning race recovers mission part");
            Check(Vector3.Distance(m.parts.transform.position, p.View.transform.position) < 1.5f, "carried part appears with player");
            BrokerMissionCheck.Capture(b, "Mechanic-recovered");

            // 4. 중단 및 재시작 시 부품 회수 상태 유지 확인
            m.Cancel();
            Check(!m.VehicleSupportUnlocked && m.PartRecovered && m.Pursuers.Length == 0, "cancel preserves part without reward");
            Check(m.Begin() && m.State == MechanicMission.Phase.Return, "retry resumes return with recovered part");
            look.enabled = false;
            move.enabled = false;

            // 5. 정비사에게 부품 전달 및 완료
            yield return Walk(p, m.approach.position);
            p.View.transform.LookAt(m.mechanic.position + Vector3.up * 1.2f);
            yield return Wait(.2f);
            yield return Key(UnityEngine.InputSystem.Key.F);
            if (m.State != MechanicMission.Phase.Handover)
            {
                var d = m.mechanic.position + Vector3.up * 1.1f - p.View.transform.position;
                log += "return diagnostic state=" + m.State + " position=" + p.transform.position + " mechanic=" + m.mechanic.position + " canAct=" + p.CanAct + " hits=" + string.Join(";", Physics.RaycastAll(p.View.transform.position, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore).Select(h => h.collider.name)) + "\n";
            }
            Check(m.State == MechanicMission.Phase.Handover, "F hands part to mechanic");

            yield return Wait(m.handoverSeconds + .2f);
            Check(m.State == MechanicMission.Phase.Debrief && m.keys.activeSelf && Vector3.Distance(m.parts.transform.position, m.benchRest.position) < .05f, "physical handover reaches workbench and shows key");
            BrokerMissionCheck.Capture(b, "Mechanic-support");

            int money = MoneyManager.Instance.currentMoney;
            yield return Key(UnityEngine.InputSystem.Key.Space);
            Check(m.Completed && m.VehicleSupportUnlocked && !m.Begin() && !MechanicMission.Active, "vehicle support awarded once");
            Check(MoneyManager.Instance.currentMoney == money, "support does not modify cash");

            app.Open(false);
            app.Move(3);
            app.Confirm();
            app.Refresh();
            Check(app.action.text.Contains("완료") && app.heading.text.Contains("빚진 정비사"), "phone displays vehicle support");

            b.day.currentDay++;
            yield return null;
            Check(m.VehicleSupportUnlocked, "vehicle support survives day change");

            app.gameObject.SetActive(false);
            look.enabled = true;
            move.enabled = true;
        }
    }
}
#endif