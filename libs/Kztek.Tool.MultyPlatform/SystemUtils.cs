using Kztek.Tool.LogHelpers;

namespace Kztek.Tool;

/// <summary>
/// Minimal runtime surface used by the Avalonia app.
/// The legacy WinForms project exposed many Windows-specific helpers here,
/// but for the current migration we only keep the shared logger entry point.
/// </summary>
public static class SystemUtils
{
    public static ILogger? logger;
}
