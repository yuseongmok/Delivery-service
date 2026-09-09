using UnityEngine;

namespace DeliveryService.Yun.Audio
{
    [AddComponentMenu("Delivery Service/Yun/Audio Manager")]
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public const int DefaultMaxVoices = 32;
        public const int AbsoluteMaxVoices = 64;

        public static AudioManager Instance { get; private set; }

        [Header("Library")]
        [Tooltip("SoundId별 음원과 재생 설정.")]
        [SerializeField]
        private SoundLibrary library;

        [Header("Pool")]
        [Tooltip("동시에 사용할 AudioSource 수. 시작 시 만들어지며 재생 중 변경은 반영되지 않는다.")]
        [SerializeField]
        [Range(1, AbsoluteMaxVoices)]
        private int maxVoices = DefaultMaxVoices;

        [Header("Master")]
        [Tooltip("전체 볼륨. 이미 재생 중인 소리에도 바로 적용된다.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float masterVolume = 1f;

        [Tooltip("켜면 모든 카테고리가 묵음이 된다.")]
        [SerializeField]
        private bool masterMuted;

        [Header("BGM")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("배경음악 볼륨.")]
        private float bgmVolume = 1f;

        [SerializeField]
        [Tooltip("배경음악 음소거.")]
        private bool bgmMuted;

        [Header("SFX")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("효과음 볼륨.")]
        private float sfxVolume = 1f;

        [SerializeField]
        [Tooltip("효과음 음소거.")]
        private bool sfxMuted;

        [Header("UI")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("UI 사운드 볼륨.")]
        private float uiVolume = 1f;

        [SerializeField]
        [Tooltip("UI 사운드 음소거.")]
        private bool uiMuted;

        [Header("Ambience")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("환경음 볼륨.")]
        private float ambienceVolume = 1f;

        [SerializeField]
        [Tooltip("환경음 음소거.")]
        private bool ambienceMuted;

        private Transform voiceRoot;
        private Voice[] voices;
        private bool initialized;

        public SoundLibrary Library => library;

        public int MaxVoices => maxVoices;

        public int ActiveCount
        {
            get
            {
                CleanupVoices();
                if (voices == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < voices.Length; i++)
                {
                    if (voices[i] != null && voices[i].inUse)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                RefreshPlayingVolumes();
            }
        }

        public bool MasterMuted
        {
            get => masterMuted;
            set
            {
                masterMuted = value;
                RefreshPlayingVolumes();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            BuildPool();
        }

        private void OnDestroy()
        {
            StopAll();
            DestroyPoolObjects(false);
            if (Instance == this)
            {
                Instance = null;
            }

            initialized = false;
        }

        private void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            bgmVolume = Mathf.Clamp01(bgmVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            uiVolume = Mathf.Clamp01(uiVolume);
            ambienceVolume = Mathf.Clamp01(ambienceVolume);
            maxVoices = Mathf.Clamp(maxVoices, 1, AbsoluteMaxVoices);
            if (initialized)
            {
                RefreshPlayingVolumes();
            }
        }

        private void LateUpdate()
        {
            CleanupVoices();
            UpdateFollowPositions();
        }

        public void SetLibrary(SoundLibrary value)
        {
            library = value;
        }

        public float GetCategoryVolume(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.Bgm:
                    return bgmVolume;
                case SoundCategory.Ui:
                    return uiVolume;
                case SoundCategory.Ambience:
                    return ambienceVolume;
                default:
                    return sfxVolume;
            }
        }

        public void SetCategoryVolume(SoundCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (category)
            {
                case SoundCategory.Bgm:
                    bgmVolume = volume;
                    break;
                case SoundCategory.Ui:
                    uiVolume = volume;
                    break;
                case SoundCategory.Ambience:
                    ambienceVolume = volume;
                    break;
                default:
                    sfxVolume = volume;
                    break;
            }

            RefreshPlayingVolumes();
        }

        public bool GetCategoryMuted(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.Bgm:
                    return bgmMuted;
                case SoundCategory.Ui:
                    return uiMuted;
                case SoundCategory.Ambience:
                    return ambienceMuted;
                default:
                    return sfxMuted;
            }
        }

        public void SetCategoryMuted(SoundCategory category, bool muted)
        {
            switch (category)
            {
                case SoundCategory.Bgm:
                    bgmMuted = muted;
                    break;
                case SoundCategory.Ui:
                    uiMuted = muted;
                    break;
                case SoundCategory.Ambience:
                    ambienceMuted = muted;
                    break;
                default:
                    sfxMuted = muted;
                    break;
            }

            RefreshPlayingVolumes();
        }

        public SoundHandle Play(SoundId id)
        {
            return PlayInternal(id, PlayPlacement.LibraryDefault, Vector3.zero, null);
        }

        public SoundHandle PlayAt(SoundId id, Vector3 worldPosition)
        {
            return PlayInternal(id, PlayPlacement.WorldPosition, worldPosition, null);
        }

        public SoundHandle PlayFollow(SoundId id, Transform target)
        {
            if (target == null)
            {
                return SoundHandle.Invalid;
            }

            return PlayInternal(id, PlayPlacement.Follow, target.position, target);
        }

        public void Stop(SoundHandle handle)
        {
            Voice voice = GetVoice(handle);
            if (voice != null)
            {
                Release(voice);
            }
        }

        public void Stop(SoundId id)
        {
            if (voices == null)
            {
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice != null && voice.inUse && voice.soundId == id)
                {
                    Release(voice);
                }
            }
        }

        public void StopAll()
        {
            if (voices == null)
            {
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice != null && voice.inUse)
                {
                    Release(voice);
                }
            }
        }

