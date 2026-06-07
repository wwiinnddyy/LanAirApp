using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LanMountainDesktop.AirAppSdk;
using System.IO;
using System.Text.Json;

namespace NotesApp;

/// <summary>
/// 笔记应用窗口
/// 提供简单的文本编辑和保存功能
/// </summary>
public sealed class NotesWindow : AirAppWindowBase
{
    private readonly TextBox _textBox;
    private readonly Button _saveButton;
    private readonly Button _clearButton;
    private readonly TextBlock _statusText;
    private string _notesFilePath = "";

    public override AirAppWindowDescriptor Descriptor => new()
    {
        Title = "笔记",
        Width = 600,
        Height = 400,
        MinWidth = 400,
        MinHeight = 300,
        ChromeMode = AirAppWindowChromeMode.Standard,
        CanResize = true,
        ShowInTaskbar = true
    };

    public NotesWindow()
    {
        // 创建 UI
        _textBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            FontSize = 14,
            Padding = new Avalonia.Thickness(8)
        };

        _saveButton = new Button
        {
            Content = "保存",
            Width = 80,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        _saveButton.Click += async (s, e) => await SaveNotesAsync();

        _clearButton = new Button
        {
            Content = "清空",
            Width = 80
        };
        _clearButton.Click += (s, e) => ClearNotes();

        _statusText = new TextBlock
        {
            FontSize = 12,
            Opacity = 0.6,
            Margin = new Avalonia.Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(8)
        };
        buttonPanel.Children.Add(_saveButton);
        buttonPanel.Children.Add(_clearButton);
        buttonPanel.Children.Add(_statusText);

        var mainPanel = new DockPanel();
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(_textBox);

        Content = mainPanel;
    }

    public override async Task OnWindowOpeningAsync()
    {
        // 确定笔记文件路径
        var dataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(dataDir, "LanMountainDesktop", "NotesApp");
        Directory.CreateDirectory(appDir);
        _notesFilePath = Path.Combine(appDir, "notes.txt");

        // 加载已保存的笔记
        if (File.Exists(_notesFilePath))
        {
            _textBox.Text = await File.ReadAllTextAsync(_notesFilePath);
            UpdateStatus("笔记已加载");
        }
        else
        {
            UpdateStatus("开始写笔记吧...");
        }
    }

    private async Task SaveNotesAsync()
    {
        try
        {
            await File.WriteAllTextAsync(_notesFilePath, _textBox.Text ?? "");
            UpdateStatus($"已保存 ({DateTime.Now:HH:mm:ss})");
        }
        catch (Exception ex)
        {
            UpdateStatus($"保存失败: {ex.Message}");
        }
    }

    private void ClearNotes()
    {
        _textBox.Text = "";
        UpdateStatus("已清空");
    }

    private void UpdateStatus(string message)
    {
        _statusText.Text = message;
    }
}
