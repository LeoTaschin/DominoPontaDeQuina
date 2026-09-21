using DominoPontaDeQuina.Api.Configuracoes;
using DominoPontaDeQuina.Api.Dtos;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Repositories;
using Microsoft.Extensions.Options;

namespace DominoPontaDeQuina.Api.Servicos;

public class ServicoAutenticacao : IServicoAutenticacao
{
    private const string MensagemCredenciaisInvalidas = "E-mail ou senha invalidos.";
    private const string MensagemRefreshInvalido = "Refresh token invalido ou expirado.";

    private readonly IUsuarioRepository _usuarios;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IServicoSenha _servicoSenha;
    private readonly IServicoToken _servicoToken;
    private readonly IServicoRefreshToken _servicoRefreshToken;
    private readonly JwtOptions _opcoes;
    private readonly TimeProvider _relogio;
    private readonly ILogger<ServicoAutenticacao> _logger;

    public ServicoAutenticacao(
        IUsuarioRepository usuarios,
        IRefreshTokenRepository refreshTokens,
        IServicoSenha servicoSenha,
        IServicoToken servicoToken,
        IServicoRefreshToken servicoRefreshToken,
        IOptions<JwtOptions> opcoes,
        TimeProvider relogio,
        ILogger<ServicoAutenticacao> logger)
    {
        _usuarios = usuarios;
        _refreshTokens = refreshTokens;
        _servicoSenha = servicoSenha;
        _servicoToken = servicoToken;
        _servicoRefreshToken = servicoRefreshToken;
        _opcoes = opcoes.Value;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<ResultadoAutenticacao> RegistrarAsync(RegistrarUsuarioRequest requisicao, CancellationToken cancellationToken = default)
    {
        var email = NormalizarEmail(requisicao.Email);

        if (await _usuarios.ExisteComEmailAsync(email, cancellationToken))
        {
            return ResultadoAutenticacao.ComFalha(ErroAutenticacao.EmailJaCadastrado, "Ja existe um usuario com este e-mail.");
        }

        var agora = _relogio.GetUtcNow().UtcDateTime;

        var usuario = new Usuario
        {
            Nome = requisicao.Nome.Trim(),
            Email = email,
            HashSenha = _servicoSenha.GerarHash(requisicao.Senha),
            CriadoEm = agora
        };

        await _usuarios.AdicionarAsync(usuario, cancellationToken);
        await _usuarios.SalvarAlteracoesAsync(cancellationToken);

        return ResultadoAutenticacao.ComSucesso(await EmitirParDeTokensAsync(usuario, anterior: null, agora, cancellationToken));
    }

    public async Task<ResultadoAutenticacao> AutenticarAsync(LoginRequest requisicao, CancellationToken cancellationToken = default)
    {
        var email = NormalizarEmail(requisicao.Email);
        var usuario = await _usuarios.ObterPorEmailAsync(email, cancellationToken);

        // Mesmo sem usuario a senha e verificada contra um hash descartavel, para que
        // e-mail inexistente e senha errada levem o mesmo tempo de resposta.
        var hashParaConferir = usuario?.HashSenha ?? HashFicticio.Value;
        var senhaConfere = _servicoSenha.Verificar(requisicao.Senha, hashParaConferir);

        if (usuario is null || !senhaConfere)
        {
            return ResultadoAutenticacao.ComFalha(ErroAutenticacao.CredenciaisInvalidas, MensagemCredenciaisInvalidas);
        }

        var agora = _relogio.GetUtcNow().UtcDateTime;

        return ResultadoAutenticacao.ComSucesso(await EmitirParDeTokensAsync(usuario, anterior: null, agora, cancellationToken));
    }

    public async Task<ResultadoAutenticacao> RenovarAsync(RefreshTokenRequest requisicao, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requisicao.RefreshToken))
        {
            return ResultadoAutenticacao.ComFalha(ErroAutenticacao.RefreshTokenInvalido, MensagemRefreshInvalido);
        }

