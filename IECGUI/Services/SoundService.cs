using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;

namespace IECGUI.Services;

/// <summary>
/// Centralized audio playback for UI actions and runtime notifications.
/// Sound files are optional and are loaded from Assets/Sounds beside the executable.
/// </summary>
public sealed class SoundService : ISoundService, IDisposable
{
    private readonly MediaPlayer _backgroundPlayer = new();
    private readonly List<MediaPlayer> _effectPlayers = new();
    private readonly List<MediaPlayer> _alarmPlayers = new();
    private readonly string _soundDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
    private double _volume = 1.0;
    private bool _disposed;
    private bool _backgroundLoaded;

    public SoundService()
    {
        _backgroundPlayer.MediaEnded += BackgroundPlayer_MediaEnded;
        _backgroundPlayer.MediaFailed += BackgroundPlayer_MediaFailed;
    }

    public void PlayClick() => PlayEffect("click", SystemSounds.Beep.Play);

    public void PlayNotification() => PlayEffect("notification", SystemSounds.Asterisk.Play);

    public void PlayAlarm() => PlayEffect("alarm", SystemSounds.Exclamation.Play, true);

    public void PlaySuccess() => PlayEffect("success", SystemSounds.Asterisk.Play);

    public void PlayError() => PlayEffect("error", SystemSounds.Hand.Play);

    public void StopAlarm()
    {
        RunOnUiThread(() =>
        {
            foreach (var player in _alarmPlayers.ToArray())
            {
                _effectPlayers.Remove(player);
                _alarmPlayers.Remove(player);
                player.Stop();
                player.Close();
            }

            _alarmPlayers.Clear();
        });
    }

    public void StartBackgroundMusic()
    {
        RunOnUiThread(() =>
        {
            _backgroundLoaded = false;
            _backgroundPlayer.Stop();
            var path = FindSoundFile("background");
            if (path == null) return;

            _backgroundPlayer.Open(new Uri(path, UriKind.Absolute));
            _backgroundPlayer.Volume = _volume * 0.2;
            _backgroundLoaded = true;
            _backgroundPlayer.Play();
        });
    }

    public void StopBackgroundMusic()
    {
        RunOnUiThread(() =>
        {
            _backgroundLoaded = false;
            _backgroundPlayer.Stop();
            _backgroundPlayer.Close();
        });
    }

    public void SetVolume(double volume)
    {
        _volume = Math.Clamp(volume, 0, 1);
        RunOnUiThread(() =>
        {
            _backgroundPlayer.Volume = _volume * 0.2;
            foreach (var player in _effectPlayers)
                player.Volume = _volume;
        });
    }

    private void PlayEffect(string soundName, Action fallback, bool isAlarm = false)
    {
        RunOnUiThread(() =>
        {
            var path = FindSoundFile(soundName);
            if (path == null)
            {
                fallback();
                return;
            }

            var player = new MediaPlayer { Volume = _volume };
            _effectPlayers.Add(player);
            if (isAlarm) _alarmPlayers.Add(player);

            EventHandler? opened = null;
            EventHandler? ended = null;
            EventHandler<ExceptionEventArgs>? failed = null;

            void Cleanup()
            {
                if (opened != null) player.MediaOpened -= opened;
                if (ended != null) player.MediaEnded -= ended;
                if (failed != null) player.MediaFailed -= failed;
                _effectPlayers.Remove(player);
                if (isAlarm) _alarmPlayers.Remove(player);
                player.Close();
            }

            opened = (_, _) => player.Play();
            ended = (_, _) => Cleanup();
            failed = (_, _) =>
            {
                var shouldFallback = _effectPlayers.Contains(player);
                Cleanup();
                if (shouldFallback) fallback();
            };

            player.MediaOpened += opened;
            player.MediaEnded += ended;
            player.MediaFailed += failed;
            player.Open(new Uri(path, UriKind.Absolute));
        });
    }

    private string? FindSoundFile(string soundName)
    {
        foreach (var extension in new[] { ".wav", ".mp3", ".wma" })
        {
            var path = Path.Combine(_soundDirectory, soundName + extension);
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private void BackgroundPlayer_MediaEnded(object? sender, EventArgs e)
    {
        if (!_disposed && _backgroundLoaded)
        {
            _backgroundPlayer.Position = TimeSpan.Zero;
            _backgroundPlayer.Play();
        }
    }

    private void BackgroundPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
    {
        _backgroundLoaded = false;
    }

    private void RunOnUiThread(Action action)
    {
        if (_disposed) return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted)
        {
            _backgroundPlayer.Close();
            return;
        }

        if (dispatcher.CheckAccess()) DisposeCore();
        else dispatcher.BeginInvoke(DisposeCore);
    }

    private void DisposeCore()
    {
        _backgroundLoaded = false;
        _backgroundPlayer.Stop();
        _backgroundPlayer.Close();
        foreach (var player in _effectPlayers)
            player.Close();
        _effectPlayers.Clear();
        _alarmPlayers.Clear();
    }
}
