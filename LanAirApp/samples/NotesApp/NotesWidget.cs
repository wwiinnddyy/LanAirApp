using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LanMountainDesktop.AirAppSdk;

namespace NotesApp;

/// <summary>
/// 笔记组件
/// 显示笔记预览和快捷打开按钮
/// </summary>
public sealed class NotesWidget : AirAppWidgetBase
{
    private readonly TextBlock _titleText;
    private readonly Button _openButton;

    public NotesWidget()
    {
        _titleText = new TextBlock
        {
            Text = "📝 笔记",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _openButton = new Button
        {
            Content = "打开笔记",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };
        _openButton.Click += async (s, e) => await OpenNotesWindowAsync();

        var panel = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(_titleText);
        panel.Children.Add(_openButton);

        Content = panel;
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        _titleText.Foreground = new SolidColorBrush(snapshot.ForegroundColor);
    }

    private async Task OpenNotesWindowAsync()
    {
        try
        {
            await Context.OpenWindowAsync("notes-window");
        }
        catch (Exception ex)
        {
            Context.Logger.Error("打开笔记窗口失败", ex);
        }
    }
}
