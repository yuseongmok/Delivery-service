#if UNITY_EDITOR
namespace DeliveryService.Yun.Editor
{
    // Retain the previous batch entry point without restoring the superseded single-engine clip.
    public static class MotorcycleDrivingAudioSetup
    {
        public static void RunBatch() { EngineBlendSetup.RunBatch(); }
    }
}
#endif
