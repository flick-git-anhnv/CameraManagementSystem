using System;

namespace Kztek.Cameras
{
    public class StreamTypes
    {
        public static StreamType GetType(string streamType)
        {
            return (StreamType)Enum.Parse(typeof(StreamType), streamType, true);
        }
        public static StreamType GetType(int index)
        {
            return (StreamType)index;
        }
    }
}
