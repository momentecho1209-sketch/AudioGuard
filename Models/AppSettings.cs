using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AudioGuard.Models;

public class AppSettings
{
    /// <summary>新規セッションに適用する既定音量 (0-100)</summary>
    public int DefaultVolume { get; set; } = 30;

    /// <summary>既定動作がミュートかどうか</summary>
    public bool MuteByDefault { get; set; } = false;

    /// <summary>音量調整を適用しない exe 名リスト (小文字)</summary>
    public List<string> ExcludedProcesses { get; set; } = new()
    {
        "explorer.exe"
    };

    /// <summary>既に音量を適用済みの exe 名リスト (小文字)</summary>
    public List<string> AppliedProcesses { get; set; } = new();

    /// <summary>Windows 起動時に自動実行するか</summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>監視が有効か</summary>
    public bool MonitoringEnabled { get; set; } = true;
}

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioGuard");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        File.WriteAllText(SettingsPath, json);
    }

    private const string AutoStartKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AudioGuard";

    public static void SetAutoStart(bool enable)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AutoStartKey, true);
        if (key == null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath ?? "";
            if (!string.IsNullOrEmpty(exePath))
                key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
