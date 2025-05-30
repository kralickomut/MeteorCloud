using System.Reflection;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace MeteorCloud.Shared.Jwt;

public static class RsaKeyUtils
{
    public static RsaSecurityKey LoadEmbeddedPublicKey(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        var keyText = reader.ReadToEnd();
        var rsa = RSA.Create();
        rsa.ImportFromPem(keyText);
        return new RsaSecurityKey(rsa);
    }

    public static RsaSecurityKey LoadEmbeddedPrivateKey(string resourceName)
    {
        var assembly = Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        var keyText = reader.ReadToEnd();
        var rsa = RSA.Create();
        rsa.ImportFromPem(keyText);
        return new RsaSecurityKey(rsa);
    }
}