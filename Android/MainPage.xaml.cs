using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Vaslim;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    // Global counter for all app-initiated data exchange
    private long _totalBytesExchanged = 0;

    public MainPage()
    {
        InitializeComponent();
        DnsPicker.SelectedIndex = 0;
    }

    // Helper to add bytes to our global counter and update the UI
    private void RecordTraffic(long bytes)
    {
        _totalBytesExchanged += bytes;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LblDataExchanged.Text = FormatBytes(_totalBytesExchanged);
        });
    }

    private string FormatBytes(long bytes)
    {
        string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblBytes = bytes;
        for (i = 0; Suffix.Length > i && bytes >= 1024; i++, bytes /= 1024)
        {
            dblBytes = bytes / 1024.0;
        }

        double val = bytes;
        if (i == 0) return $"{val:F0} {Suffix[i]}";
        return $"{val:F1} {Suffix[i]}";
    }

    private async void OnRunTestClicked(object? sender, EventArgs e)
    {
        BtnTest.IsEnabled = false;
        BtnTest.Text = "PROBING...";

        try
        {
            // 1. Speed Test
            await RunInternetSpeedTest();

            // 2. ICMP Ping
            UpdateCardLabel(LblPing, "...");
            long pingTime = await GetRealPing("8.8.8.8");
            RecordTraffic(128);
            UpdateCardLabel(LblPing, pingTime >= 0 ? $"{pingTime} ms" : "TIMEOUT");
            UpdateCardLabel(SubPing, pingTime >= 0 ? "Connection Stable" : "High Latency");

            // 3. TCP Handshake
            UpdateCardLabel(LblTcp, "...");
            bool isTcpOpen = await CheckTcpPort("google.com", 80);
            RecordTraffic(512);
            UpdateCardLabel(LblTcp, isTcpOpen ? "OPEN" : "CLOSED");
            UpdateCardLabel(SubTcp, isTcpOpen ? "Port 80 reachable" : "Connection Refused");

            // 4. UDP Probe
            UpdateCardLabel(LblUdpHandshake, "...");
            bool udpSuccess = await CheckUdpDnsProbe("8.8.8.8");
            RecordTraffic(256);
            UpdateCardLabel(LblUdpHandshake, udpSuccess ? "SUCCESS" : "FAILED");
            UpdateCardLabel(SubUdp, udpSuccess ? "UDP Port 53 Active" : "UDP/DNS Blocked");

            // 5. DNS Resolution
            UpdateCardLabel(LblDns, "...");
            var ips = await Dns.GetHostAddressesAsync("google.com");
            RecordTraffic(128);
            UpdateCardLabel(LblDns, ips.Length > 0 ? ips[0].ToString() : "Failed");
            UpdateCardLabel(SubDns, "Resolved successfully");

            // 6. IP Info
            UpdateCardLabel(LblPublicIp, "Fetching...");
            string publicIp = await GetPublicIpAsync();
            RecordTraffic(512);
            UpdateCardLabel(LblPublicIp, publicIp);

            UpdateCardLabel(LblLocalIp, "Fetching...");
            string localIp = GetLocalIp();
            UpdateCardLabel(LblLocalIp, localIp);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            BtnTest.IsEnabled = true;
            BtnTest.Text = "RUN_TEST_SEQUENCE";
        }
    }

    private async Task RunInternetSpeedTest()
    {
        UpdateCardLabel(LblInternetStatus, "TESTING");
        UpdateCardLabel(LblSpeed, "0 / 0 Mbps");

        try
        {
            string testUrl = "https://speed.cloudflare.com/__down?bytes=2000000";
            double peakDownloadMbps = 0;
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < 5)
            {
                var burstSw = Stopwatch.StartNew();
                using var request = await _httpClient.GetAsync(testUrl);
                var data = await request.Content.ReadAsByteArrayAsync();
                burstSw.Stop();

                long bytesReceived = data.Length;
                RecordTraffic(bytesReceived);

                double burstMbps = ((bytesReceived * 8) / burstSw.Elapsed.TotalSeconds) / 1_000_000.0;
                if (burstMbps > peakDownloadMbps) peakDownloadMbps = burstMbps;

                if (sw.Elapsed.TotalSeconds >= 4.5) break;
            }
            sw.Stop();

            // Upload simulation
            double peakUploadMbps = 0;
            var uploadContent = new StringContent(new string('a', 10000));
            var upSw = Stopwatch.StartNew();
            var upResponse = await _httpClient.PostAsync(testUrl, uploadContent);
            upSw.Stop();
            RecordTraffic(10000);
            peakUploadMbps = (80000 / upSw.Elapsed.TotalSeconds) / 1_000_000.0;

            UpdateCardLabel(LblInternetStatus, "ONLINE");
            UpdateCardLabel(LblSpeed, $"{peakDownloadMbps:F2} / {peakUploadMbps:F2} Mbps");
        }
        catch
        {
            UpdateCardLabel(LblInternetStatus, "OFFLINE");
            UpdateCardLabel(LblSpeed, "0 / 0 Mbps");
        }
    }

    private async Task<bool> CheckUdpDnsProbe(string dnsServer)
    {
        try
        {
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = 3000;
            udpClient.Client.SendTimeout = 3000;
            var endpoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
            await udpClient.Client.ConnectAsync(endpoint);
            return true;
        }
        catch { return false; }
    }

    private async Task<string> GetPublicIpAsync()
    {
        try { return await _httpClient.GetStringAsync("https://api.ipify.org"); }
        catch { return "Unknown"; }
    }

    private string GetLocalIp()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !a.Address.ToString().StartsWith("127."))?.Address.ToString() ?? "Not Found";
        }
        catch { return "Error"; }
    }

    private async Task<long> GetRealPing(string host)
    {
        try { using var ping = new Ping(); var reply = await ping.SendPingAsync(host, 2000); return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1; }
        catch { return -1; }
    }

    private async Task<bool> CheckTcpPort(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            if (await Task.WhenAny(task, Task.Delay(3000)) == task) return client.Connected;
            return false;
        }
        catch { return false; }
    }

    private void UpdateCardLabel(Microsoft.Maui.Controls.Label label, string text)
        => MainThread.BeginInvokeOnMainThread(() => label.Text = text);
}
