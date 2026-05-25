using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;

namespace TradingViewChart.Demo;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        CandleChart.FrameRendered += (_, args) => viewModel.OnFrameRendered(args);
        PriceChart.FrameRendered += (_, args) => viewModel.OnFrameRendered(args);
    }

    private void OnToggleThemeClick(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant =
            RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;

        Dispatcher.UIThread.Post(
            () =>
            {
                CandleChart.InvalidateVisual();
                PriceChart.InvalidateVisual();
            },
            DispatcherPriority.Render
        );
    }
}
