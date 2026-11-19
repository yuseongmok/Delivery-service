using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    public sealed class AftermathFilm : MonoBehaviour
    {
        [System.Serializable]
        public class Motion
        {
            public Transform subject, from, to;
            public AftermathActor actor;
            public Transform leftContact, rightContact;
            [Range(0, 1)] public float seatedFrom, seatedTo, reach;
            [Range(0, .9f)] public float moveDelay, reachBlend;
            public Transform bend, carriedBy;
            public Vector3 carryOffset;
            public bool facePath;
        }

        [System.Serializable]
        public class Shot
        {
            public string name;
            public GameObject set;
            public Transform cameraFrom, cameraTo, lookAt;
            public Transform lookFrom;
            [Range(20, 80)] public float fieldOfView = 45;
            public Motion[] motions;
            public AudioClip effect;
            public bool fadeIn, fadeOut, timeCard, chapterCard;
            public bool nightCard, night;
            public bool shopKeyCard;
            public Transform closingLid;
            [Min(0)] public float subtitleDelay, subtitleTail;
            public Transform reactionTarget;
            public AftermathActor reactionActor;
        }

        public AftermathDialogue dialogue;
        public AftermathFirstPerson firstPerson;
        public AftermathHideout hideout;
        public AftermathEscapeBridge escapeBridge;
        public Camera film;
        public Shot[] shots;
        public Text subtitle, title, newsHeadline, newsTicker, hint;
        public Image curtain;
        public AudioSource sound, voice, ambience;
        public string nextScene = "NewHarborCity";
        public bool autoStart = true;
        public int CurrentShot { get; private set; } = -1;
        public bool Finished { get; private set; }
        public bool Paused { get; private set; }
        public bool Loading { get; private set; }
        bool skip;
        public bool SkipRequested => skip;
        float originalTimeScale;
        CursorLockMode oldLock;
        bool oldCursor;
        float FrameDelta => Time.captureDeltaTime > 0 ? Time.captureDeltaTime : Time.unscaledDeltaTime;

        void Start()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 1;
            oldLock = Cursor.lockState; oldCursor = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            if (autoStart) StartCoroutine(Play());
        }

        void Update()
        {
            if (Finished || Loading) return;
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) SetPaused(!Paused);
            if (k != null && k.enterKey.wasPressedThisFrame) RequestSkip();
            hint.text = Paused ? "일시 정지 · ESC 계속 / ENTER 건너뛰기" : "ESC 일시 정지   ·   ENTER 건너뛰기";
            sound.volume = .5f * SuppliedAudio.Volume;
            voice.volume = SuppliedAudio.Volume;
            ambience.volume = .14f * SuppliedAudio.Volume;
        }

        public void SetPaused(bool value)
        {
            Paused = value;
            foreach (var actor in GetComponentsInChildren<AftermathActor>(true))
                if (actor.animator) actor.animator.speed = value ? 0 : 1;
            if (value) { sound.Pause(); voice.Pause(); ambience.Pause(); }
            else { sound.UnPause(); voice.UnPause(); ambience.UnPause(); }
        }

        public void RequestSkip() { skip = true; SetPaused(false); }

        public IEnumerator Play()
        {
            newsHeadline.text = dialogue.newsHeadline; newsTicker.text = dialogue.newsTicker;
            foreach (var shot in shots) if (shot.set) shot.set.SetActive(false);
            if (ambience.clip) ambience.Play();
            var sun = RenderSettings.sun;
            float daylight = sun ? sun.intensity : 1;
            Color daylightColor = sun ? sun.color : Color.white;
            for (int i = 0; i < shots.Length && !skip; i++)
            {
                CurrentShot = i;
                var shot = shots[i];
                if (sun) { sun.intensity = shot.night ? daylight * .16f : daylight; sun.color = shot.night ? new Color(.45f,.6f,1) : daylightColor; }
                foreach (var actor in GetComponentsInChildren<AftermathActor>(true)) { actor.lookTarget = null; actor.lookWeight = 0; }
                foreach (var other in shots) if (other.set && other.set != shot.set) other.set.SetActive(false);
                if (shot.set) shot.set.SetActive(true);
                var line = i < dialogue.lines.Length ? dialogue.lines[i] : null;
                float duration = line != null ? Mathf.Max(1, line.seconds) : 4;
                subtitle.text = "";
                title.text = shot.nightCard ? dialogue.nightCard : shot.timeCard ? dialogue.timeCard : shot.chapterCard ? dialogue.chapterTitle : "";
                if (shot.effect) sound.PlayOneShot(shot.effect);
                voice.Stop(); bool voiceStarted = false;
                for (float t = 0; t < duration && !skip;)
                {
                    if (!Paused)
                    {
                        float p = Mathf.Clamp01(t / duration);
                        if (!voiceStarted && t >= shot.subtitleDelay)
                        {
                            voiceStarted = true;
                            if (line != null && line.voice) { voice.clip = line.voice; voice.Play(); }
                        }
                        subtitle.text = line != null && t >= shot.subtitleDelay && t < duration - shot.subtitleTail ? line.subtitle : "";
                        ApplyShot(i, p);
                        float a = shot.fadeIn ? 1 - Mathf.Clamp01(t / .85f) : 0;
                        if (shot.shopKeyCard)
                        {
                            title.text = t < 2.2f ? dialogue.shopKeyCard : "";
                            a = 1 - Mathf.Clamp01((t - 1.7f) / .85f);
                        }
                        if (shot.fadeOut) a = Mathf.Max(a, Mathf.Clamp01((t - duration + .85f) / .85f));
                        curtain.color = new Color(0, 0, 0, a);
                        t += FrameDelta;
                    }
                    yield return null;
                }
                if (!skip) ApplyShot(i, 1);
                if (i == 0 && escapeBridge && !skip) yield return escapeBridge.Play(this);
                if (shot.motions != null) foreach (var motion in shot.motions) if (motion.actor) motion.actor.walkSpeed = 0;
            }
            yield return EndFilm();
        }

        // Editor preview and runtime use exactly the same shot data.
        public void ApplyShot(int index, float progress)
        {
            var shot = shots[index];
            float ease = Mathf.SmoothStep(0, 1, progress);
            film.fieldOfView = shot.fieldOfView;
            if (shot.closingLid) shot.closingLid.localRotation = Quaternion.Euler(Mathf.Lerp(95, 0, Mathf.SmoothStep(0,1, progress / .52f)),0,0);
            film.transform.position = Vector3.Lerp(shot.cameraFrom.position, shot.cameraTo.position, ease);
            film.transform.LookAt(shot.lookFrom ? Vector3.Lerp(shot.lookFrom.position, shot.lookAt.position, Mathf.SmoothStep(0, 1, progress / .25f)) : shot.lookAt.position);
            if (shot.motions == null) { if (firstPerson) firstPerson.Present(this, index, progress); return; }
            foreach (var m in shot.motions)
            {
                if (!m.subject || !m.from || !m.to) continue;
                float movement = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(m.moveDelay, 1, progress));
                Vector3 position = Vector3.Lerp(m.from.position, m.to.position, movement);
                Quaternion rotation = Quaternion.Slerp(m.from.rotation, m.to.rotation, ease);
                if (m.bend)
                {
                    position = Vector3.Lerp(Vector3.Lerp(m.from.position, m.bend.position, movement), Vector3.Lerp(m.bend.position, m.to.position, movement), movement);
                    if (m.facePath)
                    {
                        var tangent = Vector3.Lerp(m.bend.position - m.from.position, m.to.position - m.bend.position, movement);
                        tangent.y = 0;
                        if (tangent.sqrMagnitude > .001f) rotation = Quaternion.Slerp(m.from.rotation, Quaternion.LookRotation(tangent), Mathf.SmoothStep(0, 1, progress / Mathf.Max(.12f, m.moveDelay)));
                    }
                }
                if (m.carriedBy)
                {
                    position = m.carriedBy.TransformPoint(m.carryOffset);
                    rotation = m.carriedBy.rotation;
                }
                m.subject.SetPositionAndRotation(position, rotation);
                if (m.actor)
                {
                    m.actor.seated = Mathf.Lerp(m.seatedFrom, m.seatedTo, ease);
                    m.actor.leftContact = m.leftContact; m.actor.rightContact = m.rightContact;
                    m.actor.reach = m.reach * (m.reachBlend > 0 ? Mathf.SmoothStep(0, 1, progress / m.reachBlend) : 1);
                    m.actor.walkSpeed = progress > m.moveDelay && progress < .98f && Vector3.Distance(m.from.position, m.to.position) > .7f && m.seatedFrom == 0 ? 1.1f : 0;
                }
            }
            if (shot.reactionActor)
            {
                shot.reactionActor.lookTarget = shot.reactionTarget;
                shot.reactionActor.lookWeight = Mathf.SmoothStep(0, .65f, progress * 3);
            }
            if (hideout) hideout.Present(index, progress);
            if (firstPerson) firstPerson.Present(this, index, progress);
        }

        IEnumerator EndFilm()
        {
            Loading = true; subtitle.text = ""; title.text = dialogue.chapterTitle; hint.text = "";
            voice.Stop(); sound.Stop();
            for (float t = 0; t < 1; t += FrameDelta)
            { curtain.color = new Color(0, 0, 0, t); yield return null; }
            curtain.color = Color.black;
            for (float t = 0; t < 2; t += FrameDelta) yield return null;
            // 완료와 스킵은 같은 목적지로 연결된다.
            PlayerPrefs.SetInt("Mok.Story.PrisonAftermathSeen", 1);
            PlayerPrefs.Save(); Finished = true;
            if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
            {
                Time.timeScale = 1;
                yield return SceneManager.LoadSceneAsync(nextScene);
            }
            else { Loading = false; title.text = dialogue.chapterTitle; }
        }

        void OnDestroy()
        {
            Time.timeScale = originalTimeScale > 0 ? originalTimeScale : 1;
            if (!Loading) { Cursor.lockState = oldLock; Cursor.visible = oldCursor; }
        }
    }
}

