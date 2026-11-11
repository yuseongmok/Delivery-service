using System.Collections;
using UnityEngine;
using Phase = DeliveryService.Mok.BankPrologue.Phase;
using CombatBeat = DeliveryService.Mok.BankPrologue.CombatBeat;

namespace DeliveryService.Mok
{
    // 경보음 반복, 휴식 구간과 종료를 담당한다.
    internal sealed class PrologueAlarm
    {
        private readonly BankPrologue director;
        public PrologueAlarm(BankPrologue director)
        {
            this.director = director;
        }

        private float alarmPhaseTime;
        private bool alarmResting;
        private AudioSource siren;
        private AudioClip sirenClip;
        public bool Alarm { get; private set; }

        public void Silence()
        {
            if (siren)
            {
                siren.Stop();
            }
        }

        public void SetPaused(bool value)
        {
            if (siren)
            {
                if (value)
                {
                    siren.Pause();
                }
                else if (Alarm)
                {
                    siren.UnPause();
                }
            }
        }

        public void Dispose()
        {
            if (sirenClip)
            {
                Object.Destroy(sirenClip);
            }
        }

        public void StartBankAlarm()
        {
            if (Alarm || director.Collected >= director.cash.Length)
            {
                return;
            }

            Alarm = true;
            alarmResting = false;
            alarmPhaseTime = 0;
            siren.time = 0;
            siren.Play();
        }

        public void StopBankAlarm()
        {
            Alarm = false;
            alarmResting = false;
            alarmPhaseTime = 0;
            if (siren)
            {
                siren.Stop();
            }

            foreach (var light in director.alarmLights)
            {
                if (light)
                {
                    light.intensity = 0;
                }
            }
        }

        public void UpdateBankAlarm()
        {
            if (siren)
            {
                siren.volume = .25f * SuppliedAudio.Volume;
            }

            if (!Alarm)
            {
                return;
            }

            if (director.Collected >= director.cash.Length)
            {
                StopBankAlarm();
                return;
            }

            alarmPhaseTime += Time.deltaTime;
            float duration = alarmResting ? director.alarmRestSeconds : director.alarmCycleSeconds * director.alarmRepeats;
            if (alarmPhaseTime < duration)
            {
                return;
            }

            alarmPhaseTime = 0;
            alarmResting = !alarmResting;
            if (alarmResting)
            {
                siren.Stop();
            }
            else
            {
                siren.time = 0;
                siren.Play();
            }
        }

        public void MakeSiren()
        {
            int rate = 22050;
            var samples = new float[rate * 2];
            float angle = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                angle += 2 * Mathf.PI * (550 + 180 * Mathf.Sin(t * Mathf.PI * 2)) / rate;
                samples[i] = Mathf.Sin(angle) * .12f;
            }

            sirenClip = AudioClip.Create("은행 경보", samples.Length, 1, rate, false);
            sirenClip.SetData(samples, 0);
            siren = director.gameObject.AddComponent<AudioSource>();
            siren.clip = SuppliedAudio.Current && SuppliedAudio.Current.bankAlarm ? SuppliedAudio.Current.bankAlarm : sirenClip;
            siren.loop = true;
            siren.playOnAwake = false;
            siren.volume = .25f * SuppliedAudio.Volume;
        }
    }
}
