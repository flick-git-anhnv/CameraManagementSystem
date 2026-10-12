using Avalonia;
using Avalonia.Headless;
using CMS.UI.HeadlessTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace CMS.UI.HeadlessTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<CMS.App.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true
            });
}
