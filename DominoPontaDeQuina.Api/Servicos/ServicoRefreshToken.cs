using System.Security.Cryptography;
using System.Text;

namespace DominoPontaDeQuina.Api.Servicos;

/// <summary>
/// O refresh token e um valor opaco de 256 bits vindo do gerador criptografico. Como ele ja
/// tem entropia suficiente, guardar SHA-256 puro basta: nao ha o que adivinhar por forca bruta,
/// entao nao precisamos de salt nem de KDF lenta como na senha.
/// </summary>
public class ServicoRefreshToken : IServicoRefreshToken
{
    private const int TamanhoTokenBytes = 32;

    public string GerarToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(TamanhoTokenBytes));

    public string CalcularHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
