namespace Plugin.Maui.FeatureFlags;

static class VersionComparer
{
    public static bool IsAtLeast(string? actual, string? minimum) =>
        string.IsNullOrWhiteSpace(minimum) || Compare(actual, minimum) >= 0;

    public static bool IsAtMost(string? actual, string? maximum) =>
        string.IsNullOrWhiteSpace(maximum) || Compare(actual, maximum) <= 0;

    public static bool EqualsVersion(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        Compare(left, right) == 0;

    public static int Compare(string? left, string? right)
    {
        var a = Parse(left);
        var b = Parse(right);
        var length = Math.Max(a.Length, b.Length);

        for (var i = 0; i < length; i++)
        {
            var leftPart = i < a.Length ? a[i] : 0;
            var rightPart = i < b.Length ? b[i] : 0;
            var cmp = leftPart.CompareTo(rightPart);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    static int[] Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var parts = new List<int>();
        foreach (var token in value.Split(['.', '-', '_', '+'], StringSplitOptions.RemoveEmptyEntries))
        {
            var digits = token;
            var end = 0;
            while (end < digits.Length && char.IsDigit(digits[end]))
            {
                end++;
            }

            if (end == 0)
            {
                break;
            }

            if (int.TryParse(digits.AsSpan(0, end), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                parts.Add(number);
            }
            else
            {
                break;
            }
        }

        return [.. parts];
    }
}
