using System.Numerics;

namespace iParkingv8.Object.Objects.Bases
{
    public class Attributes
    {
        public string Code { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        /// <summary>
        /// string <br/>
        /// number <br/>
        /// boolean <br/>
        /// </summary>
        public string ValueType { get; set; } = string.Empty;

        public object GetValue()
        {
            switch (ValueType)
            {
                case "string":
                    return Value;
                case "number":
                    return BigInteger.Parse(Value);
                case "boolean": return bool.Parse(Value);
                default:
                    return Value;
            }
        }
    }

}
