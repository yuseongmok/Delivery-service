using System.Collections;
using UnityEngine;
using Phase = DeliveryService.Mok.BankPrologue.Phase;
using CombatBeat = DeliveryService.Mok.BankPrologue.CombatBeat;

namespace DeliveryService.Mok
{
    // 전투의 강약과 화면 밖 증원 투입을 담당한다.
    internal sealed class PrologueCombatDirector
    {
        private readonly BankPrologue director;
        public PrologueCombatDirector(BankPrologue director)
        {
            this.director = director;
        }

        private int reinforcements;
        private float beatStarted;
        private bool finalPushStarted;
        public int SpawnedPolice => reinforcements;
        public CombatBeat BattleBeat { get; private set; }

        public float SquadShotInterval => BattleBeat == CombatBeat.Breather ? 2.2f : BattleBeat == CombatBeat.FinalPush ? .65f : 1.05f;
        public void Begin(ref float waveTime)
        {
            waveTime = Time.time + director.waveInterval;
            reinforcements = 0;
            finalPushStarted = false;
            ChangeBeat(CombatBeat.FirstContact);
            while (reinforcements < Mathf.Min(Mathf.Min(director.initialPolice, 4), director.officers.Length))
            {
                if (!DeployNextOfficer())
                {
                    break;
                }
            }
        }

        private void ChangeBeat(CombatBeat beat)
        {
            BattleBeat = beat;
            beatStarted = Time.time;
        }

        private bool DeployNextOfficer()
        {
            // 보이는 후보는 대기시키고 가려진 다른 증원 후보부터 투입한다.
            for (int i = reinforcements; i < director.officers.Length; i++)
            {
                var officer = director.officers[i];
                if (!officer.Active && PrologueEscapePerformance.VisibleSpawn(director.player.view, officer.transform.position))
                {
                    continue;
                }

                var pending = director.officers[reinforcements];
                director.officers[reinforcements] = officer;
                director.officers[i] = pending;
                if (!officer.Active)
                {
                    officer.Activate();
                }

                reinforcements++;
                return true;
            }

            return false;
        }

        private void UpdateBattleRhythm(ref float waveTime)
        {
            if (director.Current == Phase.Escape)
            {
                if (BattleBeat != CombatBeat.Withdrawal)
                {
                    ChangeBeat(CombatBeat.Withdrawal);
                    waveTime = Mathf.Max(waveTime, Time.time + 4);
                }

                return;
            }

            float elapsed = Time.time - beatStarted;
            float collected = (director.Collected + (director.accomplice ? director.accomplice.CollectionProgress : 0)) / Mathf.Max(1, director.cash.Length);
            // 마지막 돈을 챙기는 시점에 맞추되, 최소 한 번은 숨 돌릴 틈을 보장한다.
            if (!finalPushStarted && collected >= .72f && BattleBeat != CombatBeat.FirstContact && BattleBeat != CombatBeat.Breather)
            {
                finalPushStarted = true;
                ChangeBeat(CombatBeat.FinalPush);
                director.subtitle.text = director.story.finalPressure;
                return;
            }

            if (BattleBeat == CombatBeat.FirstContact && elapsed >= director.story.firstContactSeconds)
            {
                ChangeBeat(CombatBeat.Breather);
                director.subtitle.text = director.story.breather;
            }
            else if (BattleBeat == CombatBeat.Breather && elapsed >= director.story.breatherSeconds)
            {
                ChangeBeat(CombatBeat.Reinforcement);
                director.subtitle.text = director.story.reinforcement;
            }
            else if (BattleBeat == CombatBeat.Reinforcement && !finalPushStarted && elapsed >= director.story.supportSeconds)
            {
                ChangeBeat(CombatBeat.Breather);
                director.subtitle.text = director.story.breather;
            }
        }

        public void Tick(ref float waveTime)
        {
            UpdateBattleRhythm(ref waveTime);
            if (BattleBeat != CombatBeat.Breather && BattleBeat != CombatBeat.FirstContact && Time.time >= waveTime && reinforcements < director.officers.Length)
            {
                int live = 0;
                foreach (var officer in director.officers)
                {
                    if (officer.Active && !officer.Dead)
                    {
                        live++;
                    }
                }

                int cap = BattleBeat == CombatBeat.FinalPush ? director.maxActivePolice : Mathf.Min(director.maxActivePolice, 10);
                int count = Mathf.Min(BattleBeat == CombatBeat.FinalPush ? director.waveSize + 2 : director.waveSize, cap - live);
                for (int i = 0; i < count && reinforcements < director.officers.Length; i++)
                {
                    if (!DeployNextOfficer())
                    {
                        break;
                    }
                }

                waveTime = Time.time + (BattleBeat == CombatBeat.FinalPush ? 4 : director.waveInterval);
            }
        }
    }
}
