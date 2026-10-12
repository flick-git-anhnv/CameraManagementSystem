namespace Kztek.Tool.MultyPlatform.Paths;

/// <summary>
/// Nguồn sự thật duy nhất (single source of truth) cho mọi đường dẫn file/thư mục
/// mà ParkingV8 Avalonia cần resolve từ thư mục gốc ứng dụng (<see cref="AppContext.BaseDirectory"/>).
/// <br/>
/// Thay thế toàn bộ call <c>Path.Combine(AppContext.BaseDirectory, ...)</c> rải rác trong code.
/// <br/>
/// Nguyên tắc: KHÔNG đổi giá trị path vật lý — chỉ tập trung hóa cách resolve.
/// </summary>
public static class AppPaths
{
    // ── Root ─────────────────────────────────────────────────────────────────────
    private static string? _rootOverride;

    /// <summary>Thư mục gốc ứng dụng. Bình thường = <see cref="AppContext.BaseDirectory"/>.</summary>
    public static string Root => _rootOverride ?? AppContext.BaseDirectory;

    /// <summary>Test hook — chỉ dùng trong unit test, KHÔNG dùng runtime.</summary>
    public static void OverrideRootForTesting(string root) => _rootOverride = root;

    /// <summary>Reset về <see cref="AppContext.BaseDirectory"/> (dùng trong teardown unit test).</summary>
    public static void ResetRoot() => _rootOverride = null;

    // ── App-level directories ─────────────────────────────────────────────────────
    /// <summary><c>{Root}/Config</c> — thư mục cấu hình chính (PascalCase, schema mới).</summary>
    public static string ConfigDir        => Combine("Config");

    /// <summary><c>{Root}/configs</c> — thư mục cấu hình lane/camera/controller (lowercase, schema Avalonia).</summary>
    public static string ConfigsDir       => Combine("configs");

    /// <summary><c>{Root}/logs</c> — thư mục log.</summary>
    public static string LogsDir          => Combine("logs");

    /// <summary><c>{Root}/dictionary</c> — thư mục tệp ngôn ngữ JSON.</summary>
    public static string DictionaryDir    => Combine("dictionary");

    /// <summary><c>{Root}/runtime</c> — thư mục runtime (lock file, session temp).</summary>
    public static string RuntimeDir       => Combine("runtime");

    /// <summary><c>{Root}/PrintJobs</c> — thư mục lưu file HTML in phiếu thu.</summary>
    public static string PrintJobsDir     => Combine("PrintJobs");

    /// <summary><c>{Root}/Resources</c> — thư mục tài nguyên tĩnh (XML, SSP lookup).</summary>
    public static string ResourcesDir     => Combine("Resources");

    /// <summary><c>{Root}/Voice</c> — thư mục file âm thanh (case gốc).</summary>
    public static string VoiceDir         => Combine("Voice");

    /// <summary><c>{Root}/VOICE</c> — thư mục file âm thanh (uppercase fallback).</summary>
    public static string VoiceUpperDir    => Combine("VOICE");

    // ── App config files ──────────────────────────────────────────────────────────
    /// <summary><c>{Root}/appsettings.json</c></summary>
    public static string AppSettingsFile   => Combine("appsettings.json");

    /// <summary><c>{Root}/Config/login.json</c></summary>
    public static string LoginConfigFile   => Path.Combine(ConfigDir, "login.json");

    /// <summary><c>{Root}/Config/lane-layout.json</c></summary>
    public static string LaneLayoutFile    => Path.Combine(ConfigDir, "lane-layout.json");

    /// <summary><c>{Root}/Config/lane-settings/{laneId}.json</c></summary>
    public static string LaneSettingsFile(string laneId)
        => Path.Combine(ConfigDir, "lane-settings", $"{laneId}.json");

    /// <summary><c>{Root}/Config/print/{fileName}</c> (schema mới).</summary>
    public static string PrintTemplateFile(string fileName)
        => Path.Combine(ConfigDir, "print", fileName);

    /// <summary><c>{Root}/configs/app/print/{fileName}</c> (schema legacy WinForms).</summary>
    public static string LegacyPrintTemplateFile(string fileName)
        => Path.Combine(ConfigsDir, "app", "print", fileName);

    // ── Lane configs (configs/{laneId}/...) ──────────────────────────────────────
    /// <summary><c>{Root}/configs/{laneId}</c></summary>
    public static string LaneRoot(string laneId)
        => Path.Combine(ConfigsDir, laneId);

    /// <summary><c>{Root}/configs/{laneId}/lane/baseShortcut.txt</c></summary>
    public static string LaneShortcutBase(string laneId)
        => Path.Combine(LaneRoot(laneId), "lane", "baseShortcut.txt");

    /// <summary><c>{Root}/configs/{laneId}/lane/controllerShortcut.txt</c></summary>
    public static string LaneShortcutController(string laneId)
        => Path.Combine(LaneRoot(laneId), "lane", "controllerShortcut.txt");

