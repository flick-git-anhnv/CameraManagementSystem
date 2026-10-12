namespace Kztek.Object
{
    /// <summary>Nhóm loại engine nhận dạng biển số xe (LPR — License Plate Recognition).</summary>
    public class LprDetecter
    {
        /// <summary>
        /// Loại engine LPR sử dụng để nhận dạng biển số xe từ camera.
        /// </summary>
        public enum EmLprDetecter
        {
            /// <summary>Engine LPR cũ của KZTEK (kztekLpr — phiên bản đầu, ít dùng).</summary>
            KztekLpr,
            /// <summary>Engine LPR của American (tích hợp bên thứ ba).</summary>
            AmericalLpr,
            /// <summary>KZTEK LPR AI Server — phiên bản 1 (AI server riêng).</summary>
            KztekLPRAIServer,
            /// <summary>KZTEK LPR AI Server V2 — phiên bản mới nhất, được dùng mặc định.</summary>
            KztekLPRAIServerV2
        }

        /// <summary>
        /// Trả về chuỗi hiển thị tên engine LPR.
        /// </summary>
        /// <param name="lprDetecter">Loại engine LPR.</param>
        /// <returns>Tên hiển thị dạng chuỗi.</returns>
        public static string ToString(EmLprDetecter lprDetecter)
        {
            switch (lprDetecter)
            {
                case EmLprDetecter.KztekLpr:
                    return "kztekLpr";
                case EmLprDetecter.AmericalLpr:
                    return "Americal Lpr";
                case EmLprDetecter.KztekLPRAIServer:
                    return "Kztek Lpr AI Server";
                case EmLprDetecter.KztekLPRAIServerV2:
                    return "KztekLPRAIServerV2";
                default:
                    return string.Empty;
            }
        }
    }
}
