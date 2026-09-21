using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Repository.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>Busca o token pelo hash, ja com o usuario carregado e rastreado para atualizacao.</summary>
    Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AdicionarAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revoga todos os tokens ainda ativos do usuario e devolve quantos foram revogados.</summary>
    Task<int> RevogarAtivosDoUsuarioAsync(Guid usuarioId, DateTime revogadoEm, CancellationToken cancellationToken = default);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
