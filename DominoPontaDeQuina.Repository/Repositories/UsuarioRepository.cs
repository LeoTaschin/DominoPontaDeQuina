using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Repository.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly DominoDbContext _contexto;

    public UsuarioRepository(DominoDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _contexto.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmail(email);

        return _contexto.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(usuario => usuario.Email == emailNormalizado, cancellationToken);
    }

    public Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmail(email);

        return _contexto.Usuarios
            .AnyAsync(usuario => usuario.Email == emailNormalizado, cancellationToken);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _contexto.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        return _contexto.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
