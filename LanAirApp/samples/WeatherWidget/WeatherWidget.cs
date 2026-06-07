using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LanMountainDesktop.AirAppSdk;
using System.Net.Http;
using System.Text.Json;

namespace WeatherWidget;

/// <summary>
/// 天气组件示例
/// 显示当前天气和温度信息
/// </summary>
public sealed class WeatherWidget : AirAppWidgetBase
{
    private readonly TextBlock _cityText;
    private readonly TextBlock _tempText;
    private readonly TextBlock _weatherText;
    private readonly TextBlock _updateTimeText;
    private readonly DispatcherTimer _updateTimer;
    private readonly HttpClient _httpClient;

    public WeatherWidget()
    {
        _httpClient = new HttpClient();

        // 创建 UI
        _cityText = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _tempText = new TextBlock
        {
            FontSize = 36,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _weatherText = new TextBlock
        {
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _updateTimeText = new TextBlock
        {
            FontSize = 10,
            Opacity = 0.6,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var panel = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(_cityText);
        panel.Children.Add(_tempText);
        panel.Children.Add(_weatherText);
        panel.Children.Add(_updateTimeText);

        Content = panel;

        // 每30分钟更新一次
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(30)
        };
        _updateTimer.Tick += async (s, e) => await UpdateWeatherAsync();
    }

    protected override void OnAttachedCore()
    {
        Context.Logger.Info("天气组件已添加");
        _ = UpdateWeatherAsync();
        _updateTimer.Start();
    }

    protected override void OnDetachedCore()
    {
        _updateTimer.Stop();
        _httpClient.Dispose();
    }

    protected override void OnAppearanceChangedCore(AirAppAppearanceSnapshot snapshot)
    {
        var foreground = new SolidColorBrush(snapshot.ForegroundColor);
        _cityText.Foreground = foreground;
        _tempText.Foreground = new SolidColorBrush(snapshot.AccentColor);
        _weatherText.Foreground = foreground;
        _updateTimeText.Foreground = foreground;
    }

    private async Task UpdateWeatherAsync()
    {
        try
        {
            Context.Logger.Debug("正在获取天气数据...");

            // 示例：使用模拟数据
            // 实际应用中应调用真实的天气 API
            var weatherData = await GetMockWeatherDataAsync();

            _cityText.Text = weatherData.City;
            _tempText.Text = $"{weatherData.Temperature}°C";
            _weatherText.Text = weatherData.Description;
            _updateTimeText.Text = $"更新于 {weatherData.UpdateTime:HH:mm}";

            Context.Logger.Info($"天气数据已更新: {weatherData.City} {weatherData.Temperature}°C");
        }
        catch (Exception ex)
        {
            Context.Logger.Error("获取天气数据失败", ex);
            _tempText.Text = "--°C";
            _weatherText.Text = "获取失败";
        }
    }

    private async Task<WeatherData> GetMockWeatherDataAsync()
    {
        // 模拟网络延迟
        await Task.Delay(500);

        // 返回模拟数据
        var random = new Random();
        var temperatures = new[] { 18, 22, 25, 28, 15, 20 };
        var descriptions = new[] { "晴", "多云", "阴", "小雨", "晴转多云" };

        return new WeatherData
        {
            City = "北京",
            Temperature = temperatures[random.Next(temperatures.Length)],
            Description = descriptions[random.Next(descriptions.Length)],
            UpdateTime = DateTime.Now
        };
    }

    private sealed class WeatherData
    {
        public string City { get; init; } = "";
        public int Temperature { get; init; }
        public string Description { get; init; } = "";
        public DateTime UpdateTime { get; init; }
    }
}
