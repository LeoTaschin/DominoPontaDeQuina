using System.ComponentModel.DataAnnotations;

namespace DominoPontaDeQuina.Api.Configuracoes;

/// <summary>
/// Parametros de emissao e validacao dos tokens, lidos da secao "Jwt" da configuracao.
/// </summary>
public class JwtOptions
{
    public const string SecaoConfiguracao = "Jwt";

    /// <summary>Emissor do token (claim "iss").</summary>
    [Required]
    public string Emissor { get; set; } = string.Empty;

    /// <summary>Publico do token (claim "aud").</summary>
    [Required]
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>Chave simetrica usada para assinar o token com HMAC-SHA256.</summary>
    [Required]
    [MinLength(32, ErrorMessage = "A chave secreta do JWT precisa ter ao menos 32 caracteres.")]
    public string ChaveSecreta { get; set; } = string.Empty;

    /// <summary>Tempo de vida do token de acesso, em minutos.</summary>
    [Range(1, 1440)]
    public int MinutosExpiracao { get; set; } = 60;

    /// <summary>Tempo de vida do refresh token, em dias.</summary>
    [Range(1, 365)]
    public int DiasExpiracaoRefreshToken { get; set; } = 7;
}
