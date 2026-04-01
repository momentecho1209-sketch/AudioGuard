using System.Drawing;
using AudioGuard.Models;
using AudioGuard.Services;
using Forms = System.Windows.Forms;

namespace AudioGuard;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _trayIcon;
    private AudioSessionMonitor? _monitor;
    private AppSettings _settings = null!;
    private MainWindow? _settingsWindow;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = SettingsService.Load();
        SetupTrayIcon();
        StartMonitoring();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Text = "AudioGuard - オーディオ音量ガード",
            Visible = true
        };

        // アイコンをリソースから読み込み
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/app.ico", UriKind.Absolute);
            var stream = GetResourceStream(uri)?.Stream;
            if (stream != null)
                _trayIcon.Icon = new Icon(stream);
        }
        catch
        {
            _trayIcon.Icon = SystemIcons.Shield;
        }

        // コンテキストメニュー
        var menu = new Forms.ContextMenuStrip();

        var statusItem = new Forms.ToolStripMenuItem("監視中...");
        statusItem.Enabled = false;
        menu.Items.Add(statusItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var toggleItem = new Forms.ToolStripMenuItem("監視を一時停止");
        toggleItem.Click += (_, _) =>
        {
            _settings.MonitoringEnabled = !_settings.MonitoringEnabled;
            toggleItem.Text = _settings.MonitoringEnabled ? "監視を一時停止" : "監視を再開";
            statusItem.Text = _settings.MonitoringEnabled ? "監視中..." : "一時停止中";
            _trayIcon!.Text = _settings.MonitoringEnabled
                ? "AudioGuard - 監視中"
                : "AudioGuard - 一時停止";
        };
        menu.Items.Add(toggleItem);

        var settingsItem = new Forms.ToolStripMenuItem("設定...");
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowSettings();
    }

    private void StartMonitoring()
    {
        try
        {
            _monitor = new AudioSessionMonitor(_settings);
            _monitor.SessionApplied += OnSessionApplied;
            _monitor.Start();
        }
        catch (Exception ex)
        {
            _trayIcon?.ShowBalloonTip(3000, "AudioGuard エラー",
                $"オーディオ監視の開始に失敗: {ex.Message}",
                Forms.ToolTipIcon.Error);
        }
    }

    private void OnSessionApplied(string exeName, float volume, bool muted)
    {
        var msg = muted
            ? $"{exeName} をミュートしました"
            : $"{exeName} の音量を {volume:0}% に設定しました";

        _trayIcon?.ShowBalloonTip(2000, "AudioGuard", msg, Forms.ToolTipIcon.Info);
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new MainWindow(_settings, OnSettingsSaved);
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnSettingsSaved()
    {
        // 設定変更時にモニターの参照を更新 (同じオブジェクトなので自動反映)
        if (_monitor != null)
            _monitor.Settings = _settings;
    }

    private void ExitApp()
    {
        _monitor?.Dispose();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        _settingsWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _monitor?.Dispose();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnExit(e);
    }
}
