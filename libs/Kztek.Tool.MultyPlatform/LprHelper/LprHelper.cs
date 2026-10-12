using System.Text.RegularExpressions;

namespace Kztek.Tool.LprHelper
{
    /// <summary>
    /// Tiện ích chuẩn hóa chuỗi biển số xe với logic heuristic nhận dạng ký tự nhầm
    /// (BB→88, B→8, D→0, Q/O→0, Z→2) dùng cho LPR thế hệ cũ.
    /// <para>Lưu ý: <see cref="ParkingV8.Lpr.PlateHelper"/> là phiên bản mới hơn, đã loại bỏ heuristic
    /// vì engine AI hiện đại xử lý tốt hơn ở model.</para>
    /// </summary>
    public class LprHelper
    {
        /// <summary>
        /// Chuẩn hóa biển số: áp dụng heuristic ký tự nhầm rồi lọc ký tự không hợp lệ.
        /// </summary>
        /// <param name="plate">Biển số thô từ engine LPR cũ.</param>
        /// <returns>Biển số đã chuẩn hóa (chỉ <c>[a-zA-Z0-9;]</c>).</returns>
        public static string StandardPlate(string plate)
        {
            if (plate.Length > 3 && plate[2].ToString() == "-")
            {
                plate = plate.Remove(2, 1);
            }
            if (plate.Length > 2)
            {
                if (plate.Substring(0, 2) == "BB")
                {
                    plate = "88" + plate.Substring(2);
                }
                else if (plate[1] == 'B')
                {
                    plate = plate[0] + "8" + plate.Substring(2);
                }
                else if (plate[1] == 'D')
                {
                    plate = plate[0] + "0" + plate.Substring(2);
                }
            }
            plate = plate.Replace('Q', '0');
            plate = plate.Replace('O', '0');
            plate = plate.Replace('Z', '2');
            if (plate.Length > 3)
            {
                if (plate[2] == '8')
                {
                    plate = plate.Substring(0, 2) + "B" + plate.Substring(3);
                }
                for (int i = 3; i < plate.Length - 1; i++)
                {
                    if (plate[i] == 'D' && i > 3)
                    {
                        plate = plate.Substring(0, i) + "0" + plate.Substring(i + 1);
                    }
                    else if (plate[i] == 'B')
                    {
                        plate = plate.Substring(0, i) + "8" + plate.Substring(i + 1);
                    }
                }
            }
            plate = StandardlizePlateNumber(plate);
            return plate;
        }
        /// <summary>
        /// Lọc biển số: chỉ giữ ký tự <c>[a-zA-Z0-9;]</c>, bỏ mọi ký tự đặc biệt còn lại.
        /// </summary>
        /// <param name="plateNumber">Biển số cần lọc.</param>
        /// <returns>Biển số chỉ gồm ký tự alphanumeric.</returns>
        public static string StandardlizePlateNumber(string plateNumber)
        {
            string output = string.Empty;
            Regex regex = new Regex("[a-zA-Z0-9;]");
            for (int i = 0; i < plateNumber.Length; i++)
            {
                string plateNumberItem = plateNumber[i].ToString();
                if (regex.IsMatch(plateNumberItem))
                {
                    output += plateNumberItem;
                }
            }
            return output;
        }
    }
}

