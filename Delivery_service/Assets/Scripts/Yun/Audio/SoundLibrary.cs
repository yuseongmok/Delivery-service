using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Yun.Audio
{
    [Serializable]
    public class SoundEntry
    {
        [Tooltip("이 슬롯이 가리키는 사운드 ID.")]
        public SoundId id;

        [Tooltip("Master와 함께 적용되는 볼륨 그룹.")]
        public SoundCategory category = SoundCategory.Sfx;

        [Tooltip("재생 시 비어 있지 않은 클립 중 하나를 무작위로 고른다. 비어 있어도 유효한 슬롯이다.")]
        public AudioClip[] clips = new AudioClip[1];

        [Range(0f, 1f)]
        [Tooltip("이 사운드의 기본 볼륨.")]
        public float volume = 1f;

        [Tooltip("X는 최소, Y는 최대. 재생마다 이 구간에서 피치를 고른다.")]
        public Vector2 pitchRange = Vector2.one;

        [Tooltip("켜면 핸들로 멈출 때까지 반복한다.")]
        public bool loop;

        [Tooltip("2D는 거리와 무관하고, 3D는 위치와 거리에 따라 감쇠한다.")]
        public SoundSpatialMode spatialMode = SoundSpatialMode.ThreeDimensional;

        [Min(0f)]
        [Tooltip("3D일 때 볼륨이 줄어들기 시작하는 거리.")]
        public float minDistance = 1f;

        [Min(0.01f)]
        [Tooltip("3D일 때 더 이상 거의 들리지 않는 거리.")]
        public float maxDistance = 25f;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int pick = UnityEngine.Random.Range(0, validCount);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return clips[i];
                }

                pick--;
            }

            return null;
        }
    }

    [CreateAssetMenu(
        fileName = "SoundLibrary",
        menuName = "Delivery Service/Yun/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("사운드 슬롯")]
        [Tooltip("SoundId별 음원과 재생 설정. 음원이 비어 있어도 슬롯은 유지된다.")]
        [SerializeField]
        private List<SoundEntry> entries = new List<SoundEntry>();

        private readonly Dictionary<SoundId, SoundEntry> lookup = new Dictionary<SoundId, SoundEntry>();

        public IReadOnlyList<SoundEntry> Entries => entries;

        public int EntryCount => entries != null ? entries.Count : 0;

        public static int DefinedSoundIdCount => Enum.GetValues(typeof(SoundId)).Length;

        public int CountMissingIds()
        {
            var existing = new HashSet<SoundId>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null)
                    {
                        existing.Add(entries[i].id);
                    }
                }
            }

            return DefinedSoundIdCount - existing.Count;
        }

        public bool TryGetEntry(SoundId id, out SoundEntry entry)
        {
            RebuildLookup();
            return lookup.TryGetValue(id, out entry);
        }

        /// <summary>
        /// 아직 없는 SoundId만 빈 슬롯으로 추가한다. 기존 음원과 설정은 그대로 둔다.
        /// </summary>
        public int FillMissingEntries()
        {
            if (entries == null)
            {
                entries = new List<SoundEntry>();
            }

            var existing = new HashSet<SoundId>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                {
                    existing.Add(entries[i].id);
                }
            }

            int added = 0;
            Array values = Enum.GetValues(typeof(SoundId));
            for (int i = 0; i < values.Length; i++)
            {
                var id = (SoundId)values.GetValue(i);
                if (existing.Contains(id))
                {
                    continue;
                }

                entries.Add(SoundSlotDefaults.CreateEmptySlot(id));
                existing.Add(id);
                added++;
            }

            return added;
        }

        public void CollectIssues(List<string> issues)
        {
            if (issues == null)
            {
                return;
            }

            issues.Clear();
            if (entries == null)
            {
                return;
            }

            var firstIndex = new Dictionary<SoundId, int>();
            for (int i = 0; i < entries.Count; i++)
            {
                SoundEntry entry = entries[i];
                if (entry == null)
                {
                    issues.Add($"[{i}] 비어 있는 항목이 있다.");
                    continue;
                }

                if (firstIndex.ContainsKey(entry.id))
                {
                    issues.Add($"[{i}] {entry.id} 가 [{firstIndex[entry.id]}] 과 중복이다. 재생 시 앞 항목만 사용한다.");
                }
                else
                {
                    firstIndex.Add(entry.id, i);
                }

                if (entry.pitchRange.x > entry.pitchRange.y)
                {
                    issues.Add($"[{i}] {entry.id} 피치 범위가 뒤집혀 있다. ({entry.pitchRange.x} > {entry.pitchRange.y})");
                }

                if (entry.minDistance < 0f)
                {
                    issues.Add($"[{i}] {entry.id} 최소 거리가 0보다 작다.");
                }

                if (entry.maxDistance <= 0f)
                {
                    issues.Add($"[{i}] {entry.id} 최대 거리가 0 이하이다.");
                }
                else if (entry.minDistance > entry.maxDistance)
                {
                    issues.Add($"[{i}] {entry.id} 최소 거리가 최대 거리보다 크다.");
                }

                if (entry.volume < 0f || entry.volume > 1f)
                {
                    issues.Add($"[{i}] {entry.id} 볼륨이 0~1 범위를 벗어난다.");
                }
            }
        }

        private void RebuildLookup()
        {
            lookup.Clear();
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                SoundEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (!lookup.ContainsKey(entry.id))
                {
                    lookup.Add(entry.id, entry);
                }
            }
        }
    }

    internal static class SoundSlotDefaults
    {
        public static SoundEntry CreateEmptySlot(SoundId id)
        {
            var entry = new SoundEntry
            {
                id = id,
                clips = new AudioClip[1],
                volume = 1f,
                pitchRange = Vector2.one,
                loop = false,
                category = SoundCategory.Sfx,
                spatialMode = SoundSpatialMode.ThreeDimensional,
                minDistance = 1f,
                maxDistance = 25f
            };

            Apply(entry);
            return entry;
        }

        private static void Apply(SoundEntry entry)
        {
            switch (entry.id)
            {
                case SoundId.PlayerWalkFootstep:
                case SoundId.PlayerRunFootstep:
                case SoundId.NpcFootstep:
                    entry.pitchRange = new Vector2(0.92f, 1.08f);
                    break;

                case SoundId.OrderTicketShow:
                case SoundId.OrderAccept:
                case SoundId.OrderReject:
                case SoundId.DeliverySuccess:
                case SoundId.DeliveryRefuse:
                case SoundId.UiOpen:
                case SoundId.UiClose:
                case SoundId.UiClick:
                case SoundId.SettlementComplete:
                case SoundId.MoneyIncome:
                case SoundId.MoneyExpense:
                case SoundId.PurchaseSuccess:
                case SoundId.PurchaseFail:
                case SoundId.SettlementChoice:
                case SoundId.FleeChoice:
                    entry.category = SoundCategory.Ui;
                    entry.spatialMode = SoundSpatialMode.TwoDimensional;
                    break;

                case SoundId.OvenLoop:
                case SoundId.MotorcycleEngineLoop:
                case SoundId.MotorcycleWindLoop:
                case SoundId.MotorcycleBreakdownLoop:
                case SoundId.FuelLoop:
                case SoundId.VehicleEngineLoop:
                case SoundId.VehicleDriveLoop:
                case SoundId.RepairLoop:
                    entry.loop = true;
                    break;

                case SoundId.ShopAmbienceLoop:
                case SoundId.CityDayAmbienceLoop:
                case SoundId.CityNightAmbienceLoop:
                    entry.category = SoundCategory.Ambience;
                    entry.loop = true;
                    entry.spatialMode = SoundSpatialMode.TwoDimensional;
                    entry.volume = 0.4f;
                    break;

                case SoundId.GameBgm:
                    entry.category = SoundCategory.Bgm;
                    entry.loop = true;
                    entry.spatialMode = SoundSpatialMode.TwoDimensional;
                    entry.volume = 0.5f;
                    break;

                case SoundId.WarmthLowWarning:
                case SoundId.DeliveryTimeWarning:
                case SoundId.BusinessOpen:
                case SoundId.BusinessClose:
                case SoundId.NextDayStart:
                case SoundId.RentPayment:
                case SoundId.Bankruptcy:
                case SoundId.WantedStart:
                case SoundId.ChaseEnd:
                    entry.category = SoundCategory.Ui;
                    entry.spatialMode = SoundSpatialMode.TwoDimensional;
                    break;

                case SoundId.PoliceSirenLoop:
                    entry.loop = true;
                    entry.maxDistance = 60f;
                    break;
            }
        }
    }
}
