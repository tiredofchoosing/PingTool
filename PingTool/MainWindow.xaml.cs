using ScottPlot.Plottables;
using ScottPlot.Statistics;
using ScottPlot.AxisRules;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;

namespace PingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<PingResult> PingResults = [];
        bool isRunning = false;
        bool isUserInteract = false;
        int filter = 0;

        HistogramBars histPlot;
        Histogram hist;

        public MainWindow()
        {
            InitializeComponent();
            Plot1.Plot.YLabel("Count");
            Plot1.Plot.XLabel("Ping (ms)");

            hist = Histogram.WithBinSize(2, 0, 100);
            histPlot = Plot1.Plot.Add.Histogram(hist);
            histPlot.BarWidthFraction = 0.8;

            Plot1.Plot.Axes.Rules.Add(new LockedBottom(Plot1.Plot.Axes.Left, 0));
            Plot1.Plot.Axes.Rules.Add(new LockedLeft(Plot1.Plot.Axes.Bottom, 0));

            Plot1.MouseWheel += Plot1_MouseWheel;
            Plot1.MouseDown += Plot1_MouseDown;
        }

        private void Plot1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UserInteracted(true);
        }

        private void Plot1_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            UserInteracted(true);
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning) return;

            var host = hostTextBox.Text;
            if (string.IsNullOrWhiteSpace(host))
            {
                return;
            }
            isRunning = true;
            statusTextBlock.Text = "Starting";
            StartPing(host);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false;
            statusTextBlock.Text = "Stopped";
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
                                if (hist.Bins.Max() < filtered.Max())
                                {
                                    hist = Histogram.WithBinSize(2, filtered);
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
                                
                                Plot1.Refresh();

                                statusTextBlock.Text = 
                                $"Total: {pings.Count()} | " +
                                $"Avg: {Math.Floor(pings.Average())} | " +
                                $"Min: {pings.Min()} | " +
                                $"Max: {pings.Max()}";

                                if (filter > 0)
                                {
                                    statusTextBlock.Text +=
                                    $"    |    Filtered: {filtered.Count()} | " +
                                    $"Avg: {Math.Floor(filtered.Average())} | " +
                                    $"Min: {filtered.Min()} | " +
                                    $"Max: {filtered.Max()}";
                                }
                            });
                        }
                        else
                        {
                            Debug.WriteLine($"Ping failed: {reply.Status}");
                        }
                        Task.Delay(1000).Wait();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error: {e.Message}");
                }

            });
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
        private void ResetViewBtnClick(object sender, RoutedEventArgs e)
        {
            UserInteracted(false);
        }

        private void UserInteracted(bool status)
        {
            if (status)
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
    }

    class PingResult
    {
        public DateTime DateTime { get; }
        public int Value { get; }
        public PingResult(DateTime dateTime, int value)
        {
            DateTime = dateTime;
            Value = value;
        }
        public PingResult(PingReply reply) : this(DateTime.Now, (int)reply.RoundtripTime)
        { }
    }
}