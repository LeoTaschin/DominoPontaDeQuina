namespace DominoPontaDeQuina.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HashSenha { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<Jogador> Jogadores { get; set; } = new List<Jogador>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
