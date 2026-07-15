namespace ArdaNova.Infrastructure.Wallets;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Algorand;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

public sealed class AlgorandWalletProofVerifier : IWalletProofVerifier
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private readonly string _network;

    public AlgorandWalletProofVerifier(IOptions<AlgorandSettings> settings)
    {
        _network = string.IsNullOrWhiteSpace(settings.Value.Network)
            ? "testnet"
            : settings.Value.Network.Trim().ToLowerInvariant();
    }

    public string Chain => "algorand";
    public string Network => _network;

    /// <inheritdoc/>
    public bool TryNormalizeAddress(string address, out string canonicalAddress)
    {
        canonicalAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(address))
            return false;

        var candidate = address.Trim().ToUpperInvariant();
        if (candidate.Length != 58 || !TryDecodeBase32(candidate, out var decoded) || decoded.Length != 36)
            return false;

        var publicKey = decoded[..32];
        var suppliedChecksum = decoded[32..];
        var digest = new Sha512tDigest(256);
        digest.BlockUpdate(publicKey, 0, publicKey.Length);
        var checksumSource = new byte[digest.GetDigestSize()];
        digest.DoFinal(checksumSource, 0);
        if (!CryptographicOperations.FixedTimeEquals(suppliedChecksum, checksumSource[^4..]))
            return false;

        canonicalAddress = candidate;
        return true;
    }

    /// <inheritdoc/>
    public bool Verify(string canonicalAddress, string message, string encodedSignature)
    {
        if (!TryNormalizeAddress(canonicalAddress, out var normalized)
            || !TryDecodeBase32(normalized, out var decoded)
            || !TryDecodeSignature(encodedSignature, out var signature)
            || signature.Length != 64)
        {
            return false;
        }

        var signer = new Ed25519Signer();
        signer.Init(false, new Ed25519PublicKeyParameters(decoded[..32], 0));
        var messageBytes = Encoding.UTF8.GetBytes(message);
        signer.BlockUpdate(messageBytes, 0, messageBytes.Length);
        return signer.VerifySignature(signature);
    }

    private static bool TryDecodeSignature(string value, out byte[] signature)
    {
        signature = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        try
        {
            signature = Convert.FromBase64String(normalized);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase32(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        var buffer = 0;
        var bits = 0;
        var output = new List<byte>((value.Length * 5) / 8);
        foreach (var character in value)
        {
            var digit = Base32Alphabet.IndexOf(character);
            if (digit < 0)
                return false;

            buffer = (buffer << 5) | digit;
            bits += 5;
            while (bits >= 8)
            {
                output.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
                buffer &= (1 << bits) - 1;
            }
        }

        if (bits != 2 || buffer != 0)
            return false;
        bytes = output.ToArray();
        return true;
    }
}
