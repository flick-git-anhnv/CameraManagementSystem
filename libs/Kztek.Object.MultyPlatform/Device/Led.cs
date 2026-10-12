namespace Kztek.Object
{
    public class Led
    {
        public string id { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string computerId { get; set; }
        public string comport { get; set; }
        public int baudrate { get; set; }
        private int _type = 0;

        public int type { get => _type; set { _type = value; } }
        public int typeCode { get => _type; set { _type = value; } }
        public object description { get; set; }
        public int row { get; set; }
        public bool enabled { get; set; }
        public bool deleted { get; set; }
        public string createdUtc { get; set; }
        public string createdBy { get; set; }
        public object updatedUtc { get; set; }
        public object updatedBy { get; set; }
        public object computer { get; set; }
    }
}
