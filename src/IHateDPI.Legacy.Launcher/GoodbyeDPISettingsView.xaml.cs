using IHateDPI.Legacy.Launcher.Abstractions;
using IHateDPI.Legacy.Launcher.Models;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace IHateDPI.Legacy.Launcher;

/// <summary>
/// Interaction logic for configuring the GoodbyeDPI engine parameters.
/// <para>
/// Responsible for scanning available startup scripts, filtering out service installers, 
/// and persisting the user's script selection.
/// </para>
/// </summary>
public partial class GoodbyeDPISettingsView : UserControl, ISaveable
{
    private static string BaseDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
    private static string AppConfigPath => Path.Combine(BaseDir, "appConfig.json");
    private static string GoodbyeDpiPath => Path.Combine(BaseDir, "Engines", "GoodbyeDPI");

    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// Represents a selectable batch script file found in the engine directory.
    /// </summary>
    public class ScriptItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
    }

    public GoodbyeDPISettingsView()
    {
        InitializeComponent();
        LoadScriptsAndConfig();
    }

    /// <summary>
    /// Scans the engine directory for executable batch scripts (.cmd), excludes service installation scripts, 
    /// and restores the previously selected script from the configuration.
    /// </summary>
    private void LoadScriptsAndConfig()
    {
        // 1. Scan and Filter Scripts
        if (Directory.Exists(GoodbyeDpiPath))
        {
            var cmdFiles = Directory.GetFiles(GoodbyeDpiPath, "*.cmd")
                                    .Select(path => new ScriptItem
                                    {
                                        Name = Path.GetFileName(path),
                                        FullPath = path
                                    })
                                    // Filter out service installation/removal scripts to prevent accidental system changes.
                                    .Where(item => !item.Name.StartsWith("service", StringComparison.OrdinalIgnoreCase))
                                    .OrderBy(x => x.Name)
                                    .ToList();

            CmbScripts.ItemsSource = cmdFiles;
        }

        // 2. Load Existing Configuration
        if (File.Exists(AppConfigPath))
        {
            try
            {
                var json = File.ReadAllText(AppConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);

                if (config != null && !string.IsNullOrEmpty(config.SelectedGoodbyeDpiScript))
                {
                    CmbScripts.SelectedValue = config.SelectedGoodbyeDpiScript;
                }
            }
            catch { /* Ignore read errors */ }
        }

        // 3. Set Default if None Selected
        if (CmbScripts.SelectedIndex == -1 && CmbScripts.Items.Count > 0)
        {
            CmbScripts.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Persists the selected script preference to the application configuration file.
    /// </summary>
    public void Save()
    {
        var selectedScript = CmbScripts.SelectedValue as string;

        if (string.IsNullOrEmpty(selectedScript))
        {
            MessageBox.Show("Please select a startup script.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Load existing config to avoid overwriting other settings (like Engine Selection).
        var config = new AppConfig();
        if (File.Exists(AppConfigPath))
        {
            try
            {
                var json = File.ReadAllText(AppConfigPath);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch { }
        }

        // Update specific setting
        config.SelectedGoodbyeDpiScript = selectedScript;

        try
        {
            File.WriteAllText(AppConfigPath, JsonSerializer.Serialize(config, jsonSerializerOptions));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}