        var hash = _servicoRefreshToken.CalcularHash(requisicao.RefreshToken);
        var armazenado = await _refreshTokens.ObterPorHashAsync(hash, cancellationToken);
        var agora = _relogio.GetUtcNow().UtcDateTime;

        if (armazenado is null)
        {
            return ResultadoAutenticacao.ComFalha(ErroAutenticacao.RefreshTokenInvalido, MensagemRefreshInvalido);
        }

        if (armazenado.RevogadoEm is not null)
        {
            // Token ja rotacionado sendo reapresentado: ou o cliente guardou um valor antigo,
            // ou alguem capturou o token. Como nao da para distinguir, derrubamos todas as
            // sessoes ativas do usuario e obrigamos um novo login.
            var revogados = await _refreshTokens.RevogarAtivosDoUsuarioAsync(armazenado.UsuarioId, agora, cancellationToken);
            await _refreshTokens.SalvarAlteracoesAsync(cancellationToken);

            _logger.LogWarning(
                "Reuso de refresh token detectado para o usuario {UsuarioId}. {Revogados} token(s) ativo(s) revogado(s).",
                armazenado.UsuarioId,
                revogados);

            return ResultadoAutenticacao.ComFalha(ErroAutenticacao.RefreshTokenInvalido, MensagemRefreshInvalido);
        }

        if (armazenado.Expirado(agora))
        {
            return ResultadoAutenticacao.ComFalha(ErroAutenticacao.RefreshTokenInvalido, MensagemRefreshInvalido);
        }

        return ResultadoAutenticacao.ComSucesso(
            await EmitirParDeTokensAsync(armazenado.Usuario, armazenado, agora, cancellationToken));
    }

    public async Task RevogarAsync(RefreshTokenRequest requisicao, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requisicao.RefreshToken))
        {
            return;
        }

        var hash = _servicoRefreshToken.CalcularHash(requisicao.RefreshToken);
        var armazenado = await _refreshTokens.ObterPorHashAsync(hash, cancellationToken);
        var agora = _relogio.GetUtcNow().UtcDateTime;

        if (armazenado is null || !armazenado.Ativo(agora))
        {
            return;
        }

        armazenado.RevogadoEm = agora;
        await _refreshTokens.SalvarAlteracoesAsync(cancellationToken);
    }

    /// <summary>
    /// Emite o par access token + refresh token. Quando ha um token anterior ele e revogado e
    /// apontado para o substituto, formando a cadeia usada na deteccao de reuso.
    /// </summary>
    private async Task<TokenResponse> EmitirParDeTokensAsync(
        Usuario usuario,
        RefreshToken? anterior,
        DateTime agora,
        CancellationToken cancellationToken)
    {
        var tokenAcesso = _servicoToken.GerarTokenAcesso(usuario);

        var refreshEmClaro = _servicoRefreshToken.GerarToken();
        var refreshToken = new RefreshToken
        {
            UsuarioId = usuario.Id,
            TokenHash = _servicoRefreshToken.CalcularHash(refreshEmClaro),
            CriadoEm = agora,
            ExpiraEm = agora.AddDays(_opcoes.DiasExpiracaoRefreshToken)
        };

        if (anterior is not null)
        {
            anterior.RevogadoEm = agora;
            anterior.SubstituidoPorHash = refreshToken.TokenHash;
        }

        await _refreshTokens.AdicionarAsync(refreshToken, cancellationToken);
        await _refreshTokens.SalvarAlteracoesAsync(cancellationToken);

        return new TokenResponse
        {
            Token = tokenAcesso.Token,
            ExpiraEm = tokenAcesso.ExpiraEm,
            RefreshToken = refreshEmClaro,
            RefreshTokenExpiraEm = refreshToken.ExpiraEm,
            Usuario = new UsuarioResponse
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                CriadoEm = usuario.CriadoEm
            }
        };
    }

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    private static class HashFicticio
    {
        public static readonly string Value = new ServicoSenha().GerarHash(Guid.NewGuid().ToString());
    }
}
