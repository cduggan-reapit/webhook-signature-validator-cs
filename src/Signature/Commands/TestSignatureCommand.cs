using Signature.Extensions;
using Signature.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Signature.Commands;

public class TestSignatureCommand : AsyncCommand<TestSignatureCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("The path of the utf8-encoded text file containing the webhook payload.")]
        [CommandOption("-f|--file")]
        public string? PayloadFile { get; init; }

        [Description("The method used to encode the public key (base64/hex).")]
        [CommandOption("-e|--encoding")]
        public string? KeyEncoding { get; init; }

        [Description("The encoded public key.")]
        [CommandOption("-k|--key")]
        public string? PublicKey { get; init; }

        [Description("The signature header value.")]
        [CommandOption("-s|--signature")]
        public string? Signature { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var filePath = (await GetFilePathAsync(settings.PayloadFile, cancellationToken)).Trim('"');
        var encoding = (await GetKeyEncodingAsync(settings.KeyEncoding, cancellationToken)).Trim('"');
        var publicKey = await GetPublicKeyAsync(settings.PublicKey, encoding, cancellationToken);
        var signature = (await GetSignatureAsync(settings.Signature, cancellationToken)).Trim('"');

        var isValid = await SignatureHelper.ValidateSignatureAsync(encoding, publicKey, signature, filePath, cancellationToken);
        AnsiConsole.MarkupLine(isValid
            ? "[lime]Signature valid[/]"
            : "[red]Signature invalid[/]");

        return isValid ? 0 : 999;
    }

    private static async Task<string> GetFilePathAsync(string? path, CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>("Where is the payload file?")
            .Validate(given => IsValidFilePath(given)
                ? ValidationResult.Success()
                : ValidationResult.Error("[yellow]Invalid file path[/]"));

        // If the command-line value is not valid, ask the user to provide it
        if (!IsValidFilePath(path))
            return await AnsiConsole.PromptAsync(prompt, cancellationToken);

        // Otherwise print it to the screen to expose the given value (trust me, it keeps you sane)
        AnsiConsole.MarkupLine($"[grey53]-f: {path.Tail(75)}[/]");
        return path;
    }

    private static async Task<string> GetKeyEncodingAsync(string? encoding, CancellationToken cancellationToken)
    {
        string[] validEncodings = ["base64", "hex"];
        var prompt = new SelectionPrompt<string>()
            .Title("How is the public key encoded?")
            .AddChoices(validEncodings);

        if (encoding is null || !validEncodings.Contains(encoding, StringComparer.OrdinalIgnoreCase))
            return await AnsiConsole.PromptAsync(prompt, cancellationToken);

        var value = encoding.ToLowerInvariant();
        AnsiConsole.MarkupLine($"[grey53]-e: {value}[/]");
        return value;
    }

    private static async Task<string> GetPublicKeyAsync(string? publicKey, string encoding, CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>("What is the public key for this event?")
            .Validate(given => IsValidPublicKey(given, encoding)
                ? ValidationResult.Success()
                : ValidationResult.Error($"[yellow]Invalid {encoding} key[/]"));

        if (publicKey is null || !IsValidPublicKey(publicKey, encoding))
            return await AnsiConsole.PromptAsync(prompt, cancellationToken);

        AnsiConsole.MarkupLine($"[grey53]-k: {publicKey.Truncate(75)}[/]");
        return publicKey;
    }

    private static async Task<string> GetSignatureAsync(string? signature, CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>("What is the signature to test?")
            .Validate(given => IsValidSignatureFormat(given)
                ? ValidationResult.Success()
                : ValidationResult.Error($"[yellow]Invalid signature format[/]"));

        if (signature is null || !IsValidSignatureFormat(signature))
            return await AnsiConsole.PromptAsync(prompt, cancellationToken);

        AnsiConsole.MarkupLine($"[grey53]-s: {signature.Truncate(75)}[/]");
        return signature;
    }

    private static bool IsValidFilePath([NotNullWhen(true)] string? path)
        => path is { Length: > 0 } && File.Exists(path.Trim('"'));

    private static bool IsValidPublicKey(string publicKey, string encoding)
        => encoding switch
        {
            "base64" => IsValidBase64Key(publicKey.Trim('"')),
            "hex" => IsValidHexKey(publicKey.Trim('"')),
            _ => false
        };

    private static bool IsValidBase64Key(string base64)
        => Convert.TryFromBase64String(base64, new Span<byte>(), out _);

    private static bool IsValidHexKey(string hex)
        => hex.Length % 2 == 0 &&
           hex.ToCharArray().All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');

    private static bool IsValidSignatureFormat(string signature)
    {
        var parts = signature.Trim('"').Split(':');
        if (parts.Length != 4)
            return false;

        if (!parts.First().Equals("s", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}