using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace Vaslim;

// 1. Model for Sidebar Games
public class GameCategory : INotifyPropertyChanged
{
    public string GameTitle { get; set; } = "";
    public List<GameServerModel> Servers { get; set; } = new();
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// 2. Model for actual Servers
public class GameServerModel : INotifyPropertyChanged
{
    private int _pingValue;
    private Color _pingColor = Colors.Gray;
    private Color _statusColor = Colors.Gray;

    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string Address { get; set; } = "";

    public int PingValue
    {
        get => _pingValue;
        set { _pingValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(PingColor)); }
    }

    public Color PingColor
    {
        get
        {
            if (PingValue == -1) return Color.FromArgb("#F9294A");
            if (PingValue < 50) return Color.FromArgb("#A6E22E");
            if (PingValue < 100) return Color.FromArgb("#E6DB70");
            return Color.FromArgb("#F9294A");
        }
    }

    public Color StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// 3. The Page Logic
public partial class GamePing : ContentPage
{
    public ObservableCollection<GameCategory> Categories { get; set; } = new();
    private ObservableCollection<GameServerModel> _currentServers = new();
    private bool _isLoopRunning = false;
    private GameCategory? _selectedCategory;

    public GamePing()
    {
        InitializeComponent();
        SetupData();

        GameSelectorList.ItemsSource = Categories;
        ServerList.ItemsSource = _currentServers;

        // Start the background loop once when the page loads
        _ = PingLoopAsync();
    }

    private void SetupData()
    {
        var fortnite = new GameCategory { GameTitle = "FORTNITE" };
        //.Servers.Add(new GameServerModel { Name = "", Region = "", Address = "" });
        fortnite.Servers.Add(new GameServerModel { Name = "North America", Region = "NA-East", Address = "ping-nae.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "North America", Region = "NA-Central", Address = "ping-nac.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "North America", Region = "NA-West", Address = "ping-naw.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "Europe", Region = "EU", Address = "ping-eu.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "Oceania", Region = "Oceania", Address = "ping-oce.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "Brazil", Region = "Brazil", Address = "ping-br.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "Asia", Region = "Asia", Address = "ping-asia.ds.on.epicgames.com" });
        fortnite.Servers.Add(new GameServerModel { Name = "Middle East", Region = "ME", Address = "ping-me.ds.on.epicgames.com" });

        var WoW = new GameCategory { GameTitle = "World of Warcraft" };
        WoW.Servers.Add(new GameServerModel { Name = "World of Warcraft", Region = "Europe", Address = "eu.actual.battle.net" });

        var Overwatch = new GameCategory { GameTitle = "Overwatch 2" };
        Overwatch.Servers.Add(new GameServerModel { Name = "Battle.net", Region = "Europe", Address = "eu.actual.battle.net" });

        var Minecraft = new GameCategory { GameTitle = "Minecraft" };
        Minecraft.Servers.Add(new GameServerModel { Name = "Mojang", Region = "Unknown", Address = "sessionserver.mojang.com" });

        var warthunder = new GameCategory { GameTitle = "War Thunder" };
        warthunder.Servers.Add(new GameServerModel { Name = "warthunder.com", Region = "Europe", Address = "warthunder.com" });

        var Roblox = new GameCategory { GameTitle = "Roblox" };
        Roblox.Servers.Add(new GameServerModel { Name = "Roblox", Region = "Europe", Address = "gamejoin.roblox.com" });

        var lol = new GameCategory { GameTitle = "League of Legends" };
        lol.Servers.Add(new GameServerModel { Name = "LOL EUW1", Region = "Europe", Address = "euw1.lol.riotgames.com" });

        var AWS = new GameCategory { GameTitle = "Amazon Web Services" };
        AWS.Servers.Add(new GameServerModel { Name = "Frankfurt", Region = "Europe", Address = "ec2.eu-central-1.amazonaws.com" });
        AWS.Servers.Add(new GameServerModel { Name = "Ohio", Region = "North America", Address = "ec2.us-east-2.amazonaws.com" });
        AWS.Servers.Add(new GameServerModel { Name = "Tokyo", Region = "Asia", Address = "ec2.ap-northeast-1.amazonaws.com" });
        AWS.Servers.Add(new GameServerModel { Name = "UAE", Region = "Middle East", Address = "ec2.me-central-1.amazonaws.com" });

        var Azure = new GameCategory { GameTitle = "Microsoft Azure" };
        Azure.Servers.Add(new GameServerModel { Name = "Germany", Region = "Europe", Address = "germanywestcentral.cloudapp.azure.com" });
        Azure.Servers.Add(new GameServerModel { Name = "Central US", Region = "North America", Address = "centralus.cloudapp.azure.com" });
        Azure.Servers.Add(new GameServerModel { Name = "Southeast Asia", Region = "Asia", Address = "southeastasia.cloudapp.azure.com" });
        Azure.Servers.Add(new GameServerModel { Name = "UAE", Region = "Middle East", Address = "uaenorth.cloudapp.azure.com" });

        Categories.Add(fortnite);
        Categories.Add(WoW);
        Categories.Add(Minecraft);
        Categories.Add(Overwatch);
        Categories.Add(warthunder);
        Categories.Add(Roblox);
        Categories.Add(lol);
        Categories.Add(AWS);
        Categories.Add(Azure);

    }


    private async void OnGameSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is GameCategory selected)
        {
            foreach (var cat in Categories) cat.IsSelected = false;
            selected.IsSelected = true;
            _selectedCategory = selected;

            LblEmptyState.IsVisible = false;
            LblCurrentGameTitle.Text = $"// {selected.GameTitle}_SERVERS";

            _currentServers.Clear();
            foreach (var s in selected.Servers) _currentServers.Add(s);
        }
    }

    // This replaces the Timer entirely with a much safer async loop
    private async Task PingLoopAsync()
    {
        _isLoopRunning = true;

        while (_isLoopRunning)
        {
            if (_selectedCategory != null && _currentServers.Count > 0)
            {
                var tasks = _currentServers.Select(async server =>
                {
                    int latency = await GetPingAsync(server.Address);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        server.PingValue = latency;
                        server.StatusColor = latency >= 0 ? Color.FromArgb("#A6E22E") : Color.FromArgb("#F9294A");
                    });
                });

                await Task.WhenAll(tasks);
            }

            // Wait for 3 seconds before the next round of pings
            await Task.Delay(3000);
        }
    }

    private async Task<int> GetPingAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 2000);
            return reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1;
        }
        catch { return -1; }
    }

    private async void BackToMain(object sender, EventArgs e)
    {
        _isLoopRunning = false; // Stop the loop when leaving page
        await Navigation.PopAsync();
    }
}
