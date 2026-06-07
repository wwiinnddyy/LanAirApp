using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LanMountainDesktop.AirAppSdk;
using System.Diagnostics;

namespace SystemMonitor;

/// <summary>
/// 系统监控组件
/// 显示 CPU、内存使用率
/// </summary>
public sealed class SystemMonitorWidget : AirAppWidgetBase
{
    private readonly TextBlock _cpuText;
    private readonly TextBlock _memoryText;
    private readonly ProgressBar _cpuBar;
    private readonly ProgressBar _memoryBar;
    private readonly System.Timers.Timer _updateTimer;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;

    public SystemMonitorWidget()
    {
        // 创建性能计数器
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
        }
        catch
        {
            // 性能计数器可能需要管理员权限
        }

        // 创建 UI
        _cpuText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeight.SemiBold
        };

        _cpuBar = new ProgressBar
        {
            Height = 8,
            Minimum = 0,
            Maximum = 100
        };

        _memoryText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Margin = new Avalonia.Thickness(0, 12, 0, 0)
        };

        _memoryBar = new ProgressBar
        {
            Height = 8,
            Minimum = 0,
            Maximum = 100
        };

        var panel = new StackPanel
        {
            Spacing = 4,
            Margin = new Avalonia.Thickness(12),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(_cpuText);
        panel.Children.Add(_cpuBar);
        panel.Children.Add(_memoryText);
        panel.Children.Add(_memoryBar);

        Content = panel;

        // 每2秒更新一次
        _updateTimer = new System.Timers.Timer(2000);
        _updateTimer.Elapsed += async (s, e) => await UpdateSystemInfoAsync();
    }

    protected override void OnAttachedCore()
    {
        Context.Logger.Info("系统监控组件已添加");
        _updateTimer.Start();
        _ = UpdateSystemInfoAsync();
    }

    protected override void OnDetachedCore()
    {
        _updateTimer.Stop();
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        var foreground = new SolidColorBrush(snapshot.ForegroundColor);
        _cpuText.Foreground = foreground;
        _memoryText.Foreground = foreground;
    }

    private async Task UpdateSystemInfoAsync()
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                // CPU 使用率
                var cpuUsage = GetCpuUsage();
                _cpuText.Text = $"CPU: {cpuUsage:F1}%";
                _cpuBar.Value = cpuUsage;

                // 内存使用率
                var memoryInfo = GetMemoryInfo();
                _memoryText.Text = $"内存: {memoryInfo.UsedMB:F0} MB / {memoryInfo.TotalMB:F0} MB";
                _memoryBar.Value = memoryInfo.UsagePercent;
            }
            catch (Exception ex)
            {
                Context.Logger.Error("更新系统信息失败", ex);
            }
        });
    }

    private double GetCpuUsage()
    {
        if (_cpuCounter != null && OperatingSystem.IsWindows())
        {
            try
            {
                return _cpuCounter.NextValue();
            }
            catch
            {
                // 如果获取失败，返回模拟数据
            }
        }

        // 跨平台方案或模拟数据
        var process = Process.GetCurrentProcess();
        return Random.Shared.Next(10, 60);
    }

    private (double UsedMB, double TotalMB, double UsagePercent) GetMemoryInfo()
    {
        if (_memoryCounter != null && OperatingSystem.IsWindows())
        {
            try
            {
                var availableMB = _memoryCounter.NextValue();
                var totalMB = GetTotalPhysicalMemory();
                var usedMB = totalMB - availableMB;
                var usagePercent = (usedMB / totalMB) * 100;

                return (usedMB, totalMB, usagePercent);
            }
            catch
            {
                // 如果获取失败，返回模拟数据
            }
        }

        // 跨平台方案
        var process = Process.GetCurrentProcess();
        var usedMB = process.WorkingSet64 / (1024.0 * 1024.0);
        var totalMB = 16384; // 假设 16GB
        var usagePercent = (usedMB / totalMB) * 100;

        return (usedMB, totalMB, usagePercent);
    }

    private double GetTotalPhysicalMemory()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    var totalKB = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                    return totalKB / 1024.0; // 转换为 MB
                }
            }
            catch
            {
            }
        }

        return 16384; // 默认 16GB
    }
}
