using IHateDPI.Legacy.Launcher.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IHateDPI.Legacy.Launcher;

/// <summary>
/// Interaction logic for the main dashboard view.
/// <para>
/// Manages the application lifecycle, including starting/stopping the DPI engines (IHateDPI / GoodbyeDPI),
/// handling process execution, and updating the UI state with animations.
/// </para>
/// </summary>
public partial class MainView : UserControl
{
    private static string BaseDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
    private static string AppConfigPath => Path.Combine(BaseDir, "appConfig.json");
    private static string IHateDpiPath => Path.Combine(BaseDir, "Engines", "IHateDPI", "IHateDPI Engine.exe");
    private static string GoodbyeDpiDir => Path.Combine(BaseDir, "Engines", "GoodbyeDPI");

    private Process? _engineProcess;
    private SettingsView? _settingsView;

    /// <summary>
    /// Gets a value indicating whether an engine process is currently active.
    /// </summary>
    public bool IsRunning { get; private set; } = false;

    /// <summary>
    /// Occurs when the engine starts or stops.
    /// </summary>
    public event EventHandler<bool>? EngineStateChanged;

    public MainView()
    {
        InitializeComponent();
    }

    // --- EVENT HANDLERS ---

    private void BtnPower_Click(object sender, RoutedEventArgs e)
    {
        if (IsRunning)
            StopEngine();
        else
            StartEngine();
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        // Lazy initialization of the Settings View
        if (_settingsView == null)
        {
            _settingsView = new SettingsView();
            _settingsView.OnCloseRequested += (s, args) =>
            {
                SettingsContainer.Visibility = Visibility.Collapsed;
                DashboardPanel.Visibility = Visibility.Visible;
                _settingsView = null;
                SettingsContainer.Content = null;
            };
        }

        // Switch views
        DashboardPanel.Visibility = Visibility.Collapsed;
        SettingsContainer.Content = _settingsView;
        SettingsContainer.Visibility = Visibility.Visible;
    }

    // --- ENGINE MANAGEMENT ---

    /// <summary>
    /// Attempts to start the selected DPI evasion engine based on the current configuration.
    /// </summary>
    public void StartEngine()
    {
        if (IsRunning) StopEngine();

        var currentConfig = ReadConfig();
        try
        {
            switch (currentConfig.SelectedEngine)
            {
                case EngineType.IHateDPI:
                    StartIHateDPI();
                    break;
                case EngineType.GoodbyeDPI:
                    StartGoodbyeDPI(currentConfig.SelectedGoodbyeDpiScript);
                    break;
                default:
                    throw new InvalidOperationException("Unknown engine type selected.");
            }

            UpdateState(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start engine:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StopEngine();
        }
    }

    /// <summary>
    /// Terminates the active engine process (if any).
    /// </summary>
    public async void StopEngine()
    {
        if (_engineProcess != null && !_engineProcess.HasExited)
        {
            try
            {
                if (_engineProcess.StartInfo.RedirectStandardInput)
                {
                    // Send the specific command we agreed upon
                    await _engineProcess.StandardInput.WriteLineAsync("STOP");

                    // Wait for the Engine to clean up and exit on its own
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    try
                    {
                        await _engineProcess.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // It took too long, time to be brutal
                        Debug.WriteLine("Engine timeout. Forcing kill.");
                        _engineProcess.Kill(true);
                    }
                }
                else
                {
                    _engineProcess.Kill(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop error: {ex.Message}");
                // Last resort
                if (!_engineProcess.HasExited) 
                    _engineProcess.Kill(true);
            }
            finally
            {
                _engineProcess.Dispose();
                _engineProcess = null;
            }
        }

        UpdateState(false);
    }

    private void StartIHateDPI()
    {
        if (!File.Exists(IHateDpiPath))
            throw new FileNotFoundException("IHateDPI Engine.exe not found!");

        var psi = new ProcessStartInfo(IHateDpiPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true, // Run in background
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(IHateDpiPath),
            RedirectStandardInput = true // For graceful shutdown
        };

        _engineProcess = Process.Start(psi);
        TxtActiveEngine.Text = "Motor: IHateDPI";
    }

    private void StartGoodbyeDPI(string? scriptName)
    {
        if (string.IsNullOrEmpty(scriptName))
            throw new Exception("No startup script (.cmd) selected for GoodbyeDPI.");

        string scriptPath = Path.Combine(GoodbyeDpiDir, scriptName);
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Selected script not found: {scriptName}");

        // Extract raw arguments from the batch file to run the executable directly
        string arguments = ExtractArgumentsFromCmd(scriptPath);

        // Determine correct architecture
        string exePath = Path.Combine(GoodbyeDpiDir, "x86_64", "goodbyedpi.exe");
        if (!File.Exists(exePath))
            exePath = Path.Combine(GoodbyeDpiDir, "goodbyedpi.exe");

        if (!File.Exists(exePath))
            throw new FileNotFoundException("goodbyedpi.exe not found! Please check the Engines folder.");

        var psi = new ProcessStartInfo(exePath, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = GoodbyeDpiDir
        };

        _engineProcess = Process.Start(psi);
        TxtActiveEngine.Text = $"Motor: GoodbyeDPI ({scriptName})";
    }

    /// <summary>
    /// Parses a GoodbyeDPI batch file (.cmd) to extract the command-line arguments.
    /// </summary>
    /// <param name="cmdPath">Path to the .cmd file.</param>
    /// <returns>The arguments string, or "-9" (default safe mode) if parsing fails.</returns>
    private static string ExtractArgumentsFromCmd(string cmdPath)
    {
        try
        {
            var lines = File.ReadAllLines(cmdPath);
            foreach (var line in lines)
            {
                string cleanLine = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith("REM") || cleanLine.StartsWith("::"))
                    continue;

                // Find the execution line
                if (cleanLine.Contains("goodbyedpi.exe", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract everything after the executable name
                    int index = cleanLine.IndexOf("goodbyedpi.exe", StringComparison.OrdinalIgnoreCase);
                    string args = cleanLine[(index + "goodbyedpi.exe".Length)..].Trim();

                    return args;
                }
            }
        }
        catch
        {
            return "-9"; // Default safe fallback
        }

        return "";
    }

    // --- HELPERS & UI UPDATES ---

    private static AppConfig ReadConfig()
    {
        if (File.Exists(AppConfigPath))
        {
            try
            {
                var json = File.ReadAllText(AppConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch { /* Ignore read errors, use defaults */ }
        }
        return new AppConfig();
    }

    /// <summary>
    /// Updates the UI state and triggers animations based on whether the engine is running.
    /// </summary>
    private void UpdateState(bool isRunning)
    {
        IsRunning = isRunning;

        if (BtnPower.IsChecked != isRunning)
            BtnPower.IsChecked = isRunning;

        // Animations
        DoubleAnimation fadeOut = new(0, TimeSpan.FromSeconds(0.3));
        DoubleAnimation fadeIn = new(1, TimeSpan.FromSeconds(0.3));

        if (isRunning)
        {
            // Disable settings while running
            BtnSettings.BeginAnimation(OpacityProperty, fadeOut);
            BtnSettings.IsHitTestVisible = false;

            // Show status panel
            PnlStatus.BeginAnimation(OpacityProperty, fadeIn);
        }
        else
        {
            // Enable settings
            BtnSettings.BeginAnimation(OpacityProperty, fadeIn);
            BtnSettings.IsHitTestVisible = true;

            // Hide status panel
            PnlStatus.BeginAnimation(OpacityProperty, fadeOut);
        }

        EngineStateChanged?.Invoke(this, isRunning);
    }
}