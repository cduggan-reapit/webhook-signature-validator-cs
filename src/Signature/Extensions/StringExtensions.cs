namespace Signature.Extensions;

public static class StringExtensions
{
    private const string TruncationTail = "...";

    public static string Truncate(this string input, int maxLength, string tail = TruncationTail)
    {
        var substringLength = Math.Max(1, maxLength - tail.Length);
        return input.Length > maxLength
            ? input[..substringLength] + tail
            : input;
    }

    public static string Tail(this string input, int maxLength, string head = TruncationTail)
    {
        var substringStart = input.Length - Math.Max(1, maxLength - head.Length);
        return input.Length > maxLength
            ? head + input[substringStart..]
            : input;
    }
}