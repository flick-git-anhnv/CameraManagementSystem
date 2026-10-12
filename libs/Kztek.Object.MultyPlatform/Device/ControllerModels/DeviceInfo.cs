namespace Kztek.Object
{
    public class DeviceInfo
    {
        public string DeviceName
        {
            get;
            set;
        } = string.Empty;

        public string FirmwareVersion
        {
            get;
            set;
        } = string.Empty;

        public string Ip
        {
            get;
            set;
        } = string.Empty;

        public int Port
        {
            get;
            set;
        }

        public string MacAddress
        {
            get;
            set;
        } = string.Empty;
    }
}
