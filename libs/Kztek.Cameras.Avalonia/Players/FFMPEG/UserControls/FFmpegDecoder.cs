// G4.3: Copy nguyên xi từ Players/FFMPEG/UserControls/FFmpegDecoder.cs (WinForms).
// File này HOÀN TOÀN bị comment out trong source WinForms — giữ nguyên như vậy.
// Không có code hoạt động, không cần thay đổi gì.

//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Windows.Forms;
//using FFmpeg.AutoGen;

//namespace FFmpegDecoder_H265
//{
//    public unsafe class FFmpegDecoder
//    {
//        private Thread decodeThread;
//        private bool isRunning;
//        private PictureBox displayBox;

//        public FFmpegDecoder()
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

//        private unsafe void DecodeLoop(string url)
//        {
//            AVFormatContext* formatContext = null;
//            ffmpeg.avformat_open_input(&formatContext, url, null, null);
//            ffmpeg.avformat_find_stream_info(formatContext, null);

//            int videoStreamIndex = -1;
//            for (int i = 0; i < formatContext->nb_streams; i++)
//            {
//                if (formatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
//                {
//                    videoStreamIndex = i;
//                    break;
//                }
//            }

//            if (videoStreamIndex == -1) return;

//            AVCodecContext* codecContext = formatContext->streams[videoStreamIndex]->codec;
//            AVCodec* codec = ffmpeg.avcodec_find_decoder(codecContext->codec_id);
//            ffmpeg.avcodec_open2(codecContext, codec, null);

//            AVPacket packet;
//            AVFrame* frame = ffmpeg.av_frame_alloc();

//            SwsContext* swsContext = ffmpeg.sws_getContext(
//                codecContext->width, codecContext->height, codecContext->pix_fmt,
//                codecContext->width, codecContext->height, AVPixelFormat.AV_PIX_FMT_BGR24,
//                ffmpeg.SWS_BILINEAR, null, null, null);

//            while (isRunning && ffmpeg.av_read_frame(formatContext, &packet) >= 0)
//            {
//                if (packet.stream_index == videoStreamIndex)
//                {
//                    int frameFinished;
//                    ffmpeg.avcodec_decode_video2(codecContext, frame, &frameFinished, &packet);

//                    if (frameFinished != 0)
//                    {
//                        Bitmap bitmap = new Bitmap(codecContext->width, codecContext->height, PixelFormat.Format24bppRgb);
//                        BitmapData bitmapData = bitmap.LockBits(
//                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
//                            ImageLockMode.WriteOnly, bitmap.PixelFormat);

//                        byte_ptrArray4 srcData = frame->data;
//                        int_array4 srcLinesize = frame->linesize;

//                        ffmpeg.sws_scale(swsContext,
//                            srcData, srcLinesize, 0, codecContext->height,
//                            new byte_ptrArray4 { [0] = (byte*)bitmapData.Scan0 },
//                            new int_array4 { [0] = bitmapData.Stride });

//                        bitmap.UnlockBits(bitmapData);

//                        if (displayBox.InvokeRequired)
//                        {
//                            displayBox.Invoke(new Action(() =>
//                            {
//                                displayBox.Image?.Dispose();
//                                displayBox.Image = bitmap;
//                            }));
//                        }
//                        else
//                        {
//                            displayBox.Image?.Dispose();
//                            displayBox.Image = bitmap;
//                        }
//                    }
//                }

//                ffmpeg.av_free_packet(&packet);
//            }

//            ffmpeg.av_frame_free(&frame);
//            ffmpeg.avcodec_close(codecContext);
//            ffmpeg.avformat_close_input(&formatContext);
//            ffmpeg.sws_freeContext(swsContext);
//        }
//    }
//}
