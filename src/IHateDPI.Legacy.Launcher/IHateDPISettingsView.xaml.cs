using IHateDPI.Legacy.Launcher.Abstractions;
using IHateDPI.Legacy.Launcher.Models;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace IHateDPI.Legacy.Launcher;

/// <summary>
/// Interaction logic for configuring the core parameters of the IHateDPI engine.
/// <para>
/// Handles the loading and saving of the <c>engineConfig.json</c> file, mapping UI controls 
/// to the <see cref="EngineConfig"/> model.
/// </para>
/// </summary>
public partial class IHateDPISettingsView : UserControl, ISaveable
{
    private static string BaseDir => Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;

    // Path to the specific config file for the IHateDPI engine sub-process.
    private static string ConfigFilePath => Path.Combine(
            BaseDir,
            "Engines",
            "IHateDPI",
            "engineConfig.json");

    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

    public IHateDPISettingsView()
    {
        InitializeComponent();
        LoadSettings();
    }

    /// <summary>
    /// Reads the existing configuration from disk and populates the UI controls.
    /// <para>Falls back to default values if the file is missing or corrupt.</para>
    /// </summary>
    private void LoadSettings()
    {
        var config = new EngineConfig();
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                config = JsonSerializer.Deserialize<EngineConfig>(json) ?? new EngineConfig();
            }
            catch
            {
                // Silently swallow errors here; defaults will be used, which is safe for a settings view.
            }
        }

        // --- General Settings ---
        ChkDoH.IsChecked = config.IsDoHEnabled;
        TxtDohUrl.Text = config.DohProviderUrl;
        ChkBlockQuic.IsChecked = config.BlockQuic;
        TxtMaxPayload.Text = config.MaxPayloadSize.ToString();

        // --- Fragmentation Strategy ---
        TxtFragHttp.Text = config.FragmentHttp.ToString();
        TxtFragHttps.Text = config.FragmentHttps.ToString();
        ChkAutoSplitSni.IsChecked = config.AutoSplitSni; // NEW
        ChkReverse.IsChecked = config.ReverseFragmentation;

        // --- Buffer Poisoning (NEW) ---
        ChkBufferPoisoning.IsChecked = config.BufferPoisoning;
        TxtJunkSize.Text = config.JunkPacketSize.ToString();
        TxtJunkCount.Text = config.JunkPacketCount.ToString();
        TxtJunkTTL.Text = config.JunkPacketTTL.ToString();
        ChkJunkBadSum.IsChecked = config.JunkPacketBadChecksum;
        ChkJunkBadSeq.IsChecked = config.JunkPacketBadSequence;

        // --- Header Manipulation ---
        ChkMixHost.IsChecked = config.MixHost;
        ChkHostNoSpace.IsChecked = config.HostNoSpace;
        ChkAddSpace.IsChecked = config.AdditionalSpace;

        // --- Fake Packet / Evasion Settings ---
        TxtFakeTTL.Text = config.FakePacketTTL.ToString();
        ChkBadSeq.IsChecked = config.BadSequence;
        ChkBadSum.IsChecked = config.BadCheckSum;
        TxtFakeCount.Text = config.FakeRequestResendCount.ToString();
    }

    /// <summary>
    /// Collects data from UI controls, serializes it to JSON, and writes it to the configuration file.
    /// </summary>
    public void Save()
    {
        var config = new EngineConfig
        {
            // General
            IsDoHEnabled = ChkDoH.IsChecked == true,
            DohProviderUrl = TxtDohUrl.Text,
            BlockQuic = ChkBlockQuic.IsChecked == true,
            MaxPayloadSize = ParseSafeInt(TxtMaxPayload.Text, 1200),

            // Fragmentation
            FragmentHttp = ParseSafeInt(TxtFragHttp.Text, 0),
            FragmentHttps = ParseSafeInt(TxtFragHttps.Text, 2),
            AutoSplitSni = ChkAutoSplitSni.IsChecked == true, // NEW
            ReverseFragmentation = ChkReverse.IsChecked == true,

            // Buffer Poisoning (NEW)
            BufferPoisoning = ChkBufferPoisoning.IsChecked == true,
            JunkPacketSize = ParseSafeInt(TxtJunkSize.Text, 1),
            JunkPacketCount = (ushort)ParseSafeInt(TxtJunkCount.Text, 1),
            JunkPacketTTL = ParseSafeInt(TxtJunkTTL.Text, 5),
            JunkPacketBadChecksum = ChkJunkBadSum.IsChecked == true,
            JunkPacketBadSequence = ChkJunkBadSeq.IsChecked == true,

            // Headers
            MixHost = ChkMixHost.IsChecked == true,
            HostNoSpace = ChkHostNoSpace.IsChecked == true,
            AdditionalSpace = ChkAddSpace.IsChecked == true,

            // Fake Request
            FakePacketTTL = ParseSafeInt(TxtFakeTTL.Text, 5),
            BadSequence = ChkBadSeq.IsChecked == true,
            BadCheckSum = ChkBadSum.IsChecked == true,
            FakeRequestResendCount = ParseSafeInt(TxtFakeCount.Text, 1),
        };

        try
        {
            // Ensure directory exists
            string? dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(config, jsonSerializerOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Helper method to safely parse integer values from text boxes.
    /// </summary>
    /// <param name="text">The string input.</param>
    /// <param name="defaultValue">The value to return if parsing fails.</param>
    /// <returns>The parsed integer or the default value.</returns>
    private static int ParseSafeInt(string text, int defaultValue) =>
        int.TryParse(text, out int val) ? val : defaultValue;
}