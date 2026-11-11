using System.Collections;
using UnityEngine;
using Phase = DeliveryService.Mok.BankPrologue.Phase;
using CombatBeat = DeliveryService.Mok.BankPrologue.CombatBeat;

namespace DeliveryService.Mok
{
    // 오프닝, 1인칭 탑승, 도시 진입과 매장 도착 컷씬을 담당한다.
    internal sealed class PrologueCinematics
    {
        private readonly BankPrologue director;
        public PrologueCinematics(BankPrologue director)
        {
            this.director = director;
        }

        public void SetCinematic(bool value)
        {
            director.player.view.enabled = !value;
            director.player.view.GetComponent<AudioListener>().enabled = !value;
            director.filmCamera.enabled = value;
            director.filmCamera.GetComponent<AudioListener>().enabled = value;
            director.player.weapon.SetActive(!value);
            director.bars.alpha = value ? 1 : 0;
            director.crosshair.SetActive(!value);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private IEnumerator Shot(int index, string line, float seconds, Vector3 drift)
        {
            director.subtitle.text = line;
            var start = director.shots[index];
            for (float t = 0; t < seconds && !director.CinematicSkipRequested; t += Time.deltaTime)
            {
                director.filmCamera.transform.SetPositionAndRotation(start.position + drift * Mathf.SmoothStep(0, 1, t / seconds), start.rotation);
                yield return null;
            }
        }

        public IEnumerator Opening()
        {
            SetCinematic(true);
            director.fade.alpha = 1;
            director.objective.text = director.story.title;
            director.prompt.text = "Space  컷신 건너뛰기";
            director.filmCamera.transform.SetPositionAndRotation(director.shots[0].position, director.shots[0].rotation);
            for (float t = 0; t < 1.2f && !director.CinematicSkipRequested; t += Time.deltaTime)
            {
                director.fade.alpha = 1 - t / 1.2f;
                yield return null;
            }

            director.fade.alpha = 0;
            yield return Shot(0, director.story.vault.text, director.story.vault.seconds, Vector3.forward * .35f);
            if (director.accomplice && director.accomplice.performance)
            {
                yield return director.accomplice.performance.OpenVault();
            }
            else
            {
                for (float t = 0; t < 2.4f && !director.CinematicSkipRequested; t += Time.deltaTime)
                {
                    director.vaultDoor.localRotation = Quaternion.Euler(0, -105 * Mathf.SmoothStep(0, 1, t / 2.4f), 0);
                    yield return null;
                }
            }

            director.vaultDoor.localRotation = Quaternion.Euler(0, -105, 0);
            director.StartBankAlarm();
            if (director.accomplice && director.accomplice.performance && !director.CinematicSkipRequested)
            {
                yield return director.accomplice.performance.ReactToAlarm();
            }

            yield return Shot(1, director.story.alarm.text, director.story.alarm.seconds, Vector3.forward * .2f);
            director.officers[0].StageEntrance(director.policeEntryTargets.Length > 0 ? director.policeEntryTargets[0].position : new Vector3(-.7f, 0, -5));
            director.officers[1].StageEntrance(director.policeEntryTargets.Length > 1 ? director.policeEntryTargets[1].position : new Vector3(.7f, 0, -5));
            yield return Shot(2, director.story.police.text, director.story.police.seconds, Vector3.left * .3f);
            // 입구를 보여준 뒤 플레이할 시점으로 짧게 연결해 조작 전환을 알린다.
            if (!director.CinematicSkipRequested)
            {
                director.subtitle.text = director.story.collect;
                for (float t = 0; t < .45f && !director.CinematicSkipRequested; t += Time.deltaTime)
                {
                    director.fade.alpha = t < .2f ? t / .2f : 1 - (t - .2f) / .25f;
                    if (t >= .2f)
                    {
                        director.filmCamera.transform.SetPositionAndRotation(director.player.view.transform.position, director.player.view.transform.rotation);
                    }

                    yield return null;
                }
            }

            director.BeginRobbery();
        }

        public IEnumerator Getaway()
        {
            foreach (var officer in director.officers)
            {
                officer.Stop();
            }

            SetCinematic(true);
            director.objective.text = "";
            director.ammo.text = "";
            director.health.text = "";
            director.prompt.text = "Space  컷신 건너뛰기";
            var escapePerformance = director.GetComponent<PrologueEscapePerformance>();
            if (escapePerformance)
            {
                yield return escapePerformance.BoardFirstPerson();
            }
            else if (director.accomplice && director.accomplice.performance)
            {
                yield return director.accomplice.performance.EnterVehicle();
            }

            director.subtitle.text = director.story.boarding.text;
        }

        public IEnumerator Arrival()
        {
            director.objective.text = "";
            director.ammo.text = "";
            director.health.text = "";
            director.prompt.text = "Space  컷신 건너뛰기";
            director.subtitle.text = "";
            for (float t = 0; t < 1; t += Time.deltaTime)
            {
                director.fade.alpha = t;
                yield return null;
            }

            director.fade.alpha = 1;
            director.StopBankAlarm();
            director.bars.alpha = 1;
            foreach (var o in director.roadPolice)
            {
                o.Stop();
            }

            // 실제 본편 씬의 건물·도로만 저장한 전경을 사용해 경제와 일차는 아직 시작하지 않는다.
            director.cityVista.SetActive(true);
            RenderSettings.fog = false;
            director.filmCamera.farClipPlane = 3000;
            var carBody = director.getawayCar.GetComponent<Rigidbody>();
            carBody.position = director.cityCarStart.position;
            carBody.rotation = director.cityCarStart.rotation;
            Vector3 carA = director.cityCarStart.position, carB = director.cityCarEnd.position;
            for (float t = 0; t < director.story.cityArrivalSeconds && !director.CinematicSkipRequested; t += Time.deltaTime)
            {
                float p = t / Mathf.Max(.1f, director.story.cityArrivalSeconds);
                var moving = Vector3.Lerp(carA, carB, p);
                carBody.position = moving;
                director.getawayCar.position = moving;
                foreach (var wheel in director.drive.wheels)
                {
                    wheel.Rotate(Vector3.right, Vector3.Distance(carA, carB) / Mathf.Max(.1f, director.story.cityArrivalSeconds) * Time.deltaTime * 130, Space.Self);
                }

                director.filmCamera.transform.position = director.getawayCar.position - director.getawayCar.forward * 12 + director.getawayCar.right * 9 + Vector3.up * 6;
                director.filmCamera.transform.LookAt(director.getawayCar.position + director.getawayCar.forward * 4 + Vector3.up);
                director.fade.alpha = 1 - Mathf.Clamp01(t);
                yield return null;
            }

            director.getawayCar.position = carB;
            carBody.position = carB;
            director.fade.alpha = 0;
            float citySpeed = Vector3.Distance(carA, carB) / Mathf.Max(.1f, director.story.cityArrivalSeconds);
            Vector3 cameraStart = director.filmCamera.transform.position;
            Quaternion rotationStart = director.filmCamera.transform.rotation;
            director.subtitle.text = director.shopArrival ? "" : director.story.getaway.text;
            for (float t = 0; t < director.story.cityPanSeconds && !director.CinematicSkipRequested; t += Time.deltaTime)
            {
                float p = Mathf.SmoothStep(0, 1, t / Mathf.Max(.1f, director.story.cityPanSeconds));
                float rollingSpeed = Mathf.Lerp(citySpeed, 1.2f, p);
                var moving = director.getawayCar.position + director.getawayCar.forward * rollingSpeed * Time.deltaTime;
                carBody.position = moving;
                director.getawayCar.position = moving;
                foreach (var wheel in director.drive.wheels)
                {
                    wheel.Rotate(Vector3.right, rollingSpeed * Time.deltaTime * 130, Space.Self);
                }

                director.filmCamera.transform.position = Vector3.Lerp(cameraStart, director.cityWideCamera.position, p);
                director.filmCamera.transform.rotation = Quaternion.Slerp(rotationStart, director.cityWideCamera.rotation, p);
                yield return null;
            }

            director.filmCamera.transform.SetPositionAndRotation(director.cityWideCamera.position, director.cityWideCamera.rotation);
            if (director.shopArrival)
            {
                yield return director.shopArrival.Play();
            }

            director.subtitle.text = "";
            director.prompt.text = "";
            director.logo.alpha = 0;
            // 로고는 전경이 완전히 드러난 뒤 등장한다.
            for (float t = 0; t < director.story.logoSeconds; t += Time.deltaTime)
            {
                director.logo.alpha = Mathf.Clamp01(t * 2);
                yield return null;
            }

            for (float t = 0; t < 1; t += Time.deltaTime)
            {
                director.fade.alpha = t;
                director.logo.alpha = 1 - t;
                yield return null;
            }

            director.fade.alpha = 1;
            director.logo.alpha = 0;
            director.objective.text = director.shopArrival ? "다음 날 아침\n이얏호 피자, 첫 영업일" : director.story.ending.text;
            yield return new WaitForSeconds(Mathf.Max(.1f, director.story.ending.seconds));
        }
    }
}
