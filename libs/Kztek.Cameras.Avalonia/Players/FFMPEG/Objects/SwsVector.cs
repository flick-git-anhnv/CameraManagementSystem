using System.Runtime.InteropServices;

namespace ANV.Cameras.Objects
{
    // Structs
    /// <summary>
    /// Filter vector containing an array of coefficients.
    /// Used for horizontal/vertical filtering.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SwsVector
    {
        public nint coeff; // double* - pointer to coefficients
        public int length;   // number of coefficients
    }
}
