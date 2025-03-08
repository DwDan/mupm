using MUProcessMonitor.Models;
using NAudio.Wave;

public class AlarmService
{
    private IWavePlayer? waveOut;
    private AudioFileReader? audioFile;
    private readonly object lockObj = new();

    public async Task Start(string? alarmSound = null)
    {
        lock (lockObj)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resourcePath = Path.Combine(basePath, "Resources", "Alert", alarmSound ?? Configuration.AlarmSound);

            Stop();
            waveOut = new WaveOutEvent();
            audioFile = new AudioFileReader(resourcePath);
            waveOut.Init(audioFile);
            waveOut.Play();
        }

        if (audioFile != null)
        {
            await Task.Delay((int)(audioFile.TotalTime.TotalMilliseconds));
        }
    }

    public void Stop()
    {
        lock (lockObj)
        {
            waveOut?.Stop();
            audioFile?.Dispose();
            waveOut?.Dispose();
            audioFile = null;
            waveOut = null;
        }
    }
}
