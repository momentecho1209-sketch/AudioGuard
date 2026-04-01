using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioGuard.CoreAudio;
using AudioGuard.Models;

namespace AudioGuard.Services;

/// <summary>
/// Core Audio API を使って新しいオーディオセッションを監視し、
/// 未登録のプロセスの音量を自動調整する。
/// </summary>
public sealed class AudioSessionMonitor : IDisposable
{
    private static readonly Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;

    private IMMDeviceEnumerator? _enumerator;
    private IAudioSessionManager2? _sessionManager;
    private AudioSessionCallback? _callback;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>新しいセッションが検出・適用されたときに発火</summary>
    public event Action<string, float, bool>? SessionApplied;

    public AppSettings Settings { get; set; }

    public AudioSessionMonitor(AppSettings settings)
    {
        Settings = settings;
    }

    /// <summary>監視を開始する</summary>
    public void Start()
    {
        _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();

        int hr = _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
        Marshal.ThrowExceptionForHR(hr);

        hr = device.Activate(IID_IAudioSessionManager2, (uint)CLSCTX.CLSCTX_ALL, IntPtr.Zero, out var obj);
        Marshal.ThrowExceptionForHR(hr);
        _sessionManager = (IAudioSessionManager2)obj;

        // 既存セッションをスキャン
        ScanExistingSessions();

        // 新規セッション通知を登録
        _callback = new AudioSessionCallback(OnNewSession);
        hr = _sessionManager.RegisterSessionNotification(_callback);
        Marshal.ThrowExceptionForHR(hr);
    }

    /// <summary>監視を停止する</summary>
    public void Stop()
    {
        if (_sessionManager != null && _callback != null)
        {
            try { _sessionManager.UnregisterSessionNotification(_callback); } catch { }
        }
        _callback = null;

        if (_sessionManager != null)
        {
            Marshal.ReleaseComObject(_sessionManager);
            _sessionManager = null;
        }
        if (_enumerator != null)
        {
            Marshal.ReleaseComObject(_enumerator);
            _enumerator = null;
        }
    }

    /// <summary>既存の全セッションを走査して未適用のものに音量設定</summary>
    private void ScanExistingSessions()
    {
        if (_sessionManager == null) return;

        int hr = _sessionManager.GetSessionEnumerator(out var sessionEnum);
        if (hr != 0) return;

        hr = sessionEnum.GetCount(out int count);
        if (hr != 0) return;

        for (int i = 0; i < count; i++)
        {
            hr = sessionEnum.GetSession(i, out var sessionCtl);
            if (hr != 0) continue;

            ProcessSession(sessionCtl);
        }
    }

    /// <summary>新規セッション作成時のコールバック</summary>
    private void OnNewSession(IAudioSessionControl newSession)
    {
        ProcessSession(newSession);
    }

    /// <summary>セッションを評価し、必要なら音量を適用</summary>
    private void ProcessSession(IAudioSessionControl sessionControl)
    {
        if (!Settings.MonitoringEnabled) return;

        try
        {
            var session2 = (IAudioSessionControl2)sessionControl;

            int hr = session2.GetProcessId(out uint pid);
            if (hr != 0 || pid == 0) return;

            string? exeName = GetProcessName(pid);
            if (string.IsNullOrEmpty(exeName)) return;

            string exeLower = exeName.ToLowerInvariant();

            lock (_lock)
            {
                // 除外リストに含まれている場合はスキップ
                if (Settings.ExcludedProcesses.Contains(exeLower)) return;

                // 既に適用済みならスキップ
                if (Settings.AppliedProcesses.Contains(exeLower)) return;

                // 音量を適用
                var volume = (ISimpleAudioVolume)sessionControl;
                float targetLevel = Settings.DefaultVolume / 100f;
                bool mute = Settings.MuteByDefault;

                if (mute)
                {
                    volume.SetMute(true, Guid.Empty);
                }
                else
                {
                    volume.SetMasterVolume(targetLevel, Guid.Empty);
                }

                // 適用済みリストに追加して保存
                Settings.AppliedProcesses.Add(exeLower);
                SettingsService.Save(Settings);

                SessionApplied?.Invoke(exeName, targetLevel * 100, mute);
            }
        }
        catch
        {
            // COMエラーやプロセスアクセス権限エラーは無視
        }
    }

    private static string? GetProcessName(uint pid)
    {
        try
        {
            var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName + ".exe";
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
