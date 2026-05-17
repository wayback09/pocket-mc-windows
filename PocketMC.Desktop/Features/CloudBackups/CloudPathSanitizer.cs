using System;
using System.Text.RegularExpressions;

namespace PocketMC.Desktop.Features.CloudBackups;

public static class CloudPathSanitizer
{
    private static readonly Regex InvalidCharsRegex = new Regex(@"[<>:""/\\|?*]", RegexOptions.Compiled);
    
    public static string SanitizeFolderName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Unknown";
        string sanitized = InvalidCharsRegex.Replace(input, "-");
        return sanitized.Trim();
    }
}
