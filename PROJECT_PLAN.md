# KẾ HOẠCH DỰ ÁN HỆ THỐNG QUẢN LÝ CAMERA (VMS / NVR SOFTWARE)
**Multi-brand Camera Management System with Integrated Edge AI**

---

## 1. TỔNG QUAN DỰ ÁN

* **Tên dự án**: Camera Management System (CMS / VMS)
* **Mục tiêu**: Xây dựng hệ thống quản lý Camera tập trung (VMS) và đầu ghi phần mềm (NVR Software) đa nền tảng, có đầy đủ các tính năng giám sát, ghi hình, xem lại như đầu ghi chuyên dụng của Hikvision/Dahua, hỗ trợ kết nối đa dạng hãng camera theo kiến trúc plugin mở, tích hợp sâu các mô hình AI độc quyền do công ty phát triển (Phương tiện & Con người).
* **Nền tảng công nghệ chính**:
  * **Core Backend / App**: .NET 8, C# 12
  * **UI Framework**: Avalonia UI 12.x (Fluent Theme, SkiaSharp / DirectX / OpenGL rendering)
  * **Video Engine**: FFmpeg P/Invoke (Direct stream copy, H.264/H.265/AAC demux/mux, HW decode GPU)
  * **AI Inference**: OpenVINO Runtime (`Sdcb.OpenVINO`), ONNX Runtime, OpenCV (`OpenCvSharp4`)
  * **Lưu trữ & Metadata**: SQLite / LiteDB / RocksDB + Vector search nhúng
  * **Kế thừa Baselib công ty**:
    * `Kztek.Cameras.Avalonia`
    * `Kztek.Api.MultyPlatform`
    * `Kztek.Object.MultyPlatform`
    * `Kztek.Tool.MultyPlatform`

---

## 2. KIẾN TRÚC TỔNG THỂ HỆ THỐNG (SYSTEM ARCHITECTURE)

Hệ thống được thiết kế theo mô hình **Modular Monolith & Layered Architecture**, tách biệt rõ rệt giữa:
1. **Core Processing Engine (Headless Daemon)**: Quản lý kết nối camera, Demux/Mux ghi hình zero-transcode, điều phối luồng AI.
2. **Client Interface**: Avalonia Desktop (Phase 1), WebRTC / Web Portal & Mobile App (Phase 2).

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            HỆ THỐNG CAMERA                                  │
│   [Hikvision]    [Dahua]    [Hanwha]    [Uniview]    [ONVIF]    [Generic RTSP]
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ (RTSP / ONVIF / SDK / HTTP)
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. DRIVER & PROTOCOL ADAPTER LAYER (Plugin Architecture)                     │
│  - Dynamic Plugin Loader (Hot-plug DLLs)                                    │
│  - ONVIF Discovery (WS-Discovery Probe) & ONVIF Profile S/G/T Standardizer  │
│  - RTSP URL Builders (Kế thừa CameraRtspUrlBuilderFactory)                  │
│  - PTZ Controller (ONVIF PTZ, CGI HTTP commands)                            │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ (Raw Packets / Audio-Video Samples)
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. CORE MEDIA & STREAM DISPATCHER ENGINE                                    │
│  - Stream Demuxer (H.264 / H.265 / AAC / PCMA)                              │
│  - Dual-Stream Router (Main-Stream vs Sub-Stream)                           │
│  - Zero-Copy Ring Buffer & Frame Dispatcher                                 │
└──────────────┬───────────────────────┬───────────────────────┬──────────────┘
               │                       │                       │
               ▼                       ▼                       ▼
┌────────────────────────┐  ┌────────────────────────┐  ┌─────────────────────┐
│ 3. STORAGE ENGINE      │  │ 4. AI INFERENCE ENGINE │  │ 5. DISPLAY ENGINE   │
│ - Zero-Transcode       │  │ - OpenVINO Runtime     │  │ - GPU Hardware      │
│   Remuxing (Direct MP4)│  │ - Frame Sampler (5-10) │  │   Decode (NVDEC/QSV)│
│ - 24/7 Continuous      │  │ - Vehicle Pipeline:    │  │ - WriteableBitmap   │
│ - Schedule Recording   │  │   Plate, Class, Color  │  │   Frame Pooling     │
│ - Event / Motion Rec   │  │ - Person Pipeline:     │  │ - Dual-Stream Auto  │
│ - Disk Quota & FIFO    │  │   Face, ReID, Track    │  │   Switch (Grid/Full)│
└──────────────┬─────────┘  └──────────┬─────────────┘  └──────────┬──────────┘
               │                       │                           │
               ▼                       ▼                           ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 6. DATA & METADATA ACCESS LAYER                                             │
