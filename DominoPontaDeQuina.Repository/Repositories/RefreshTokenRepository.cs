using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Repository.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DominoDbContext _contexto;

    public RefreshTokenRepository(DominoDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _contexto.RefreshTokens
            .Include(token => token.Usuario)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AdicionarAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _contexto.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task<int> RevogarAtivosDoUsuarioAsync(Guid usuarioId, DateTime revogadoEm, CancellationToken cancellationToken = default)
    {
        var ativos = await _contexto.RefreshTokens
            .Where(token => token.UsuarioId == usuarioId
                && token.RevogadoEm == null
                && token.ExpiraEm > revogadoEm)
            .ToListAsync(cancellationToken);

        foreach (var token in ativos)
        {
            token.RevogadoEm = revogadoEm;
        }

        return ativos.Count;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        return _contexto.SaveChangesAsync(cancellationToken);
    }
}
