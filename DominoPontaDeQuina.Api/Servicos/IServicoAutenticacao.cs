using DominoPontaDeQuina.Api.Dtos;

namespace DominoPontaDeQuina.Api.Servicos;

public interface IServicoAutenticacao
{
    Task<ResultadoAutenticacao> RegistrarAsync(RegistrarUsuarioRequest requisicao, CancellationToken cancellationToken = default);

    Task<ResultadoAutenticacao> AutenticarAsync(LoginRequest requisicao, CancellationToken cancellationToken = default);

    /// <summary>Troca um refresh token valido por um novo par de tokens, rotacionando o anterior.</summary>
    Task<ResultadoAutenticacao> RenovarAsync(RefreshTokenRequest requisicao, CancellationToken cancellationToken = default);

    /// <summary>Revoga o refresh token informado, se ele ainda estiver ativo.</summary>
    Task RevogarAsync(RefreshTokenRequest requisicao, CancellationToken cancellationToken = default);
}
