namespace Kztek.Cameras
{
    public interface IAlarmSource
    {
        event AlarmSourceEventHandler NewAlarmSourceEvent;
        event ErrorEventHandler NewAlarmSourceErrorEvent;
        string Source { get; set; }
        string Login { get; set; }
        string Password { get; set; }
        void Start();
        void Stop();
        void SignalToStop();
    }
}
