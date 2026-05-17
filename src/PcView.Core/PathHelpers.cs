using System.Text.RegularExpressions;

namespace PcView.Core;

public static class PathHelpers
{
    private static readonly Regex ExePathPattern = new(
        "^[A-Za-z]:\\\\.*?\\.exe",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? ExtractExecutablePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var expanded = Environment.ExpandEnvironmentVariables(value.Trim());
        if (expanded.StartsWith('"'))
        {
            var end = expanded.IndexOf('"', 1);
            if (end > 1)
            {
                return expanded[1..end];
            }
        }

        var match = ExePathPattern.Match(expanded);
        if (match.Success)
        {
            return match.Value;
        }

        if (expanded.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return expanded;
        }

        return expanded.Split(',')[0].Trim();
    }

    public static string StableId(string value)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)).TrimEnd('=');
    }
}
