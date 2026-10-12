using System;
using System.ComponentModel;

namespace Kztek.Cameras
{
    public static class ExtensionMethod
    {
        public static TResult SafeInvoke<T, TResult>(this T isi, Func<T, TResult> call) where T : ISynchronizeInvoke
        {
            if (isi.InvokeRequired)
            {
                IAsyncResult result = isi.BeginInvoke(call, new object[]
                {
                    isi
                });
                object endResult = isi.EndInvoke(result);
                return (TResult)((object)endResult);
            }
            return call(isi);
        }
        public static void SafeInvoke<T>(this T isi, Action<T> call) where T : ISynchronizeInvoke
        {
            if (isi.InvokeRequired)
            {
                isi.BeginInvoke(call, new object[]
                {
                    isi
                });
                return;
            }
            call(isi);
        }
    }
}
