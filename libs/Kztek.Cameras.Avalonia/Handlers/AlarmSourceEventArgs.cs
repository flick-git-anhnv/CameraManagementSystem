using System;

namespace Kztek.Cameras
{
    public class AlarmSourceEventArgs : EventArgs
    {
        private string timeStamp = "";
        public bool motionDetectionStatus;
        private int[] ioState;
        public string TimeStamp
        {
            get { return this.timeStamp; }
            set { this.timeStamp = value; }
        }
        public bool MotionDetectionStatus
        {
            get { return this.motionDetectionStatus; }
            set { this.motionDetectionStatus = value; }
        }
        public int[] IOState
        {
            get { return this.ioState; }
            set { this.ioState = value; }
        }
        public AlarmSourceEventArgs()
        {
        }
        public AlarmSourceEventArgs(bool motionDetectionStatus, int[] ioState)
        {
            this.motionDetectionStatus = motionDetectionStatus;
            this.ioState = ioState;
        }
    }
}
