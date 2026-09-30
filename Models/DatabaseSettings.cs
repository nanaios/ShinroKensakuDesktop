using System.IO;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace ShinroKensakuDesktop.Models;

public static class DatabaseSettings
{
    public const string EnvironmentVariable = "SHINRO_DB_CONNECTION_STRING";
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KST-DeCS", "database.bin");
    public static bool UsesEnvironment => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable));
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("KST-DeCS/database/v1");
    private static int version;
    public static int Version => Volatile.Read(ref version);

    public static string LoadConnectionString()
    {
        string? configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
            return new MySqlConnectionStringBuilder(configured).ConnectionString;
        return LoadLocalConnectionString();
    }

    public static string LoadLocalConnectionString() => ReadProtected(SettingsPath);

    public static string ReadProtected(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException("設定画面でデータベースの接続を設定してください。");
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
        try { return new MySqlConnectionStringBuilder(Encoding.UTF8.GetString(plain)).ConnectionString; }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public static void SaveConnectionString(string connectionString)
    {
        WriteProtected(SettingsPath, connectionString);
        Interlocked.Increment(ref version);
    }

    public static void WriteProtected(string path, string connectionString)
    {
        string normalized = new MySqlConnectionStringBuilder(connectionString).ConnectionString;
        byte[] plain = Encoding.UTF8.GetBytes(normalized);
        try
        {
            byte[] encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, encrypted);
                File.Move(temporary, path, overwrite: true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
}
