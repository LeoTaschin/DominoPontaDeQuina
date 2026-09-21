using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Repository.Configurations;

public class JogoConfiguration : IEntityTypeConfiguration<Jogo>
{
    public void Configure(EntityTypeBuilder<Jogo> builder)
    {
        builder.ToTable("Jogos");
        builder.HasKey(jogo => jogo.Id);

        builder.Property(jogo => jogo.IniciadoEm)
            .IsRequired();

        builder.Property(jogo => jogo.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasMany(jogo => jogo.Participacoes)
            .WithOne(participacao => participacao.Jogo)
            .HasForeignKey(participacao => participacao.JogoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
