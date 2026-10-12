using System;

namespace Kztek.Cameras
{
    public class ProtocolTypes
    {
        public static ProtocolType GetType(string protocolType)
        {
            return (ProtocolType)Enum.Parse(typeof(ProtocolType), protocolType, true);
        }
        public static ProtocolType GetType(int index)
        {
            return (ProtocolType)index;
        }
    }
}
