namespace DominoPontaDeQuina.Api.Dtos;

/// <summary>Par de tokens devolvido pelo registro, pelo login e pela renovacao.</summary>
public class TokenResponse
{
    public string TipoToken { get; set; } = "Bearer";

    /// <summary>Token JWT de acesso, enviado no cabecalho Authorization.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    /// <summary>Token opaco usado para obter um novo par de tokens. Nao e um JWT.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiraEm { get; set; }

    public UsuarioResponse Usuario { get; set; } = new();
}
