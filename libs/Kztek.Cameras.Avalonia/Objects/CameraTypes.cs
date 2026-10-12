using System;

namespace Kztek.Cameras
{
    public class CameraTypes
    {
        public static CameraType GetType(string cameraType)
        {
            return (CameraType)Enum.Parse(typeof(CameraType), cameraType, true);
        }
        public static CameraType GetType(int index)
        {
            return (CameraType)index;
        }
    }
}
