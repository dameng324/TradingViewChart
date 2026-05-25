using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using TradingViewChart.Indicators;

namespace TradingViewChart.Demo;

internal sealed class DemoIndicatorEditor : ITradingChartIndicatorEditor
{
    public Task<bool> EditAsync(
        global::TradingViewChart.TradingViewChart chart,
        TradingIndicatorEditorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return DemoIndicatorEditorDialog.ShowAsync(chart, request);
    }
}

internal sealed class DemoIndicatorEditorDialog : Window
{
    private readonly TradingIndicatorItem _item;
    private readonly Dictionary<TradingIndicatorParameterValue, Control> _editors = new();
    private readonly TextBlock _errorTextBlock;
    private readonly CheckBox _hiddenCheckBox;

    private DemoIndicatorEditorDialog(TradingIndicatorEditorRequest request)
    {
        _item = request.Item;
        Title = request.Title;
        Width = 360;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(16) };

        root.Children.Add(
            new TextBlock
            {
                Text = request.Item.DisplayName,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                FontSize = 16,
            }
        );

        for (var i = 0; i < request.Item.Parameters.Count; i++)
        {
            root.Children.Add(BuildParameterEditor(request.Item.Parameters[i]));
        }

        _hiddenCheckBox = new CheckBox { Content = "Hidden", IsChecked = request.Item.IsHidden };
        root.Children.Add(_hiddenCheckBox);

        _errorTextBlock = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            IsVisible = false,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        root.Children.Add(_errorTextBlock);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => Close(false);
        buttonPanel.Children.Add(cancelButton);

        var okButton = new Button { Content = "OK", MinWidth = 84 };
        okButton.Click += (_, _) =>
        {
            if (TryApplyValues())
            {
                Close(true);
            }
        };
        buttonPanel.Children.Add(okButton);

        root.Children.Add(buttonPanel);
        Content = root;
    }

    public static async Task<bool> ShowAsync(Control owner, TradingIndicatorEditorRequest request)
    {
        if (TopLevel.GetTopLevel(owner) is not Window ownerWindow)
        {
            return false;
        }

        var dialog = new DemoIndicatorEditorDialog(request);
        return await dialog.ShowDialog<bool>(ownerWindow);
    }

    private Control BuildParameterEditor(TradingIndicatorParameterValue parameter)
    {
        var container = new StackPanel { Spacing = 4 };
        container.Children.Add(new TextBlock { Text = parameter.Definition.DisplayName });

        Control editor = parameter.Definition.Kind switch
        {
            TradingIndicatorParameterKind.Boolean => new CheckBox
            {
                IsChecked = parameter.Value is bool boolValue && boolValue,
            },
            _ => new TextBox
            {
                Text = parameter.Value?.ToString() ?? string.Empty,
                Watermark = parameter.Definition.DefaultValue?.ToString(),
            },
        };

        if (!string.IsNullOrWhiteSpace(parameter.Definition.Description))
        {
            container.Children.Add(
                new TextBlock
                {
                    Text = parameter.Definition.Description,
                    Opacity = 0.7,
                    FontSize = 11,
                }
            );
        }

        container.Children.Add(editor);
        _editors[parameter] = editor;
        return container;
    }

    private bool TryApplyValues()
    {
        _errorTextBlock.IsVisible = false;
        _errorTextBlock.Text = string.Empty;

        for (var i = 0; i < _item.Parameters.Count; i++)
        {
            var parameter = _item.Parameters[i];
            if (!_editors.TryGetValue(parameter, out var editor))
            {
                continue;
            }

            if (!TryParseValue(parameter.Definition, editor, out var parsedValue, out var error))
            {
                _errorTextBlock.Text = error;
                _errorTextBlock.IsVisible = true;
                return false;
            }

            parameter.Value = parsedValue;
        }

        _item.IsHidden = _hiddenCheckBox.IsChecked == true;
        return true;
    }

    private static bool TryParseValue(
        TradingIndicatorParameterDefinition definition,
        Control editor,
        out object? value,
        out string error
    )
    {
        error = string.Empty;
        switch (definition.Kind)
        {
            case TradingIndicatorParameterKind.Boolean:
                value = editor is CheckBox checkBox && checkBox.IsChecked == true;
                return true;
            case TradingIndicatorParameterKind.Integer:
                if (editor is TextBox intBox && int.TryParse(intBox.Text, out var intValue))
                {
                    value = intValue;
                    return true;
                }

                value = null;
                error = $"{definition.DisplayName} must be an integer.";
                return false;
            case TradingIndicatorParameterKind.Double:
                if (
                    editor is TextBox doubleBox
                    && double.TryParse(doubleBox.Text, out var doubleValue)
                )
                {
                    value = doubleValue;
                    return true;
                }

                value = null;
                error = $"{definition.DisplayName} must be a number.";
                return false;
            default:
                value = editor is TextBox textBox ? textBox.Text ?? string.Empty : string.Empty;
                return true;
        }
    }
}
