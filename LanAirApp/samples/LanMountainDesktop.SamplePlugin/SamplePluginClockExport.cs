using LanMountainDesktop.SharedContracts.SampleClock;

namespace LanMountainDesktop.SamplePlugin;

internal sealed class SamplePluginClockExport : ISampleClockExport
{
    private readonly SamplePluginClockService _clockService;

    public SamplePluginClockExport(SamplePluginClockService clockService)
    {
        _clockService = clockService;
    }

    public string GetCurrentTimeText()
    {
        return _clockService.CurrentTime.LocalDateTime.ToString("HH:mm:ss");
    }
}
