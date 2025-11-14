using ScottPlot;
using ScottPlot.AxisRules;
using ScottPlot.Plottables;
using ScottPlot.Statistics;
using ScottPlot.TickGenerators;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<PingResult> PingResults;
        bool isRunning = false;
        bool isUserInteract = false;
        int filter = 0;
        string? prevHost = null;
        readonly int initialPingRetry = 3;

        HistogramBars histPlot;
        Histogram hist;

        Status status;

        public MainWindow()
        {
            InitializeComponent();
            Plot1.Plot.YLabel("Count");
            Plot1.Plot.XLabel("Ping (ms)");

            hostTextBox.Text = "192.168.0.1";

            Init();

            Plot1.MouseWheel += Plot1_MouseWheel;
            Plot1.MouseDown += Plot1_MouseDown;
        }

        private void Init()
        {
            PingResults = [];
            status = new();

            Plot1.Plot.Clear();
            hist = Histogram.WithBinSize(1, 0, 80);
            histPlot = Plot1.Plot.Add.Histogram(hist);
            histPlot.BarWidthFraction = 0.8;

            Plot1.Plot.Axes.Rules.Add(new LockedBottom(Plot1.Plot.Axes.Left, 0));
            Plot1.Plot.Axes.Rules.Add(new LockedLeft(Plot1.Plot.Axes.Bottom, 0));

            Plot1.Refresh();
            UserInteracted(false);
        }

        private void Plot1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UserInteracted(true);
        }

        private void Plot1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            UserInteracted(true);
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning) return;

            startBtn.IsEnabled = false;

            var host = hostTextBox.Text;
            if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                status.StatusMessage = StatusMessages.HostError;
                SetStatus();
                return;
            }

            status.StatusMessage = StatusMessages.Starting;
            SetStatus();

            var ping = new Ping();
            PingReply reply = null;
            int i = 0;
            await Task.Run(() =>
            {
                try
                {
                    do
                    {
                        reply = ping.Send(host);
                        if (reply.Status == IPStatus.Success)
                            break;

                        Task.Delay(500);
                    }
                    while (i++ < initialPingRetry);
                }
                catch { }
            });

            if (reply == null || reply.Status != IPStatus.Success)
            {
                status.StatusMessage = StatusMessages.PingError;
                SetStatus();
                return;
            }

            if (prevHost != null && prevHost != host)
            {
                Init();
            }

            isRunning = true;
            stopBtn.IsEnabled = true;
            prevHost = host;
            StartPing(host);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            StopProcess();

            status.StatusMessage = StatusMessages.Stopped;
            SetStatus();
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            var statusPrev = status.StatusMessage;
            Init();
            status.StatusMessage = statusPrev;
            SetStatus();
        }
        private void ResetViewBtn_Click(object sender, RoutedEventArgs e)
        {
            UserInteracted(false);
        }

        private void filterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(filterTextBox.Text, out int flt))
            {
                filter = flt;
            }
            else
            {
                filter = 0;
            }
        }

        private async void StartPing(string host)
        {
            await Task.Run(() =>
            {
                Ping pingSender = new Ping();

                try
                {
                    while (isRunning)
                    {
                        PingReply reply = pingSender.Send(host);

                        if (reply.Status == IPStatus.Success)
                        {
                            PingResults.Add(new PingResult(reply));
                            if (PingResults.Count < 2) continue;

                            var pings = PingResults.Select(p => (double)p.Value);
                            var filtered = pings.Where(p => p >= filter);
                            Dispatcher.Invoke(() =>
                            {
                                if (filtered.Any() && hist.Bins.Max() < filtered.Max())
                                {
                                    hist = Histogram.WithBinSize(1, filtered);
                                    Plot1.Plot.Clear();
                                    histPlot = Plot1.Plot.Add.Histogram(hist);
                                    histPlot.BarWidthFraction = 0.8;
                                }

                                hist.Clear();
                                hist.AddRange(filtered);

                                if (!isUserInteract)
                                {
                                    Plot1.Plot.Axes.AutoScale();
                                }
                                else
                                {
                                    // ToDo return zoom back or change from histogram to bars
                                }

                                Plot1.Refresh();

                                status.Update(pings.Count(), Math.Floor(pings.Average()), pings.Min(), pings.Max(), pings.Median(), StatusMessages.Running);
                                status.Filtered = filter > 0 ? filtered.Count() : null;
                                SetStatus();
                            });
                        }
                        else
                        {
                            Debug.WriteLine($"Ping failed: {reply.Status}");
                            status.Lost++;

                            Dispatcher.Invoke(() =>
                            {
                                SetStatus();
                            });
                        }
                        Task.Delay(1000).Wait();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error: {e.Message}");

                    status.StatusMessage = StatusMessages.PingError;
                    Dispatcher.Invoke(() =>
                    {
                        StopProcess();
                        SetStatus();
                    });
                }
            });
        }

        private void StopProcess()
        {
            isRunning = false;
            stopBtn.IsEnabled = false;
            startBtn.IsEnabled = true;
        }

        private void UserInteracted(bool flag)
        {
            if (flag)
            {
                isUserInteract = true;
                ResetViewBtn.IsEnabled = true;
            }
            else
            {
                isUserInteract = false;
                ResetViewBtn.IsEnabled = false;
                Plot1.Plot.Axes.AutoScale();
                Plot1.Refresh();
            }
        }

        private void SetStatus()
        {
            statusTextBlock.Text = status.ToString();
        }
    }
}