│  - Video Segments Catalog (Indexed MP4 fragments)                           │
│  - Event & Alarm Store (Motion, Tripwire, Blacklist Alert)                  │
│  - Vector Embedding Index (512-dim Face / ReID feature vectors)             │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 7. CLIENT PRESENTATION LAYER                                                │
│  [Phase 1] Avalonia Desktop App (.NET 8 Windows/Linux)                      │
│     - Grid Liveview (1, 4, 9, 16, 25, 36, 64)                               │
│     - Multi-channel Sync Timeline Playback                                  │
│     - E-Map, PTZ Virtual Joystick, Smart Search Dialog                      │
│  [Phase 2] Media Gateway & External Clients                                 │
│     - WebRTC / HLS Streaming Gateway                                        │
│     - Web Browser Portal (HTML5/React/Blazor)                               │
│     - Mobile App (iOS / Android)                                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. PHÂN KỲ & LỘ TRÌNH TRIỂN KHAI CHI TIẾT

### **PHASE 1: ỨNG DỤNG MULTIPLATFORM TRÊN AVALONIA (.NET 8)**

#### **Phase 1.1: Xây dựng App Đầy Đủ Chức Năng Như Đầu Ghi Chuyên Nghiệp**
* **Milestone 1: Khởi tạo Kiến trúc & Quản trị Thiết bị Đa hãng**
  * Thiết kế Solution Clean Architecture (`CMS.Core`, `CMS.Infrastructure`, `CMS.Drivers`, `CMS.MediaServer`, `CMS.AI`, `CMS.App`).
  * Xây dựng `ICameraDriver` interface và cơ chế nạp plugin động.
  * Xây dựng module **ONVIF Discovery** (WS-Discovery) tự động dò tìm camera IP trong mạng LAN.
  * Tích hợp và mở rộng `CameraRtspUrlBuilderFactory` từ Baselib cho hơn 18 hãng camera.
  * Giao diện quản lý danh sách thiết bị: thêm thủ công (IP/Port), quét tự động, nhóm camera, kiểm tra trạng thái Online/Offline theo thời gian thực (Heartbeat check).
* **Milestone 2: High-Performance Live View & Dual-Stream Switching**
  * Lưới hiển thị đa kênh tùy biến: 1x1, 2x2, 3x3, 4x4, 1+5, 1+7, lên đến 36-64 kênh.
  * Kéo thả (Drag & Drop) camera từ danh sách thiết bị vào từng ô lưới.
  * Cơ chế **Dual-Stream Switching**: Xem lưới ở chế độ Sub-stream (nhẹ, tiết kiệm CPU); Click đúp phóng to 1 camera tự chuyển sang Main-stream (Full HD/4K).
  * Render tối ưu không rò rỉ RAM với `WriteableBitmap` và `AvaloniaFramePool`.
* **Milestone 3: Storage Engine & Ghi Hình "Zero-Transcode" (Direct Stream Copy)**
  * Triển khai bộ Remuxer trực tiếp từ packet RTSP sang định dạng file MP4 (15 phút / 30 phút / 60 phút mỗi đoạn) mà không cần decode hình ảnh, giảm 95% tải CPU.
  * Chế độ ghi hình linh động: Ghi liên tục 24/7, Ghi theo lịch biểu (Schedule - giờ hành chính, ban đêm, ngày lễ), Ghi theo sự kiện (Motion / AI trigger).
  * Cơ chế Storage Manager: Quản lý danh sách ổ đĩa cứng, giám sát dung lượng khả dụng, tự động ghi đè video cũ nhất khi đạt ngưỡng giới hạn (FIFO retention).
* **Milestone 4: Smart Playback Engine & Đồng Bộ Timeline Đa Kênh**
  * Thanh Timeline 24 giờ trực quan với các màu sắc biểu thị trạng thái (Xanh: liên tục, Vàng: chuyển động, Đỏ: sự kiện AI).
  * Đồng bộ phát lại (Synchronous Playback): Xem lại đồng thời 1, 4, 9 camera tại cùng một mốc thời gian.
  * Điều khiển playback đầy đủ: Play, Pause, Tua nhanh (x2, x4, x8, x16), Tua lùi (Reverse play), Nhảy từng khung hình (Frame stepping).
  * Chức năng trích xuất và xuất video clip (Export Video Clip) ra file MP4 độc lập có watermark thời gian.
