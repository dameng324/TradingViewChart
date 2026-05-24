using Avalonia;
using Avalonia.Headless;
using Avalonia.Skia;

namespace TradingViewChart.Tests;

public sealed class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApplication>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true
            });
    }
}
