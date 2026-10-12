// Fix (2026-07-20, loại bỏ SkiaSharp toàn thư viện):
//   - Predict(SKBitmap image, ...) → Predict(WriteableBitmap image, ...)
//   - Letterbox+Preprocess gộp thành 1 hàm: bilinear-sample trực tiếp từ buffer WriteableBitmap.Lock()
//     (con trỏ thô, Bgra8888) vào tensor float[] đích — không còn cấp phát SKBitmap trung gian cho
//     letterbox, không còn SKCanvas.DrawImage (thay bằng bilinear tay, giữ đúng hành vi resize cũ).
// Xem ADR §6.3 — trước đây SKBitmap là decode target nội bộ; giờ thống nhất WriteableBitmap toàn pipeline.
using Avalonia.Media.Imaging;
using Sdcb.OpenVINO;

namespace Kztek.Cameras.AiDetection
{
    public class PredictionResult
    {
        public EmClassification ClassName { get; set; }
        public float Confidence { get; set; }
        public int ClassId { get; set; }
    }

    public enum EmClassification
    {
        Close,
        Open,
        Unknown
    }

    /// <summary>
    /// Phân loại Open/Close cho 1 vùng ảnh (box) bằng model OpenVINO (YOLO-classification, 2 lớp).
    /// Preprocessing được cấu hình đúng theo rt_info của model đã export (xem best.xml):
    /// input 1x3x128x128, letterbox pad=114, reverse_input_channels=YES (BGR→RGB), scale=255,
    /// output đã có sẵn Softmax trong graph, labels="Close Open" (index0=Close, index1=Open).
    /// </summary>
    public class MolexDetection : IDisposable
    {
        private const int InputSize = 128;
        private const byte PadValue = 114;
        private static readonly EmClassification[] LabelOrder = { EmClassification.Close, EmClassification.Open };

        private readonly OVCore _core;
        private readonly Model _model;
        private readonly CompiledModel _compiledModel;
        private readonly InferRequest _inferRequest;
        private readonly IOPort _inputPort;
        private readonly IOPort _outputPort;
        private bool _disposed;

        /// <param name="modelXmlPath">
        /// Đường dẫn tới file .xml (IR OpenVINO). File .bin cùng tên PHẢI nằm cùng thư mục —
        /// nếu không có, OpenVINO sẽ nạp model KHÔNG có trọng số (kết quả inference vô nghĩa).
        /// Mặc định trỏ tới Models/MolexDetection/best.xml cạnh assembly.
        /// </param>
        /// <param name="device">Thiết bị inference: "CPU" (mặc định, ổn định nhất) hoặc "GPU".</param>
        public MolexDetection(string? modelXmlPath = null, string device = "CPU")
        {
            modelXmlPath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "MolexDetection", "best.xml");

            if (!File.Exists(modelXmlPath))
            {
                throw new FileNotFoundException($"Không tìm thấy model MolexDetection tại '{modelXmlPath}'.", modelXmlPath);
            }

            string binPath = Path.ChangeExtension(modelXmlPath, ".bin");
            if (!File.Exists(binPath))
            {
                throw new FileNotFoundException(
                    $"Không tìm thấy file trọng số '{binPath}' — model sẽ không hoạt động đúng nếu thiếu file này.", binPath);
            }

            _core = OVCore.Shared;
            _model = _core.ReadModel(modelXmlPath, binPath);
            _compiledModel = _core.CompileModel(_model, new DeviceOptions(device));
            _inferRequest = _compiledModel.CreateInferRequest();
            _inputPort = _compiledModel.Inputs.Primary;
            _outputPort = _compiledModel.Outputs.Primary;
        }

