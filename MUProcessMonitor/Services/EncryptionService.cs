using System.Security.Cryptography;
using System.Text;

namespace MUProcessMonitor.Services;

public static class EncryptionService
{
    private static readonly string EncryptionKey = "6BvQ9s8eJ2dV1kX3mN7aP4yR0tF5wZ8b";

    public static byte[] Encrypt(string plainText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            sw.Write(plainText);
            sw.Flush();
            cs.FlushFinalBlock();

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Encryption Error: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    public static string Decrypt(byte[] cipherText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decryption Error: {ex.Message}");
            return string.Empty;
        }
    }
}
