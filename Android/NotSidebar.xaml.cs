using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace Vaslim;

// Keep this class here so both pages can technically use it if needed, 
// but logically it belongs to the Monitor feature.
public class SiteMonitorItem : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    private Color _statusColor = Color.FromArgb("#565F89");
    public Color StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public partial class NotSidebar : ContentPage
{
    private List<SiteMonitorItem> _monitorItems;
    private Timer? _sidebarTimer;
    private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    public NotSidebar()
    {
        InitializeComponent();
        SetupMonitor();
    }

    private void SetupMonitor()
    {
        _monitorItems = new List<SiteMonitorItem>
        {
            new SiteMonitorItem { Name = "Google", Url = "https://www.google.com/" },
            new SiteMonitorItem { Name = "GitHub", Url = "https://github.com/" },
            new SiteMonitorItem { Name = "YouTube", Url = "https://www.youtube.com/" },
            new SiteMonitorItem { Name = "ChatGPT", Url = "https://chatgpt.com/" },
            new SiteMonitorItem { Name = "Spotify", Url = "https://open.spotify.com/" },
            new SiteMonitorItem { Name = "Twitter (X)", Url = "https://twitter.com/" },
            new SiteMonitorItem { Name = "Time.ir", Url = "https://www.time.ir/" },
            new SiteMonitorItem { Name = "Steam", Url = "https://store.steampowered.com/" },
            new SiteMonitorItem { Name = "Telegram", Url = "https://web.telegram.org/" },
            new SiteMonitorItem { Name = "Reddit", Url = "https://www.reddit.com/" },
            new SiteMonitorItem { Name = "Discord", Url = "https://www.discord.com/" },
            new SiteMonitorItem { Name = "TikTok", Url = "https://www.tiktok.com/" },
            new SiteMonitorItem { Name = "WhatsApp", Url = "https://web.whatsapp.com/" },
            new SiteMonitorItem { Name = "Instagram", Url = "https://www.instagram.com/" },
        };

        SiteMonitorList.ItemsSource = _monitorItems; // This now works because it's in this file!
        _ = RunSidebarPingCheck();
        _sidebarTimer = new Timer(async (_) => await RunSidebarPingCheck(), null, 30000, 30000);
    }

    private async Task RunSidebarPingCheck()
    {
        if (_monitorItems == null) return;
        var tasks = _monitorItems.Select(async item =>
        {
            bool isAlive = false;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync(item.Url, cts.Token);
                isAlive = response.IsSuccessStatusCode;
            }
            catch { isAlive = false; }

            MainThread.BeginInvokeOnMainThread(() =>
                item.StatusColor = isAlive ? Color.FromArgb("#A6E22E") : Color.FromArgb("#F9294A"));
        });
        await Task.WhenAll(tasks);
    }
}
