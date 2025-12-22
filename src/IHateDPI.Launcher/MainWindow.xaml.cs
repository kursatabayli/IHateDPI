using System.Windows;
using System.Windows.Input;

namespace IHateDPI.Launcher;

/// <summary>
/// Interaction logic for the main application window.
/// Manages the window lifecycle, system tray integration, and UI updates based on the engine state.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MyMainView.EngineStateChanged += OnEngineStateChanged;
    }

    /// <summary>
    /// Handles state changes from the DPI engine to update UI elements.
    /// <para>Updates the tray icon tooltip and the context menu text (Start/Stop) dynamically.</para>
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="isRunning">Indicates whether the engine is currently active.</param>
    private void OnEngineStateChanged(object? sender, bool isRunning)
    {
        if (isRunning)
        {
            MenuStartStop.Header = "Durdur";
            TrayIcon.ToolTipText = "IHateDPI: Aktif ⚡";
        }
        else
        {
            MenuStartStop.Header = "Başlat";
            TrayIcon.ToolTipText = "IHateDPI: Beklemede";
        }
    }

    // --- WINDOW MANAGEMENT ---

    /// <summary>
    /// Enables window dragging functionality when clicking on the custom title bar.
    /// </summary>
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    /// <summary>
    /// Minimizes the window to the taskbar (or system tray, depending on state logic).
    /// </summary>
    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

    /// <summary>
    /// Initiates the full application shutdown process.
    /// </summary>
    private void BtnClose_Click(object sender, RoutedEventArgs e) => FullExit();

    // --- SYSTEM TRAY LOGIC ---

    /// <summary>
    /// Overrides the state change behavior to minimize the application to the system tray.
    /// <para>Hides the window from the taskbar when minimized.</para>
    /// </summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            this.Hide();
            // Optional: Show a balloon tip notification here if needed.
            // TrayIcon.ShowBalloonTip("IHateDPI", "Running in background...", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
        base.OnStateChanged(e);
    }

    /// <summary>
    /// Restores the application window when the tray icon is double-clicked.
    /// </summary>
    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e) => ShowApp();

    /// <summary>
    /// Handles the "Show" menu item click from the tray context menu.
    /// </summary>
    private void MenuShow_Click(object sender, RoutedEventArgs e) => ShowApp();

    /// <summary>
    /// Toggles the DPI engine state (Start/Stop) directly from the tray context menu.
    /// </summary>
    private void MenuStartStop_Click(object sender, RoutedEventArgs e)
    {
        if (MyMainView.IsRunning)
            MyMainView.StopEngine();
        else
            MyMainView.StartEngine();
    }

    /// <summary>
    /// Handles the "Exit" menu item click to shut down the application.
    /// </summary>
    private void MenuExit_Click(object sender, RoutedEventArgs e) => FullExit();

    // --- HELPERS ---

    /// <summary>
    /// Makes the main window visible, restores it to normal state, and brings it to the foreground.
    /// </summary>
    private void ShowApp()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    /// <summary>
    /// Performs a graceful shutdown of the application.
    /// <para>Stops the engine, unsubscribes from events, disposes the tray icon, and terminates the process.</para>
    /// </summary>
    private void FullExit()
    {
        MyMainView.EngineStateChanged -= OnEngineStateChanged;
        MyMainView.StopEngine();
        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }
}