        /// <summary>
        /// Chạy inference trên 1 ảnh crop (vùng box). Trả về null nếu ảnh rỗng.
        /// </summary>
        /// <param name="image">Ảnh vùng box cần phân loại Open/Close.</param>
        /// <param name="conf">
        /// Ngưỡng confidence (0-100). Nếu confidence dự đoán thấp hơn ngưỡng này, trả về
        /// <see cref="EmClassification.Unknown"/> thay vì Open/Close.
        /// </param>
        public PredictionResult Predict(WriteableBitmap image, float conf)
        {
            if (image == null || image.PixelSize.Width <= 0 || image.PixelSize.Height <= 0)
            {
                return null;
            }

            float[] inputData = Preprocess(image);

            Tensor inputTensor = _inferRequest.GetTensorByPort(_inputPort);
            inputData.CopyTo(inputTensor.GetData<float>());

            _inferRequest.Run();

            Tensor outputTensor = _inferRequest.GetTensorByPort(_outputPort);
            var scores = outputTensor.GetData<float>();

            int bestIdx = scores[0] >= scores[1] ? 0 : 1;
            float bestConfidence = scores[bestIdx] * 100f;

            if (bestConfidence < conf)
            {
                return new PredictionResult
                {
                    ClassId = 2,
                    ClassName = EmClassification.Unknown,
                    Confidence = bestConfidence
                };
            }

            return new PredictionResult
            {
                ClassId = bestIdx,
                ClassName = LabelOrder[bestIdx],
                Confidence = bestConfidence
            };
        }

        /// <summary>
        /// Letterbox (giữ tỉ lệ khung hình, pad màu xám 114) + chuyển BGR→RGB + chuẩn hoá [0,1] +
        /// layout NCHW — gộp thành 1 pass bilinear-sample trực tiếp từ buffer nguồn, không cấp phát
        /// bitmap trung gian. Hành vi pixel-level (bilinear resize, byte order, normalize /255f) giữ
        /// nguyên so với bản SKCanvas.DrawImage(FilterMode.Linear) trước đây.
        /// </summary>
        private static unsafe float[] Preprocess(WriteableBitmap src)
        {
            int srcW = src.PixelSize.Width;
            int srcH = src.PixelSize.Height;

            float scale = Math.Min((float)InputSize / srcW, (float)InputSize / srcH);
            int newW = Math.Max(1, (int)Math.Round(srcW * scale));
            int newH = Math.Max(1, (int)Math.Round(srcH * scale));
            int padX = (InputSize - newW) / 2;
            int padY = (InputSize - newH) / 2;

            var data = new float[3 * InputSize * InputSize];
            int planeSize = InputSize * InputSize;
            float padNorm = PadValue / 255f;
            for (int i = 0; i < planeSize; i++)
            {
                data[i] = padNorm;
                data[planeSize + i] = padNorm;
                data[2 * planeSize + i] = padNorm;
            }

            using var fb = src.Lock();
            byte* basePtr = (byte*)fb.Address.ToPointer();
            int stride = fb.RowBytes;
            const int bytesPerPixel = 4; // Bgra8888

            for (int y = 0; y < newH; y++)
            {
                float srcYf = Math.Min(srcH - 1, y / scale);
                int y0 = (int)srcYf;
                int y1 = Math.Min(srcH - 1, y0 + 1);
                float fy = srcYf - y0;

                for (int x = 0; x < newW; x++)
                {
                    float srcXf = Math.Min(srcW - 1, x / scale);
                    int x0 = (int)srcXf;
                    int x1 = Math.Min(srcW - 1, x0 + 1);
                    float fx = srcXf - x0;

                    byte* p00 = basePtr + y0 * stride + x0 * bytesPerPixel;
                    byte* p10 = basePtr + y0 * stride + x1 * bytesPerPixel;
                    byte* p01 = basePtr + y1 * stride + x0 * bytesPerPixel;
                    byte* p11 = basePtr + y1 * stride + x1 * bytesPerPixel;

                    // reverse_input_channels=YES → model được huấn luyện với input RGB.
                    float r = Bilerp(p00[2], p10[2], p01[2], p11[2], fx, fy);
                    float g = Bilerp(p00[1], p10[1], p01[1], p11[1], fx, fy);
                    float b = Bilerp(p00[0], p10[0], p01[0], p11[0], fx, fy);

                    int dstIdx = (y + padY) * InputSize + (x + padX);
                    data[dstIdx] = r / 255f;
                    data[planeSize + dstIdx] = g / 255f;
                    data[2 * planeSize + dstIdx] = b / 255f;
                }
            }

            return data;
        }

        private static float Bilerp(byte v00, byte v10, byte v01, byte v11, float fx, float fy)
        {
            float top = v00 + (v10 - v00) * fx;
            float bottom = v01 + (v11 - v01) * fx;
            return top + (bottom - top) * fy;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _inferRequest?.Dispose();
            _compiledModel?.Dispose();
            _model?.Dispose();
            // _core là instance dùng chung (OVCore.Shared) — KHÔNG dispose ở đây.
        }
    }
}
