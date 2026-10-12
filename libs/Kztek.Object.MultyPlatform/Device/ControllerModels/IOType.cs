namespace Kztek.Object
{
    /// <summary>Phân loại chiều của tín hiệu I/O trên thiết bị.</summary>
    public class IOType
    {
        /// <summary>Chiều tín hiệu I/O.</summary>
        public enum EmIOType
        {
            /// <summary>Tín hiệu đầu vào (cảm biến vòng từ, nút bấm, v.v.).</summary>
            Input = 0,
            /// <summary>Tín hiệu đầu ra (relay, còi, đèn, v.v.).</summary>
            Output = 1
        }
    }
}
