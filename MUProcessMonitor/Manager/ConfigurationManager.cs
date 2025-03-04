using global::MUProcessMonitor.Services;
using MUProcessMonitor.Models;

namespace MUProcessMonitor.Manager;

public class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance = new(() => new ConfigurationManager());
    public static ConfigurationManager Instance => _instance.Value;


    private readonly string configFilePath = "config.dat";

    public void LoadConfiguration()
    {
        if (File.Exists(configFilePath))
        {
            var encryptedData = File.ReadAllBytes(configFilePath);
            var decryptedData = EncryptionService.Decrypt(encryptedData);
            var configParts = decryptedData.Split(';');

            if (configParts.Length == 5)
            {
                Configuration.BotToken = configParts[0];
                Configuration.ChatId = configParts[1];
                Configuration.UseAlarm = bool.Parse(configParts[2]);
                Configuration.AlarmSound = configParts[3];
                Configuration.ThreadSleepTime = int.Parse(configParts[4]);
            }
        }
    }

    public void SaveConfiguration()
    {
        var configData = $"{Configuration.BotToken};{Configuration.ChatId};{Configuration.UseAlarm};{Configuration.AlarmSound};{Configuration.ThreadSleepTime}";
        var encryptedData = EncryptionService.Encrypt(configData);

        if (encryptedData.Length > 0)
            File.WriteAllBytes(configFilePath, encryptedData);
    }
}

