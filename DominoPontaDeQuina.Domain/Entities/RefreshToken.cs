namespace DominoPontaDeQuina.Domain.Entities;

/// <summary>
/// Token de renovacao emitido para um usuario. O valor entregue ao cliente nunca e
/// persistido: guardamos apenas o hash, de modo que um vazamento do banco nao permite
/// renovar sessoes.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Hash SHA-256 (base64) do token entregue ao cliente.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiraEm { get; set; }

    public DateTime? RevogadoEm { get; set; }

    /// <summary>Hash do token que substituiu este na rotacao, quando houver.</summary>
    public string? SubstituidoPorHash { get; set; }

    public bool Expirado(DateTime agora) => agora >= ExpiraEm;

    public bool Ativo(DateTime agora) => RevogadoEm is null && !Expirado(agora);
}