    // LED
    /// <summary><c>{Root}/configs/{laneId}/led/{ledId}.txt</c></summary>
    public static string LaneLedFile(string laneId, string ledId)
        => Path.Combine(LaneRoot(laneId), "led", $"{ledId}.txt");

    /// <summary><c>{Root}/configs/{laneId}/led/{ledId}_default.txt</c></summary>
    public static string LaneLedDefaultFile(string laneId, string ledId)
        => Path.Combine(LaneRoot(laneId), "led", $"{ledId}_default.txt");

    /// <summary><c>{Root}/configs/{laneId}/led/{fileName}</c> — dùng khi fileName đã bao gồm suffix.</summary>
    public static string LaneLedFileByName(string laneId, string fileName)
        => Path.Combine(LaneRoot(laneId), "led", fileName);

    // Camera
    /// <summary><c>{Root}/configs/{laneId}/camera/{cameraId}.json</c></summary>
    public static string LaneCameraFile(string laneId, string cameraId)
        => Path.Combine(LaneRoot(laneId), "camera", $"{cameraId}.json");

    /// <summary><c>{Root}/configs/{laneId}/camera/{cameraId}_lpr.txt</c> (schema cũ).</summary>
    public static string LaneCameraLprFile(string laneId, string cameraId)
        => Path.Combine(LaneRoot(laneId), "camera", $"{cameraId}_lpr.txt");

    /// <summary><c>{Root}/configs/{laneId}/camera/{cameraId}_loop.txt</c> (schema cũ).</summary>
    public static string LaneCameraLoopFile(string laneId, string cameraId)
        => Path.Combine(LaneRoot(laneId), "camera", $"{cameraId}_loop.txt");

    // Controller — Avalonia schema (mới)
    /// <summary><c>{Root}/configs/{laneId}/controller/{controllerId}</c></summary>
    public static string LaneControllerDir(string laneId, string controllerId)
        => Path.Combine(LaneRoot(laneId), "controller", controllerId);

    /// <summary><c>{Root}/configs/{laneId}/controller/{controllerId}/reader-{readerIndex}.json</c></summary>
    public static string LaneControllerReaderFile(string laneId, string controllerId, int readerIndex)
        => Path.Combine(LaneControllerDir(laneId, controllerId), $"reader-{readerIndex}.json");

    /// <summary><c>{Root}/configs/{laneId}/controller/{controllerId}/barrie-{barrierIndex}.json</c></summary>
    public static string LaneControllerBarrierFile(string laneId, string controllerId, int barrierIndex)
        => Path.Combine(LaneControllerDir(laneId, controllerId), $"barrie-{barrierIndex}.json");

    // Controller — WinForms legacy schema (giữ song song để backward-compat)
    /// <summary><c>{Root}/configs/{laneId}/{controllerId}_{readerIndex}/reader_config.txt</c></summary>
    public static string LegacyLaneControllerReaderFile(string laneId, string controllerId, int readerIndex)
        => Path.Combine(LaneRoot(laneId), $"{controllerId}_{readerIndex}", "reader_config.txt");

    /// <summary><c>{Root}/configs/{laneId}/{controllerId}_barrie{barrierIndex}/barrie_config.txt</c></summary>
    public static string LegacyLaneControllerBarrierFile(string laneId, string controllerId, int barrierIndex)
        => Path.Combine(LaneRoot(laneId), $"{controllerId}_barrie{barrierIndex}", "barrie_config.txt");

    // ── Sound (Voice / VOICE probe) ───────────────────────────────────────────────
    /// <summary>
    /// Trả về danh sách đường dẫn probe âm thanh theo thứ tự ưu tiên cố định:
    /// <list type="number">
    ///   <item><c>{Root}/Voice/{languageCode}/{fileName}</c></item>
    ///   <item><c>{Root}/VOICE/{languageCode}/{fileName}</c></item>
    ///   <item><c>{Root}/Voice/{fileName}</c></item>
    ///   <item><c>{Root}/VOICE/{fileName}</c></item>
    /// </list>
    /// Thứ tự PHẢI giữ nguyên để backward-compat với logic probe trong <c>LaneSoundService</c>.
    /// </summary>
    public static IReadOnlyList<string> SoundProbePaths(string languageCode, string fileName)
        => new[]
        {
            Path.Combine(VoiceDir,      languageCode, fileName),
            Path.Combine(VoiceUpperDir, languageCode, fileName),
            Path.Combine(VoiceDir,      fileName),
            Path.Combine(VoiceUpperDir, fileName),
        };

    // ── Native / Resources ────────────────────────────────────────────────────────
    /// <summary><c>{Root}/{fileName}</c> — native DLL nằm cạnh app executable (VD: Aopu_Controller.dll).</summary>
    public static string NativeLibrary(string fileName)
        => Combine(fileName);

    /// <summary><c>{Root}/Resources/{fileName}</c></summary>
    public static string ResourceFile(string fileName)
        => Path.Combine(ResourcesDir, fileName);

    // ── Internal helper ───────────────────────────────────────────────────────────
    private static string Combine(params string[] parts)
        => Path.Combine(new[] { Root }.Concat(parts).ToArray());
}
