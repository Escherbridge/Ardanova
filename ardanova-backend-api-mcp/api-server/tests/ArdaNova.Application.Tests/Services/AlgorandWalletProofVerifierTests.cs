namespace ArdaNova.Application.Tests.Services;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.Infrastructure.Algorand;
using ArdaNova.Infrastructure.Wallets;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

public class AlgorandWalletProofVerifierTests
{
    [Fact]
    public void Verify_AcceptsOnlyTheMatchingCanonicalAddressAndEd25519Proof()
    {
        var privateKey = new Ed25519PrivateKeyParameters(RandomNumberGenerator.GetBytes(32), 0);
        var address = ToAlgorandAddress(privateKey.GeneratePublicKey().GetEncoded());
        const string message = "ArdaNova Wallet Verification\nversion:1";
        var signature = Sign(privateKey, message);
        var sut = new AlgorandWalletProofVerifier(Options.Create(new AlgorandSettings { Network = "TESTNET" }));

        sut.TryNormalizeAddress(address.ToLowerInvariant(), out var normalized).Should().BeTrue();
        normalized.Should().Be(address);
        sut.Network.Should().Be("testnet");
        sut.Verify(address, message, signature).Should().BeTrue();
        sut.Verify(address, message + "\nchanged", signature).Should().BeFalse();
        var wrongAddress = address[..57] + (address[57] == 'A' ? "B" : "A");
        sut.Verify(wrongAddress, message, signature).Should().BeFalse();
    }

    private static string Sign(Ed25519PrivateKeyParameters privateKey, string message)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, privateKey);
        var bytes = Encoding.UTF8.GetBytes(message);
        signer.BlockUpdate(bytes, 0, bytes.Length);
        return Convert.ToBase64String(signer.GenerateSignature());
    }

    private static string ToAlgorandAddress(byte[] publicKey)
    {
        var digest = new Sha512tDigest(256);
        digest.BlockUpdate(publicKey, 0, publicKey.Length);
        var checksumSource = new byte[digest.GetDigestSize()];
        digest.DoFinal(checksumSource, 0);
        return EncodeBase32(publicKey.Concat(checksumSource[^4..]).ToArray());
    }

    private static string EncodeBase32(byte[] value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var builder = new StringBuilder((value.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var item in value)
        {
            buffer = (buffer << 8) | item;
            bits += 8;
            while (bits >= 5)
            {
                builder.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
                buffer &= (1 << bits) - 1;
            }
        }

        if (bits > 0)
            builder.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return builder.ToString();
    }
}
