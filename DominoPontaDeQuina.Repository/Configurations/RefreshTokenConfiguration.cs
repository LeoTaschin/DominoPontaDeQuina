using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Repository.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(token => token.CriadoEm).IsRequired();
        builder.Property(token => token.ExpiraEm).IsRequired();

        builder.Property(token => token.SubstituidoPorHash)
            .HasMaxLength(200);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => token.UsuarioId);

        builder.HasOne(token => token.Usuario)
            .WithMany(usuario => usuario.RefreshTokens)
            .HasForeignKey(token => token.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
