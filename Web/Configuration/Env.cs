using System.Globalization;

namespace Web.Configuration;

public static class Env
{
    public static void LoadFile(string contentRootPath)
    {
        if (TryLoadFileFromDirectory(contentRootPath))
        {
            return;
        }

        TryLoadFileFromDirectory(Directory.GetCurrentDirectory());
    }

    public static string? Get(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    public static string GetRequired(string name)
    {
        var value = Get(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} must be configured in .env.");
        }

        return value;
    }

    public static string[] GetRequiredList(string name)
    {
        var values = GetList(name);
        if (values is null || values.Length == 0)
        {
            throw new InvalidOperationException($"{name} must be configured in .env.");
        }

        return values;
    }

    public static int GetInt(string name, int fallback)
    {
        var value = Get(name);
        return int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    public static ushort GetUShort(string name, ushort fallback)
    {
        var value = Get(name);
        return ushort.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    public static double GetDouble(string name, double fallback)
    {
        var value = Get(name);
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : fallback;
    }

    public static bool GetBool(string name, bool fallback)
    {
        var value = Get(name);
        return bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    public static void EnsureDefault(string name, string fallback)
    {
        var value = Get(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            Environment.SetEnvironmentVariable(name, fallback);
        }
    }

    private static string[]? GetList(string name)
    {
        var value = Get(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var values = value
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return values.Length == 0
            ? null
            : values;
    }

    private static bool TryLoadFileFromDirectory(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            var envPath = Path.Combine(directory.FullName, ".env");
            if (File.Exists(envPath))
            {
                LoadFilePath(envPath);
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    private static void LoadFilePath(string envPath)
    {
        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
