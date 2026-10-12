// G4.3: Copy nguyên xi từ Players/FFMPEG/UserControls/FFmpegDecoderGPU.cs (WinForms).
// File này HOÀN TOÀN bị comment out trong source WinForms — giữ nguyên như vậy.
// Không có code hoạt động, không cần thay đổi gì.

//// FFmpegDecoder.cs - Sử dụng GPU (CUDA/NVDEC) để decode H.265 từ luồng RTSP
//using FFmpeg.AutoGen;
//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Windows.Forms;

//namespace FFmpegDecoder_H265
//{
//    public unsafe class FFmpegGpuDecoder
//    {
//        private Thread decodeThread;
//        private bool isRunning;
//        private PictureBox displayBox;

//        public FFmpegGpuDecoder()
//        {
//            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
//            ffmpeg.avdevice_register_all();
//            ffmpeg.avformat_network_init();
//        }

//        public void StartDecoding(string url, PictureBox pictureBox)
//        {
//            if (isRunning) return;

//            isRunning = true;
//            displayBox = pictureBox;
//            decodeThread = new Thread(() => DecodeLoop(url));
//            decodeThread.IsBackground = true;
//            decodeThread.Start();
//        }

//        public void StopDecoding()
//        {
//            isRunning = false;
//        }
//    }
//}
