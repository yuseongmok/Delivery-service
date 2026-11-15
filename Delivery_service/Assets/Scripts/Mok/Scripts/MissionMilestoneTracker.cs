namespace DeliveryService.Mok
{
    // 완료는 한 번만, 재도전 시작은 다시 알린다. 미션 상태에는 쓰지 않는다.
    public sealed class MissionMilestoneTracker
    {
        public enum Change { None, Started, Completed }
        private bool wasRunning, completed;
        public MissionMilestoneTracker(bool alreadyComplete){completed=alreadyComplete;}
        public Change Observe(bool running,bool complete)
        {
            if(complete&&!completed){completed=true;wasRunning=false;return Change.Completed;}
            bool started=running&&!wasRunning&&!completed;wasRunning=running;
            return started?Change.Started:Change.None;
        }
    }
}
