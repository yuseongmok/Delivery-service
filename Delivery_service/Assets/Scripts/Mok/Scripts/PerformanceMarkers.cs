using Unity.Profiling;
namespace DeliveryService.Mok {public static class PerformanceMarkers {
 public static readonly ProfilerMarker Traffic=new("Mok.TrafficLeader"),Route=new("Mok.Route"),Planner=new("Mok.Planner"),HUD=new("Mok.HUD"),Merchant=new("Mok.Merchant");
}}
