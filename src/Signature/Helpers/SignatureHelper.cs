using Microsoft.AspNetCore.WebUtilities;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System.Globalization;
using System.Text;

namespace Signature.Helpers;

public static class SignatureHelper
{
    public static async Task<bool> ValidateSignatureAsync(string encoding, string publicKey, string signature, string payloadPath, CancellationToken cancellationToken)
    {
        // Read the public key
        var keyBytes = GetPublicKeyBytes(encoding, publicKey);

        // Parse the signature header (s:guid:timestamp:signature)
        var signatureParts = signature.Split(':');
        var timestamp = signatureParts[2];
        var signatureBytes = WebEncoders.Base64UrlDecode(signatureParts[3]);

        // Read the payload in from the file
        var payload = await ReadPayloadFileAsync(payloadPath, cancellationToken);
        var bytes = Encoding.UTF8.GetBytes($"{timestamp}{payload}");

        var validator = new Ed25519Signer();
        validator.Init(false, new Ed25519PublicKeyParameters(keyBytes, 0));
        validator.BlockUpdate(bytes, 0, bytes.Length);

        return validator.VerifySignature(signatureBytes);
    }

    private static byte[] GetPublicKeyBytes(string encoding, string publicKey)
        => encoding.Equals("hex", StringComparison.OrdinalIgnoreCase)
            ? ConvertHexStringToByteArray(publicKey)
            : WebEncoders.Base64UrlDecode(publicKey);

    private static byte[] ConvertHexStringToByteArray(string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
        }

        var data = new byte[hexString.Length / 2];
        for (var index = 0; index < data.Length; index++)
        {
            var byteValue = hexString.Substring(index * 2, 2);
            data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return data;
    }

    private static async Task<string> ReadPayloadFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var handle = File.OpenRead(path);
        using var reader = new StreamReader(handle, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}