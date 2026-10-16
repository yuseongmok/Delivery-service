using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
public static class CitizenCrime {
    public const float PoliceChance=.20f;
    static readonly HashSet<int> reported=new HashSet<int>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){reported.Clear();}
    public static bool ReportPlayerKill(NPCControl victim){
        if(victim==null||!victim.IsDead||!reported.Add(victim.GetInstanceID()))return false;
        if(Random.value>=PoliceChance)return false;
        var manager=Object.FindFirstObjectByType<PoliceManager>();if(manager==null)return false;
        manager.SpawnPoliceOnAccidentRefused();return true;
    }
}
}
