namespace Kztek.Object
{
    public class ControllerConfig
    {
        public string comport { get; set; }
        public int baudrate { get; set; }
        public int port { get; set; }
        public int input_bao_cua_mo { get; set; }
        public int input_bao_may_pos { get; set; }

        public ControllerConfig(string comport, int input_bao_cua_mo, int input_bao_may_pos, int port)
        {
            this.comport = comport;
            this.input_bao_may_pos = input_bao_may_pos;
            this.input_bao_cua_mo = input_bao_cua_mo;
            this.baudrate = 100;
            this.port = 100;
        }
    }
}
