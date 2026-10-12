using System.Runtime.InteropServices;

namespace ANV.Cameras.Objects
{
    /// <summary>
    /// Filter container with separate horizontal/vertical luma and chroma filters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SwsFilter
    {
        public nint lumH; // SwsVector*
        public nint lumV;
        public nint chrH;
        public nint chrV;
    }
}
