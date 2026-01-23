using IHateDPI.Legacy.Launcher.Abstractions;
using IHateDPI.Legacy.Launcher.Models;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace IHateDPI.Legacy.Launcher;

/// <summary>
/// Interaction logic for the centralized settings panel.
/// <para>
/// Manages navigation between different engine configuration views (IHateDPI vs GoodbyeDPI)
/// and handles the persistence of global launcher settings.
/// </para>
/// </summary>
public partial class SettingsView : UserControl
{
    private static string BaseDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    /// <summary>
    /// Event triggered when the settings panel should be closed (Save or Cancel).
    /// </summary>
    public event EventHandler? OnCloseRequested;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

    private static string AppConfigPath => Path.Combine(BaseDir, "appConfig.json");
    private const string GoodbyeDPIExeName = "goodbyedpi.exe";

    public SettingsView()
    {
        InitializeComponent();
        InitializeNavigation();
    }

    /// <summary>
    /// Checks for the presence of external engines and configures the navigation UI accordingly.
    /// <para>
    /// If GoodbyeDPI is missing, the engine selection header is hidden, and the view defaults to IHateDPI.
    /// </para>
    /// </summary>
    private void InitializeNavigation()
    {
        string goodbyePath = Path.Combine(BaseDir, "Engines", "GoodbyeDPI", "x86_64", GoodbyeDPIExeName);

        bool isGoodbyeDpiAvailable = File.Exists(goodbyePath);

        if (!isGoodbyeDpiAvailable)
        {
            HeaderPanel.Visibility = Visibility.Collapsed;
            LoadView(EngineType.IHateDPI);
        }
        else
        {
            HeaderPanel.Visibility = Visibility.Visible;
            LoadLauncherSettings();
        }
    }

    /// <summary>
    /// Loads the saved launcher configuration and populates the engine selection dropdown.
    /// </summary>
    private void LoadLauncherSettings()
    {
        CmbEngineSelect.ItemsSource = Enum.GetValues<EngineType>();

        var currentEngine = EngineType.IHateDPI;
        if (File.Exists(AppConfigPath))
        {
            try
            {
                var json = File.ReadAllText(AppConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null) currentEngine = config.SelectedEngine;
            }
            catch { /* Ignore read errors */ }
        }
        CmbEngineSelect.SelectedItem = currentEngine;
    }

    private void CmbEngineSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbEngineSelect.SelectedItem is EngineType selectedEngine)
        {
            LoadView(selectedEngine);
        }
    }

    /// <summary>
    /// Dynamically injects the specific settings user control for the selected engine.
    /// </summary>
    private void LoadView(EngineType engine)
    {
        UserControl? view = null;
        switch (engine)
        {
            case EngineType.IHateDPI:
                view = new IHateDPISettingsView();
                break;
            case EngineType.GoodbyeDPI:
                view = new GoodbyeDPISettingsView();
                break;
        }
        ActiveSettingsContainer.Content = view;
    }

    /// <summary>
    /// Persists the selected engine type to the launcher configuration file.
    /// </summary>
    private void SaveLauncherEngineSelection(EngineType engine)
    {
        if (HeaderPanel.Visibility != Visibility.Visible) return;

        var config = new AppConfig { SelectedEngine = engine };

        try
        {
            // Preserve existing settings (e.g., selected script) to avoid data loss.
            if (File.Exists(AppConfigPath))
            {
                var existingJson = File.ReadAllText(AppConfigPath);
                var existingConfig = JsonSerializer.Deserialize<AppConfig>(existingJson);
                if (existingConfig != null)
                {
                    config.SelectedGoodbyeDpiScript = existingConfig.SelectedGoodbyeDpiScript;
                }
            }

            File.WriteAllText(AppConfigPath, JsonSerializer.Serialize(config, jsonSerializerOptions));
        }
        catch { }
    }

    /// <summary>
    /// Orchestrates the save operation for both the active child view and the global launcher settings.
    /// </summary>
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // 1. Delegate save responsibility to the active child view (Polymorphism via ISaveable).
        if (ActiveSettingsContainer.Content is ISaveable activePage)
        {
            activePage.Save();
        }

        // 2. Save the global engine selection.
        if (CmbEngineSelect.SelectedItem is EngineType selectedEngine)
        {
            SaveLauncherEngineSelection(selectedEngine);
        }

        // 3. Signal the main window to close the settings panel.
        OnCloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        // Discard changes and close.
        OnCloseRequested?.Invoke(this, EventArgs.Empty);
    }
}