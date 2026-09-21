using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Repository.Context;

public class DominoDbContext : DbContext
{
    public DominoDbContext()
    {
    }

    public DominoDbContext(DbContextOptions<DominoDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<ParticipacaoJogo> Participacoes => Set<ParticipacaoJogo>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=domino.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DominoDbContext).Assembly);
    }
}
