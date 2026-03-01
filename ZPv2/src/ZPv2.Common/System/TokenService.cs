using System.Security.Cryptography;
using System.Text;

namespace ZPv2.Common.System;

public static class TokenService
{
    private static readonly char[] Allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public static string Generate16()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder(16);
        for (var i = 0; i < 16; i++)
        {
            sb.Append(Allowed[bytes[i] % Allowed.Length]);
        }
        return sb.ToString();
    }

    public static bool IsValid(string? token)
    {
        var value = (token ?? string.Empty).Trim();
        if (value.Length != 16) return false;
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            var isUpper = ch >= 'A' && ch <= 'Z';
            var isDigit = ch >= '0' && ch <= '9';
            if (!isUpper && !isDigit) return false;
        }
        return true;
    }

    public static string NormalizeOrGenerate(string? token)
    {
        var normalized = (token ?? string.Empty).Trim().ToUpperInvariant();
        return IsValid(normalized) ? normalized : Generate16();
    }
}
