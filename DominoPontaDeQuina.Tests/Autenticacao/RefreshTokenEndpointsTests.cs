using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DominoPontaDeQuina.Api.Dtos;

namespace DominoPontaDeQuina.Tests.Autenticacao;

public class RefreshTokenEndpointsTests : IClassFixture<ApiAutenticacaoFactory>
{
    private readonly HttpClient _cliente;

    public RefreshTokenEndpointsTests(ApiAutenticacaoFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    private static string EmailUnico() => $"jogador-{Guid.NewGuid():N}@exemplo.com";

    /// <summary>Registra um usuario novo e devolve o par de tokens emitido.</summary>
    private async Task<TokenResponse> RegistrarAsync()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", new RegistrarUsuarioRequest
        {
            Nome = "Joao da Silva",
            Email = EmailUnico(),
            Senha = "SenhaSegura123"
        });

        resposta.EnsureSuccessStatusCode();

        return (await resposta.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private Task<HttpResponseMessage> RenovarAsync(string refreshToken) =>
        _cliente.PostAsJsonAsync("/api/autenticacao/renovar", new RefreshTokenRequest { RefreshToken = refreshToken });

    private Task<HttpResponseMessage> RevogarAsync(string refreshToken) =>
        _cliente.PostAsJsonAsync("/api/autenticacao/revogar", new RefreshTokenRequest { RefreshToken = refreshToken });

    [Fact]
    public async Task Registrar_DeveEmitirRefreshTokenJuntoDoAccessToken()
    {
        var tokens = await RegistrarAsync();

        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.NotEqual(tokens.Token, tokens.RefreshToken);
        Assert.True(tokens.RefreshTokenExpiraEm > tokens.ExpiraEm);
    }

    [Fact]
    public async Task Login_DeveEmitirRefreshToken()
    {
        var registro = await RegistrarAsync();

        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/login", new LoginRequest
        {
            Email = registro.Usuario.Email,
            Senha = "SenhaSegura123"
        });

        var tokens = await resposta.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.False(string.IsNullOrWhiteSpace(tokens!.RefreshToken));
        Assert.NotEqual(registro.RefreshToken, tokens.RefreshToken);
    }

    [Fact]
    public async Task Renovar_DeveDevolverNovoParDeTokens()
    {
        var tokens = await RegistrarAsync();

        var resposta = await RenovarAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var renovados = await resposta.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(renovados!.Token));
        Assert.NotEqual(tokens.RefreshToken, renovados.RefreshToken);
        Assert.Equal(tokens.Usuario.Id, renovados.Usuario.Id);
    }

    [Fact]
    public async Task Renovar_AccessTokenRenovadoDeveAcessarRotaProtegida()
    {
        var tokens = await RegistrarAsync();

        var resposta = await RenovarAsync(tokens.RefreshToken);
        var renovados = await resposta.Content.ReadFromJsonAsync<TokenResponse>();

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/autenticacao/eu");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", renovados!.Token);

        var protegida = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.OK, protegida.StatusCode);

        var usuario = await protegida.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal(tokens.Usuario.Id, usuario!.Id);
    }

    [Fact]
    public async Task Renovar_DeveInvalidarORefreshTokenAnterior()
    {
        var tokens = await RegistrarAsync();

        await RenovarAsync(tokens.RefreshToken);
        var reuso = await RenovarAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
    }

    [Fact]
    public async Task Renovar_ReusoDeTokenRotacionadoDeveDerrubarTodasAsSessoes()
    {
        var tokens = await RegistrarAsync();

        var primeiraRenovacao = await RenovarAsync(tokens.RefreshToken);
        var vigente = (await primeiraRenovacao.Content.ReadFromJsonAsync<TokenResponse>())!;

        // Reapresentar o token ja rotacionado sinaliza possivel vazamento.
        var reuso = await RenovarAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);

        // Por isso ate o refresh token que ainda estava valido deixa de funcionar.
        var aposDeteccao = await RenovarAsync(vigente.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, aposDeteccao.StatusCode);
    }

    [Fact]
    public async Task Renovar_DeveRecusarTokenInexistente()
    {
        var resposta = await RenovarAsync("refresh-token-que-nunca-foi-emitido");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Renovar_DeveRecusarCorpoVazio()
    {
        var resposta = await RenovarAsync(string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Revogar_DeveImpedirRenovacaoPosterior()
    {
        var tokens = await RegistrarAsync();

        var revogacao = await RevogarAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, revogacao.StatusCode);

        var renovacao = await RenovarAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, renovacao.StatusCode);
    }

    [Fact]
    public async Task Revogar_DeveResponder204ParaTokenDesconhecido()
    {
        var resposta = await RevogarAsync("refresh-token-que-nunca-foi-emitido");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    [Fact]
    public async Task Revogar_NaoDeveAfetarOutrasSessoesDoMesmoUsuario()
    {
        var primeiraSessao = await RegistrarAsync();

        var login = await _cliente.PostAsJsonAsync("/api/autenticacao/login", new LoginRequest
        {
            Email = primeiraSessao.Usuario.Email,
            Senha = "SenhaSegura123"
        });
        var segundaSessao = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;

        await RevogarAsync(primeiraSessao.RefreshToken);

        var renovacao = await RenovarAsync(segundaSessao.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, renovacao.StatusCode);
    }
}
