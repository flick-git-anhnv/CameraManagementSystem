namespace iParkingv8.Object.ConfigObjects.AppConfigs
{
    public class AppViewModeConfig
    {
        public enum EmAppViewMode
        {
            Optional = 0,
            Horizontal = 1,
            Vertical = 2,
        }
        public int ViewMode { get; set; } = 1;
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
    }
}
