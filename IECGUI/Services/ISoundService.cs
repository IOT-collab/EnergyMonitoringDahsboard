namespace IECGUI.Services;

public interface ISoundService
{
    void PlayClick();
    void PlayNotification();
    void PlayAlarm();
    void PlaySuccess();
    void PlayError();
    void StopAlarm();
    void StartBackgroundMusic();
    void StopBackgroundMusic();
    void SetVolume(double volume);
}
