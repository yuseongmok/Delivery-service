using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/Challenge Rules")]
    public sealed class ChallengeRules : ScriptableObject
    {
        [Header("하루 고정비")]
        [Min(0)] public int rent = 3000;
        [Min(0)] public int utilities = 1000;
        [Min(0)] public int dailyIncrease = 500;
        [Header("영업 시간 (8시~20시)")]
        [Min(60)] public float businessSeconds = 480;
        private static ChallengeRules current;
        public static ChallengeRules Current
        {
            get
            {
                if (!current) current = Resources.Load<ChallengeRules>("ChallengeRules");
                if (!current) current = CreateInstance<ChallengeRules>();
                return current;
            }
        }
        public int FixedCost(int day) => rent + utilities + Mathf.Max(0,day-1) * dailyIncrease;
    }
}
