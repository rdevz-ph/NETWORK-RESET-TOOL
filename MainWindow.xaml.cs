using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using NetworkResetTool.Models;

namespace NetworkResetTool
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<AdapterInfo> Adapters { get; } = new();
        public ObservableCollection<ResetStep> ResetSteps { get; } = new();
        public ObservableCollection<LogMessage> LogMessages { get; } = new();

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var helper = new WindowInteropHelper(this);
                int useDarkMode = 1;
                DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            }
            catch
            {
                try
                {
                    var helper = new WindowInteropHelper(this);
                    int useDarkMode = 1;
                    DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
                }
                catch
                {
                    // Ignore on older operating systems
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            // Set bindings
            AdaptersItemsControl.ItemsSource = Adapters;
            StepsItemsControl.ItemsSource = ResetSteps;
            LogsItemsControl.ItemsSource = LogMessages;

            // Auto scroll terminal log to the bottom when new logs are added
            LogMessages.CollectionChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() => LogScrollViewer.ScrollToEnd()));
            };

            // Initialize Reset Steps
            InitializeResetSteps();

            // Load initial network configuration
            LoadNetworkConfigurations();
        }

        private void InitializeResetSteps()
        {
            ResetSteps.Add(new ResetStep { Command = "ipconfig /flushdns", Description = "Flush DNS Cache" });
            ResetSteps.Add(new ResetStep { Command = "ipconfig /release", Description = "Release IP Addresses" });
            ResetSteps.Add(new ResetStep { Command = "ipconfig /renew", Description = "Renew IP Addresses" });
            ResetSteps.Add(new ResetStep { Command = "netsh winsock reset", Description = "Reset Winsock Catalog" });
            ResetSteps.Add(new ResetStep { Command = "netsh int ip reset", Description = "Reset TCP/IP Stack" });
            ResetSteps.Add(new ResetStep { Command = "netsh interface ipv4 reset", Description = "Reset IPv4 Interface" });
            ResetSteps.Add(new ResetStep { Command = "netsh interface ipv6 reset", Description = "Reset IPv6 Interface" });
            ResetSteps.Add(new ResetStep { Command = "netsh interface tcp reset", Description = "Reset TCP Configurations" });
        }

        private void LoadNetworkConfigurations()
        {
            Adapters.Clear();
            AddLog("INFO", "Scanning active network interfaces...");

            var detected = NetworkManager.GetActiveAdapters();
            // Prioritize active adapters ("Up") on top, inactive/disabled ("Down", etc.) below
            var sorted = detected.OrderByDescending(a => a.Status == "Up").ThenBy(a => a.Name);
            
            foreach (var adapter in sorted)
            {
                Adapters.Add(adapter);
            }

            AddLog("INFO", $"Found {detected.Count} network adapter(s).");
        }

        private void AddLog(string level, string message)
        {
            LogMessages.Add(new LogMessage { Level = level, Message = message });
        }

        private void RefreshAdapters_Click(object sender, RoutedEventArgs e)
        {
            LoadNetworkConfigurations();
        }

        private async void StartResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable UI inputs to prevent double execution
            StartResetButton.IsEnabled = false;
            AddLog("INFO", "Initializing network reset pipeline...");

            // Reset step statuses and progress bar
            foreach (var step in ResetSteps)
            {
                step.Status = StepStatus.Pending;
                step.StatusText = "Pending";
            }
            ResetProgressBar.Maximum = ResetSteps.Count;
            ResetProgressBar.Value = 0;

            bool overallSuccess = true;

            for (int i = 0; i < ResetSteps.Count; i++)
            {
                var step = ResetSteps[i];
                if (!step.IsSelected)
                {
                    step.StatusText = "Ignored";
                    AddLog("INFO", $"Skipping step: {step.Command}");
                    ResetProgressBar.Value = i + 1;
                    continue;
                }

                step.Status = StepStatus.Running;
                step.StatusText = "Running";

                AddLog("CMD", $"Executing: {step.Command}");

                // Run command asynchronously
                var result = await NetworkManager.ExecuteCommandAsync(
                    step.Command,
                    onOutputLine => Dispatcher.Invoke(() => AddLog("OUT", onOutputLine)),
                    onErrorLine => Dispatcher.Invoke(() => AddLog("ERROR", onErrorLine))
                );

                if (result.ExitCode == 0)
                {
                    step.Status = StepStatus.Success;
                    step.StatusText = "Success";
                    AddLog("SUCCESS", $"Command completed successfully.");
                }
                else
                {
                    step.Status = StepStatus.Failed;
                    step.StatusText = "Failed";
                    AddLog("ERROR", $"Command failed with exit code: {result.ExitCode}");
                    overallSuccess = false;
                }

                ResetProgressBar.Value = i + 1;

                // Add a small delay for visual feedback between steps
                await Task.Delay(500);
            }

            if (overallSuccess)
            {
                AddLog("SUCCESS", "Network reset pipeline completed successfully. A computer restart is highly recommended to finalize resets.");
                MessageBox.Show(
                    "Network resets have been successfully executed.\n\nIt is highly recommended that you restart your computer now to apply all registry changes.",
                    "Network Reset Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                AddLog("ERROR", "One or more reset steps failed. Please review the error log above.");
                MessageBox.Show(
                    "The network reset process completed with some errors. Please check the logs for detail.",
                    "Reset Completed With Errors",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // Reload configurations at the end of reset
            LoadNetworkConfigurations();

            // Re-enable start button
            StartResetButton.IsEnabled = true;
        }
    }
}