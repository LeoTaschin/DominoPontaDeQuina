using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Repository.Configurations;

public class ParticipacaoJogoConfiguration : IEntityTypeConfiguration<ParticipacaoJogo>
{
    public void Configure(EntityTypeBuilder<ParticipacaoJogo> builder)
    {
        builder.ToTable("ParticipacoesJogo");
        builder.HasKey(participacao => participacao.Id);

        builder.Property(participacao => participacao.Posicao).IsRequired();
        builder.Property(participacao => participacao.Pontuacao).IsRequired();
        builder.Property(participacao => participacao.Vencedor).IsRequired();

        builder.HasIndex(participacao => new { participacao.JogoId, participacao.JogadorId })
            .IsUnique();
    }
}
