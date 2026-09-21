using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Repository.Configurations;

public class JogadorConfiguration : IEntityTypeConfiguration<Jogador>
{
    public void Configure(EntityTypeBuilder<Jogador> builder)
    {
        builder.ToTable("Jogadores");
        builder.HasKey(jogador => jogador.Id);

        builder.Property(jogador => jogador.NomeExibicao)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasMany(jogador => jogador.Participacoes)
            .WithOne(participacao => participacao.Jogador)
            .HasForeignKey(participacao => participacao.JogadorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
