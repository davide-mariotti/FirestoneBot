using System.Linq;
using System.Text.RegularExpressions;

namespace Firebot.Utilities;

public static class StringUtils
{
    /// <summary>"GuardianTrainingTask" -> "Guardian Training": strips the suffix, spaces out PascalCase.</summary>
    public static string Humanize(string input, string suffixToRemove = "Task")
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var cleaned = input.EndsWith(suffixToRemove)
            ? input.Substring(0, input.Length - suffixToRemove.Length)
            : input;

        return Regex.Replace(cleaned, @"(?<!^)(?=[A-Z])", " ").Trim();
    }

    /// <summary>All digits in the string read as one integer: "x5" -> 5, "Level 12" -> 12.</summary>
    public static bool TryParseIntFromString(string input, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out value);
    }
}