* **Milestone 5: Điều Khiển PTZ, E-Map, Quản Lý Cảnh Báo & Phân Quyền (RBAC)**
  * Bảng điều khiển PTZ trên màn hình: 8 hướng, Zoom In/Out, Focus, Iris, Preset point (gọi điểm và lưu điểm), Patrol tour (tuần tra tự động).
  * Bản đồ số (E-Map): Nhập sơ đồ mặt bằng (PNG/SVG), định vị camera trên bản đồ, nhấp nháy cảnh báo trực quan khi có sự kiện.
  * Hệ thống cảnh báo (Alarm Linkage): Pop-up camera khi có chuyển động, còi báo âm thanh, kích hoạt đầu ra I/O Relay.
  * Phân quyền người dùng (RBAC): Quản trị viên (Admin), Người vận hành (Operator), Người xem (Viewer) với phân quyền chi tiết tới từng camera.

---

#### **Phase 1.2: Tích Hợp Các Engine AI Do Công Ty Phát Triển**
* **Milestone 6: Hạ Tầng AI Inference Pipeline & Metadata Store**
  * Xây dựng `AIEngineWorker` bất đồng bộ: Tách biệt hoàn toàn luồng hiển thị video và luồng tính toán AI để đảm bảo UI 60fps không bị giật lag.
  * Bộ lấy mẫu thông minh (Adaptive Frame Sampler): Chạy AI ở tần suất 5-10 fps hoặc kích hoạt khi có Motion Detection.
  * Tích hợp OpenVINO Runtime đa nền tảng (`Sdcb.OpenVINO`) tận dụng CPU Intel / Intel Iris Xe / Nvidia GPU.
  * Cơ sở dữ liệu Metadata cục bộ để lưu trữ thuộc tính xe, biển số, khuôn mặt và vector embedding.
* **Milestone 7: Engine AI Phương Tiện (Vehicles)**
  * **Phân loại xe (Vehicle Classification)**: Xe con (Sedan/SUV), Xe bán tải, Xe buýt, Xe tải, Container, Xe máy, Xe đạp.
  * **Nhận dạng biển số (ANPR/LPR)**: Nhận dạng biển số đa nền tảng (kế thừa `ReadPL` trên Windows và mô hình OpenVINO trên Linux), chuẩn hóa ký tự biển số.
  * **Nhận dạng hãng & màu xe**: Nhận dạng các hãng xe phổ biến (Toyota, Hyundai, Kia, Mazda, Honda, Ford, VinFast, Mercedes, BMW...) và màu sắc cơ bản.
  * **Vehicle Tracking (1 Camera)**: Thuật toán ByteTrack theo dõi quỹ đạo xe, đếm xe, phát hiện xe dừng đỗ trái phép, xe đi ngược chiều.
* **Milestone 8: Engine AI Con Người (Persons)**
  * **Nhận diện khuôn mặt (Face Recognition 1:N)**: Pipeline Face Detect $\to$ Align $\to$ Feature Extraction (512-dim vector) $\to$ Tìm kiếm nhanh trong danh sách (Nhân viên, Khách VIP, Đối tượng trong Blacklist).
  * **Cảnh báo Blacklist**: Bật pop-up cảnh báo âm thanh và hình ảnh ngay lập tức khi phát hiện đối tượng cần theo dõi.
  * **Single-Camera Person Tracking**: Theo dõi người, phát hiện lảng vãng (Loitering), vượt hàng rào ảo (Tripwire), xâm nhập vùng cấm (Intrusion Detection).
* **Milestone 9: Multi-Camera Person Tracking (Person Re-ID Đa Camera)**
  * Trích xuất vector đặc trưng ngoại hình toàn thân (Appearance Embedding).
  * Giải thuật liên kết không-thời gian (Spatio-Temporal Graph Matching): Đối tượng rời Camera 1 lúc $T_1$, xuất hiện ở Camera 2 lúc $T_2$.
  * **Vẽ lại hành trình đối tượng**: Người dùng chọn 1 bức ảnh/khuôn mặt, hệ thống tự động quét và vẽ lại hành trình di chuyển qua sơ đồ các camera kèm đoạn video minh chứng.
