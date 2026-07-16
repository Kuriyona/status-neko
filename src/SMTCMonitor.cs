using Windows.Media.Control;

namespace StatusNeko;

public sealed class SMTCMonitor
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private CancellationTokenSource _cts = new();

    public event Action<MediaInfo>? OnMediaUpdate;
    public event Action? OnMediaStopped;

    public async Task StartAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;

        _currentSession = _manager.GetCurrentSession();
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            await EmitCurrentMedia();
        }
    }

    public void Stop()
    {
        _cts.Cancel();

        if (_currentSession != null)
        {
            try { _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged; }
            catch { }
            _currentSession = null;
        }

        if (_manager != null)
        {
            try { _manager.CurrentSessionChanged -= OnCurrentSessionChanged; }
            catch { }
            _manager = null;
        }
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        if (_currentSession != null)
        {
            try { _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged; }
            catch { }
        }

        _currentSession = sender.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            await EmitCurrentMedia();
        }
        else
        {
            OnMediaStopped?.Invoke();
        }
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await EmitSessionMedia(sender);
    }

    private async Task EmitCurrentMedia()
    {
        await EmitSessionMedia(_currentSession);
    }

    private async Task EmitSessionMedia(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session == null)
        {
            OnMediaStopped?.Invoke();
            return;
        }

        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            if (props != null)
            {
                OnMediaUpdate?.Invoke(new MediaInfo(
                    Title: props.Title ?? "",
                    Artist: props.Artist ?? "",
                    Album: props.AlbumTitle ?? "",
                    SourceApp: session.SourceAppUserModelId ?? ""
                ));
            }
        }
        catch
        {
            OnMediaStopped?.Invoke();
        }
    }
}
