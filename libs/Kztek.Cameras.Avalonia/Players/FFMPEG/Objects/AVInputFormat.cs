using System.Runtime.InteropServices;

namespace ANV.Cameras.Objects
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct AVInputFormat
    {
        /// <summary>
        /// string
        /// </summary>
        public nint name;
        /// <summary>
        /// string
        /// </summary>
        public nint long_name;
        public int flags;
        /// <summary>
        /// string
        /// </summary>
        public nint extensions;
        /// <summary>
        /// input/output formats
        /// </summary>
        public nint codec_tag;

        /// <summary>
        /// <see cref="AVClass"/>
        /// </summary>
        public nint priv_class;
        public nint mime_type;
    }
}