        public void SetVolume(SoundHandle handle, float volume)
        {
            Voice voice = GetVoice(handle);
            if (voice == null)
            {
                return;
            }

            voice.instanceVolume = Mathf.Clamp01(volume);
            ApplyVoiceVolume(voice);
        }

        public void SetPitch(SoundHandle handle, float pitch)
        {
            Voice voice = GetVoice(handle);
            if (voice == null || voice.source == null)
            {
                return;
            }

            voice.instancePitch = pitch;
            voice.source.pitch = pitch;
        }

        public bool IsHandleValid(SoundHandle handle)
        {
            CleanupVoices();
            return GetVoice(handle) != null;
        }

        public bool IsPlaying(SoundHandle handle)
        {
            CleanupVoices();
            Voice voice = GetVoice(handle);
            return voice != null && voice.source != null && voice.source.isPlaying;
        }

#if UNITY_EDITOR
        public void RebuildPoolForEditorTests(int voiceCount)
        {
            maxVoices = Mathf.Clamp(voiceCount, 1, AbsoluteMaxVoices);
            BuildPool();
        }
#endif

        private SoundHandle PlayInternal(
            SoundId id,
            PlayPlacement placement,
            Vector3 worldPosition,
            Transform followTarget)
        {
            CleanupVoices();

            if (library == null || !library.TryGetEntry(id, out SoundEntry entry) || entry == null)
            {
                return SoundHandle.Invalid;
            }

            AudioClip clip = entry.PickClip();
            if (clip == null)
            {
                return SoundHandle.Invalid;
            }

            Voice voice = AcquireVoice();
            if (voice == null)
            {
                return SoundHandle.Invalid;
            }

            bool use3D = placement != PlayPlacement.LibraryDefault
                || entry.spatialMode == SoundSpatialMode.ThreeDimensional;
            Vector3 position = transform.position;
            if (placement == PlayPlacement.WorldPosition || placement == PlayPlacement.Follow)
            {
                position = worldPosition;
            }

            ConfigureSource(voice.source, entry, clip, use3D, position);
            voice.inUse = true;
            voice.loop = entry.loop;
            voice.soundId = id;
            voice.category = entry.category;
            voice.entryVolume = Mathf.Clamp01(entry.volume);
            voice.instanceVolume = 1f;
            voice.instancePitch = PickPitch(entry.pitchRange);
            voice.follow = placement == PlayPlacement.Follow;
            voice.followTarget = voice.follow ? followTarget : null;
            voice.startedAt = Time.unscaledTime;
            voice.source.pitch = voice.instancePitch;
            ApplyVoiceVolume(voice);
            voice.source.Play();
            return new SoundHandle(voice.index, voice.generation);
        }

        private Voice AcquireVoice()
        {
            EnsurePool();
            Voice idle = null;
            Voice oldestOneShot = null;
            float oldestTime = float.PositiveInfinity;

            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice == null)
                {
                    continue;
                }

                if (!voice.inUse)
                {
                    idle = voice;
                    break;
                }

