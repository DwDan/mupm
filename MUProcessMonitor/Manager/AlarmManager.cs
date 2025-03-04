using MUProcessMonitor.Models;

namespace MUProcessMonitor.Manager;

public class AlarmManager
{
    private readonly AlarmService alarmService = new();
    private bool isAlarmPlaying = false;

    public void StartAlarm()
    {
        if (!Configuration.UseAlarm || isAlarmPlaying) return;

        isAlarmPlaying = true;
        new Thread(() =>
        {
            while (isAlarmPlaying)
            {
                alarmService.Start().Wait();
            }
        })
        { IsBackground = true }.Start();
    }

    public void StopAlarm()
    {
        isAlarmPlaying = false;
        alarmService.Stop();
    }
}

