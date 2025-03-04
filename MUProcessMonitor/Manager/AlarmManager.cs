using MUProcessMonitor.Models;

namespace MUProcessMonitor.Manager;

public class AlarmManager
{
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

    public async Task PlaySelectedSound(string selectedSound = "None")
    {
        if (selectedSound != "None")
            await _alarmService.Start(selectedSound);
        else
            _alarmService.Stop();
    }
}

