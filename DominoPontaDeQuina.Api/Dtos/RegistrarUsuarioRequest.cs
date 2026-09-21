using System.ComponentModel.DataAnnotations;

namespace DominoPontaDeQuina.Api.Dtos;

public class RegistrarUsuarioRequest
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(120, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}
