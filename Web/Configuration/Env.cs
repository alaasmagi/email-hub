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
        var value = GetRequired(name);
        var values = value
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (values.Length == 0)
        {
            throw new InvalidOperationException($"{name} must be configured in .env.");
        }

        return values;
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
