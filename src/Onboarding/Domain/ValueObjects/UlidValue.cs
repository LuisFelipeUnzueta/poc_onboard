using System.Security.Cryptography;
using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public abstract record UlidValue
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    protected UlidValue(string value)
    {
        Value = value;
    }

    public string Value { get; }

    protected static string NewUlid()
    {
        var bytes = new byte[16];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;

        RandomNumberGenerator.Fill(bytes.AsSpan(6));

        Span<char> chars = stackalloc char[26];
        var bitBuffer = 0;
        var bitBufferLength = 0;
        var charIndex = 0;

        foreach (var currentByte in bytes)
        {
            bitBuffer = (bitBuffer << 8) | currentByte;
            bitBufferLength += 8;

            while (bitBufferLength >= 5)
            {
                chars[charIndex++] = Alphabet[(bitBuffer >> (bitBufferLength - 5)) & 31];
                bitBufferLength -= 5;
            }
        }

        if (bitBufferLength > 0)
        {
            chars[charIndex] = Alphabet[(bitBuffer << (5 - bitBufferLength)) & 31];
        }

        return new string(chars);
    }

    protected static Result<string> Validate(string value, string fieldName)
    {
        if (value.Length != 26 || value.Any(c => !Alphabet.Contains(c)))
        {
            return Result<string>.Failure(DomainError.Validation($"{fieldName} must be a valid ULID."));
        }

        return Result<string>.Success(value);
    }
}
