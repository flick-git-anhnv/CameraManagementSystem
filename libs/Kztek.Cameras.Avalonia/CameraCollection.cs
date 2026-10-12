// G4.4: Port từ CameraCollection.cs (WinForms).
// Không có System.Drawing dependency — copy nguyên vẹn.
using System.Collections;
using System.Collections.Generic;

namespace Kztek.Cameras
{
    public class CameraCollection : CollectionBase
    {
        public CameraCollection()
        {
        }

        public CameraCollection(List<Camera> cameras)
        {
            this.InnerList.Clear();
            foreach (var item in cameras)
            {
                this.InnerList.Add(item);
            }
        }

        public Camera this[int index]
        {
            get { return ((Camera)InnerList[index]); }
        }

        public void Add(Camera camera)
        {
            InnerList.Add(camera);
        }

        public void Remove(Camera camera)
        {
            InnerList.Remove(camera);
        }

        public Camera GetCamera_ByID(string cameraID)
        {
            foreach (Camera camera in InnerList)
            {
                if (camera.ID == cameraID)
                    return camera;
            }
            return null;
        }

        public Camera GetCamera_ByName(string cameraName)
        {
            foreach (Camera camera in InnerList)
            {
                if (camera.Name == cameraName)
                    return camera;
            }
            return null;
        }
    }
}
