namespace CMS.Core.Entities;

/// <summary>
/// Vị trí của một camera trên sơ đồ mặt bằng (E-Map)
/// </summary>
public class CameraMapNode
{
    public string CameraId { get; set; } = string.Empty;
    public string CameraName { get; set; } = string.Empty;
    public double XPercent { get; set; } // 0.0 -> 100.0%
    public double YPercent { get; set; } // 0.0 -> 100.0%
    public double RotationAngle { get; set; } = 0.0; // Góc xoay hướng nhìn 0-360 độ
}

/// <summary>
/// Sơ đồ mặt bằng điện tử (E-Map)
/// </summary>
public class EmapConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Floor Plan 1";
    public string ImagePath { get; set; } = string.Empty;
    public List<CameraMapNode> CameraNodes { get; set; } = new();
}
