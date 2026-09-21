using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DominoPontaDeQuina.Repository.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.Id);

        builder.Property(usuario => usuario.Nome)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(usuario => usuario.Email)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(usuario => usuario.HashSenha)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(usuario => usuario.CriadoEm)
            .IsRequired();

        builder.HasIndex(usuario => usuario.Email)
            .IsUnique();

        builder.HasMany(usuario => usuario.Jogadores)
            .WithOne(jogador => jogador.Usuario)
            .HasForeignKey(jogador => jogador.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
