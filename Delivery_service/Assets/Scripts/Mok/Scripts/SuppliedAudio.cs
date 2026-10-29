using UnityEngine;
namespace DeliveryService.Mok {
// 원본은 보존하고 게임용으로 정리한 음원을 공유한다.
public sealed class SuppliedAudio : ScriptableObject {
 public AudioClip gunshot,reload,flyby,bankAlarm,policeSiren,playerEngine;
 public AudioClip vaultLatch,vaultSwing,bodyFall,bag,shutter,missionMusic,carCrash,carOpen,carClose,wallImpact,metalImpact;
 public AudioClip cityAmbience,roomAmbience,detected,escalation;
 public AudioClip[] traffic,indoorSteps,streetSteps,boots;
 static SuppliedAudio current;
 public static SuppliedAudio Current=>current?current:current=Resources.Load<SuppliedAudio>("SuppliedAudio");
 public static float Volume=>Mathf.Clamp01(PlayerPrefs.GetFloat("Yun.Settings.SfxVolume",1));
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){current=null;}
}
}
