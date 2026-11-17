using UnityEngine;
namespace DeliveryService.Mok
{
    [CreateAssetMenu(menuName="Delivery Service/Prison cooking sounds")]
    public sealed class PrisonCookingSounds : ScriptableObject
    {
        public AudioClip chop, flour, liquid, stir, fry, brush, snore;
        public AudioClip pickup, tick, confirm, flip, detected;
        public AudioClip music, slice;
        public static PrisonCookingSounds Current => Resources.Load<PrisonCookingSounds>("PrisonCookingSounds");
    }
}
