using System.Security.Cryptography;

namespace DominoPontaDeQuina.Api.Servicos;

/// <summary>
/// Hash de senha com PBKDF2 (HMAC-SHA256). O valor armazenado tem o formato
/// "pbkdf2-sha256.{iteracoes}.{salt-base64}.{hash-base64}", de modo que o salt e o
/// numero de iteracoes viajam junto com o hash e podem evoluir sem quebrar usuarios antigos.
/// </summary>
public class ServicoSenha : IServicoSenha
{
    private const string Identificador = "pbkdf2-sha256";
    private const int IteracoesPadrao = 210_000;
    private const int TamanhoSaltBytes = 16;
    private const int TamanhoHashBytes = 32;

    public string GerarHash(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        var salt = RandomNumberGenerator.GetBytes(TamanhoSaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, IteracoesPadrao, HashAlgorithmName.SHA256, TamanhoHashBytes);

        return string.Join('.', Identificador, IteracoesPadrao, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verificar(string senha, string hashArmazenado)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hashArmazenado))
        {
            return false;
        }

        var partes = hashArmazenado.Split('.');
        if (partes.Length != 4 || partes[0] != Identificador)
        {
            return false;
        }

        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] hashEsperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            hashEsperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var hashInformado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);

        // Comparacao em tempo constante para nao vazar informacao pelo tempo de resposta.
        return CryptographicOperations.FixedTimeEquals(hashInformado, hashEsperado);
    }
}