* **Milestone 10: Tối Ưu Hóa Hiệu Năng & Đóng Gói Cài Đặt**
  * Stress-test liên tục 72h với tải 16 - 32 kênh camera kèm AI, đảm bảo RAM và CPU ổn định tuyệt đối.
  * Đóng gói bộ cài đặt: Windows Installer (.msi / InnoSetup) và Linux Package (.deb / AppImage).

---

### **PHASE 2: MỞ RỘNG NỀN TẢNG WEB & MOBILE**
* **Media Streaming Gateway (WebRTC / HLS / MSE)**:
  * Xây dựng dịch vụ Streaming Gateway chuyển tiếp luồng RTSP của các camera sang giao thức WebRTC (độ trễ dưới 500ms) và HLS để trình duyệt và điện thoại xem trực tiếp mà không cần cài plugin.
* **Web Management Portal**:
  * Giao diện Web SPA (React/Vue hoặc Blazor) cho phép quản trị viên đăng nhập từ bất kỳ máy tính nào qua trình duyệt Chrome/Edge để xem Liveview, Playback, tìm kiếm sự kiện AI và cấu hình camera.
* **Mobile Application (iOS / Android)**:
  * Ứng dụng di động (Flutter / React Native) hỗ trợ:
    * Xem Liveview camera mọi lúc mọi nơi.
    * Nhận thông báo đẩy (Push Notification Firebase / APNs) tức thì khi có sự kiện AI (xe blacklist, người lạ xâm nhập).
    * Xem lại đoạn video clip ngắn gắn kèm sự kiện ngay trên điện thoại.

---

## 4. TỔ CHỨC CẤU TRÚC MÃ NGUỒN (SOLUTION STRUCTURE)

```
d:\CameraManagementSystem\
├── .gitignore
├── CameraManagementSystem.sln
├── PROJECT_PLAN.md
├── src\
│   ├── CMS.Core\                       (Domain models, Entities, Interfaces)
│   │   ├── Entities\                   (Camera, StorageSegment, AiEvent, User)
│   │   ├── Enums\                      (CameraType, StreamType, RecordingMode)
│   │   └── Interfaces\                 (ICameraDriver, IStorageEngine, IAIEngine)
│   │
│   ├── CMS.Infrastructure\             (Database, Configuration, Logging)
│   │   ├── Data\                       (SQLite / EF Core Context & Repositories)
│   │   └── Services\                   (ConfigService, EventBus, LogService)
│   │
│   ├── CMS.Drivers\                    (Multi-brand Camera Adapters)
│   │   ├── Abstractions\               (BaseCameraDriver, DriverFactory)
│   │   ├── Onvif\                      (WS-Discovery, OnvifClient, OnvifPtz)
│   │   ├── Hikvision\                  (HikvisionRtsp, HikCgiDriver)
│   │   ├── Dahua\                      (DahuaRtsp, DahuaCgiDriver)
│   │   └── GenericRtsp\                (GenericRtspDriver, UrlTemplateBuilder)
│   │
│   ├── CMS.MediaServer\                (Streaming & Storage Engine)
│   │   ├── FFmpeg\                     (FFmpegInterop, HWDecoder, FramePool)
│   │   ├── Streaming\                  (RtspClientSession, DualStreamManager)
│   │   └── Storage\                    (ZeroTranscodeMuxer, DiskManager, RetentionPolicy)
│   │
│   ├── CMS.AI\                         (Edge AI Engines & Pipeline)
│   │   ├── Pipeline\                   (AIPipelineWorker, FrameSampler)
│   │   ├── Tracking\                   (ByteTracker, KalmanFilter)
│   │   ├── Vehicles\                   (VehicleClassifier, PlateReader, ColorDetector)
│   │   ├── Persons\                    (FaceDetector, FaceMatcher, PersonReId)
│   │   └── Storage\                    (MetadataStore, VectorSearchEngine)
│   │
│   └── CMS.App\                        (Avalonia Desktop UI)
│       ├── Views\                      (LiveViewGrid, PlaybackView, DeviceConfig, AiDashboard)
│       ├── ViewModels\                 (MainViewModel, LiveViewModel, PlaybackViewModel)
│       ├── Controls\                   (VideoGridCell, TimelineControl, PtzJoystick)
│       └── Assets\                     (Icons, Themes, Styles)
│
└── tests\                              (Unit & Integration Tests)
```