                if (!voice.loop && voice.startedAt < oldestTime)
                {
                    oldestTime = voice.startedAt;
                    oldestOneShot = voice;
                }
            }

            if (idle != null)
            {
                PrepareForReuse(idle);
                return idle;
            }

            if (oldestOneShot != null)
            {
                Release(oldestOneShot);
                PrepareForReuse(oldestOneShot);
                return oldestOneShot;
            }

            return null;
        }

        private void PrepareForReuse(Voice voice)
        {
            if (voice.generation == 0)
            {
                voice.generation = 1;
            }
        }

        private Voice GetVoice(SoundHandle handle)
        {
            if (voices == null || handle.index < 0 || handle.index >= voices.Length || handle.generation == 0)
            {
                return null;
            }

            Voice voice = voices[handle.index];
            if (voice == null || !voice.inUse || voice.generation != handle.generation)
            {
                return null;
            }

            return voice;
        }

        private void CleanupVoices()
        {
            if (voices == null)
            {
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice == null || !voice.inUse)
                {
                    continue;
                }

                if (voice.follow && voice.followTarget == null)
                {
                    Release(voice);
                    continue;
                }

                if (voice.source == null || !voice.source.isPlaying)
                {
                    Release(voice);
                }
            }
        }

        private void UpdateFollowPositions()
        {
            if (voices == null)
            {
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice == null || !voice.inUse || !voice.follow || voice.followTarget == null || voice.source == null)
                {
                    continue;
                }

                voice.source.transform.position = voice.followTarget.position;
            }
        }

        private void Release(Voice voice)
        {
            if (voice == null || !voice.inUse)
            {
                return;
            }

            if (voice.source != null)
            {
                voice.source.Stop();
                ResetSource(voice.source);
            }

            voice.inUse = false;
            voice.loop = false;
            voice.follow = false;
            voice.followTarget = null;
            voice.entryVolume = 1f;
            voice.instanceVolume = 1f;
            voice.instancePitch = 1f;
            voice.generation++;
            if (voice.generation == 0)
            {
                voice.generation = 1;
            }
        }

        private void RefreshPlayingVolumes()
        {
            if (voices == null)
            {
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice != null && voice.inUse)
                {
                    ApplyVoiceVolume(voice);
                }
            }
        }

        private void ApplyVoiceVolume(Voice voice)
        {
            if (voice == null || voice.source == null)
            {
                return;
            }

            bool muted = masterMuted || GetCategoryMuted(voice.category);
            if (muted)
            {
                voice.source.volume = 0f;
                return;
            }

            voice.source.volume = masterVolume
                * GetCategoryVolume(voice.category)
                * voice.entryVolume
                * voice.instanceVolume;
        }

        private void BuildPool()
        {
            DestroyPoolObjects(true);
            maxVoices = Mathf.Clamp(maxVoices, 1, AbsoluteMaxVoices);

            var rootObject = new GameObject("Voices");
            rootObject.transform.SetParent(transform, false);
            voiceRoot = rootObject.transform;

            voices = new Voice[maxVoices];
            for (int i = 0; i < maxVoices; i++)
            {
                var voiceObject = new GameObject("Voice_" + i.ToString("00"));
                voiceObject.transform.SetParent(voiceRoot, false);
                AudioSource source = voiceObject.AddComponent<AudioSource>();
                ResetSource(source);
                voices[i] = new Voice
                {
                    index = i,
                    source = source,
                    generation = 1
                };
            }

            initialized = true;
        }

        private void EnsurePool()
        {
            if (!initialized || voices == null)
            {
                BuildPool();
            }
        }

        private void DestroyPoolObjects(bool immediate)
        {
            if (voices != null)
            {
                for (int i = 0; i < voices.Length; i++)
                {
                    Voice voice = voices[i];
                    if (voice != null && voice.source != null)
                    {
                        voice.source.Stop();
                    }
                }
            }

            voices = null;
            if (voiceRoot != null)
            {
                GameObject rootObject = voiceRoot.gameObject;
                voiceRoot = null;
                if (immediate || !Application.isPlaying)
                {
                    DestroyImmediate(rootObject);
                }
                else
                {
                    Destroy(rootObject);
                }
            }

            initialized = false;
        }

        private static void ConfigureSource(
            AudioSource source,
            SoundEntry entry,
            AudioClip clip,
            bool use3D,
            Vector3 worldPosition)
        {
            ResetSource(source);
            source.clip = clip;
            source.loop = entry.loop;
            source.spatialBlend = use3D ? 1f : 0f;
            source.minDistance = Mathf.Max(0f, entry.minDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 0.01f, entry.maxDistance);
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.transform.position = worldPosition;
        }

        private static void ResetSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.mute = false;
            source.bypassEffects = false;
            source.bypassListenerEffects = false;
            source.bypassReverbZones = false;
            source.playOnAwake = false;
            source.loop = false;
            source.priority = 128;
            source.volume = 1f;
            source.pitch = 1f;
            source.panStereo = 0f;
            source.spatialBlend = 0f;
            source.reverbZoneMix = 1f;
            source.dopplerLevel = 0f;
            source.spread = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 500f;
            source.ignoreListenerVolume = false;
            source.ignoreListenerPause = false;
            source.spatialize = false;
            source.transform.localPosition = Vector3.zero;
            source.transform.localRotation = Quaternion.identity;
            source.transform.localScale = Vector3.one;
        }

        private static float PickPitch(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            if (Mathf.Approximately(min, max))
            {
                return min;
            }

            return UnityEngine.Random.Range(min, max);
        }

        private enum PlayPlacement
        {
            LibraryDefault,
            WorldPosition,
            Follow
        }

        private sealed class Voice
        {
            public int index;
            public int generation;
            public bool inUse;
            public bool loop;
            public bool follow;
            public SoundId soundId;
            public SoundCategory category;
            public float entryVolume = 1f;
            public float instanceVolume = 1f;
            public float instancePitch = 1f;
            public float startedAt;
            public Transform followTarget;
            public AudioSource source;
        }
    }
}
