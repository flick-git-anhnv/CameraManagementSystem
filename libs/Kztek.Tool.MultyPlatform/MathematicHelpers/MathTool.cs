using System;

namespace Kztek.Tool.MathematicHelpers
{
    public class MathTool
    {
        public static long HexToDec(string hexNumber)
        {
            return Convert.ToInt64(hexNumber, 16);
        }
        public static string HexToBin(string hexNumber, int binaryLength)
        {
            string binData = Convert.ToString(HexToDec(hexNumber), 2);
            while (binData.Length < 8)
            {
                binData = "0" + binData;
            }
            return binData;
        }

        public static int GetMin(params int[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Phải truyền ít nhất một giá trị", nameof(values));

            int min = values[0];
            foreach (int v in values)
            {
                if (v < min) min = v;
            }
            return min;
        }

    }
}

