using System.Runtime.InteropServices;

namespace AudioGuard.CoreAudio;

/// <summary>
/// IAudioSessionNotification の実装。新しいオーディオセッション作成時にコールバックを受け取る。
/// </summary>
public class AudioSessionCallback : IAudioSessionNotification
{
    private readonly Action<IAudioSessionControl> _onSessionCreated;

    public AudioSessionCallback(Action<IAudioSessionControl> onSessionCreated)
    {
        _onSessionCreated = onSessionCreated;
    }

    public int OnSessionCreated(IAudioSessionControl newSession)
    {
        try
        {
            _onSessionCreated(newSession);
        }
        catch
        {
            // コールバック内の例外を飲み込む (COM境界)
        }
        return 0; // S_OK
    }
}
