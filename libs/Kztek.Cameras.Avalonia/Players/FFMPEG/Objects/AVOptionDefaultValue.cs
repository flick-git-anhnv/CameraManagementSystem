using System.Runtime.InteropServices;

namespace ANV.Cameras.Objects
{
    [StructLayout(LayoutKind.Explicit)]
    public struct AVOptionDefaultValue
    {
        [FieldOffset(0)]
        public long i64;
        [FieldOffset(0)]
        public double dbl;
        [FieldOffset(0)]
        public nint str;
        [FieldOffset(0)]
        public AVRational q;
    }
}
