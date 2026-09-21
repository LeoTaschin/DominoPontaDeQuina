using System.ComponentModel.DataAnnotations;

namespace DominoPontaDeQuina.Api.Dtos;

/// <summary>Corpo usado tanto na renovacao quanto na revogacao (logout).</summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Informe o refresh token.")]
    public string RefreshToken { get; set; } = string.Empty;
}
