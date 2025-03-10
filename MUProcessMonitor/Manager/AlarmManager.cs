using MUProcessMonitor.Models;

namespace MUProcessMonitor.Manager;

public class AlarmManager
{
    private static readonly Lazy<AlarmManager> _instance = new(() => new AlarmManager());
    public static AlarmManager Instance => _instance.Value;

    private readonly AlarmService _alarmService = new();
    private bool isAlarmPlaying = false;

    public void StartAlarm()
    {
        if (!Configuration.UseAlarm || isAlarmPlaying) return;

        isAlarmPlaying = true;

        new Thread(() =>
        {
            while (isAlarmPlaying)
            {
                _alarmService.Start().Wait();
            }
        })
        { IsBackground = true }.Start();
    }

    public void StopAlarm()
    {
        isAlarmPlaying = false;
        _alarmService.Stop();
    }

    public void PlaySelectedSound(string selectedSound = "None")
    {
        new Thread(() =>
        {
            if (selectedSound != "None")
                _alarmService.Start(selectedSound).Wait();
            else
                _alarmService.Stop();
        })
        { IsBackground = true }.Start();
    }
}

