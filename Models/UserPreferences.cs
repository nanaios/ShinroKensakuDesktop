using System.IO;
using System.Text.Json;
using Wpf.Ui.Appearance;

namespace ShinroKensakuDesktop.Models;

public static class UserPreferences
{
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KST-DeCS", "preferences.json");

    public static ApplicationTheme LoadTheme(string? path = null)
    {
        try
        {
            var value = JsonSerializer.Deserialize<Preference>(File.ReadAllText(path ?? SettingsPath));
            return value != null && Enum.IsDefined(value.Theme) ? value.Theme : ApplicationTheme.Unknown;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return ApplicationTheme.Unknown; }
    }

    public static void SaveTheme(ApplicationTheme theme, string? path = null)
    {
        string target = path ?? SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new Preference(theme)));
            File.Move(temporary, target, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed record Preference(ApplicationTheme Theme);
}
