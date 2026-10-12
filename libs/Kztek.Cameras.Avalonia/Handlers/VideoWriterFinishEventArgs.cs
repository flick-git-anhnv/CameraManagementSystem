using System;

namespace Kztek.Cameras
{
    public class VideoWriterFinishEventArgs : EventArgs
    {
        private DateTime startTime = DateTime.Now;
        private DateTime stopTime = DateTime.Now;
        private string fileName = "";
        public DateTime StartTime
        {
            get { return this.startTime; }
            set { this.startTime = value; }
        }
        public DateTime StopTime
        {
            get { return this.stopTime; }
            set { this.stopTime = value; }
        }
        public string FileName
        {
            get { return this.fileName; }
            set { this.fileName = value; }
        }
        public VideoWriterFinishEventArgs()
        {
        }
        public VideoWriterFinishEventArgs(DateTime startTime, DateTime stopTime, string fileName)
        {
            this.startTime = startTime;
            this.stopTime = stopTime;
            this.fileName = fileName;
        }
    }
}
