namespace Kztek.Object
{
    /// <summary>Phân loại đối tượng trong hệ thống theo nhóm chức năng.</summary>
    public enum EmObjectType
    {
        /// <summary>Thiết bị phần cứng (controller, camera, barrier, v.v.).</summary>
        DEVICE,
        /// <summary>Hệ thống nội bộ (ứng dụng, dịch vụ nền).</summary>
        SYSTEM,
        /// <summary>Máy chủ quản lý chính (server API).</summary>
        MAIN_SERVER,
        /// <summary>Hệ thống bên thứ ba tích hợp (thanh toán, kiểm soát ra vào, v.v.).</summary>
        THIRT_PARTY
    }
}
