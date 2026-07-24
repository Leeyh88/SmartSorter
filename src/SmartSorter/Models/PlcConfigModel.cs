namespace SmartSorter.Models
{
    public class PlcConfigModel
    {
        public string PortName { get; set; } = "COM9";
        public int BaudRate { get; set; } = 9600;
        public byte StationId { get; set; } = 1;
    }
}