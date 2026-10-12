using ANV.Cameras.Enums;
using System.Runtime.InteropServices;

namespace ANV.Cameras.Objects
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct AVOption
    {
        public nint name;
        public nint help;
        public int offset;
        public AVOptionType type;
        public AVOptionDefaultValue default_val;
        public double min;
        public double max;
        public int flags;
        public nint unit;
    }
}
