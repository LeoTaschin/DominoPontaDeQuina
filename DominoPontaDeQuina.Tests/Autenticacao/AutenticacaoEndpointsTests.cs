using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DominoPontaDeQuina.Api.Dtos;

namespace DominoPontaDeQuina.Tests.Autenticacao;

public class AutenticacaoEndpointsTests : IClassFixture<ApiAutenticacaoFactory>
{
    private readonly HttpClient _cliente;

    public AutenticacaoEndpointsTests(ApiAutenticacaoFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    private static RegistrarUsuarioRequest NovoRegistro(string email) => new()
    {
        Nome = "Joao da Silva",
        Email = email,
        Senha = "SenhaSegura123"
    };

    private static string EmailUnico() => $"jogador-{Guid.NewGuid():N}@exemplo.com";

    [Fact]
    public async Task Registrar_DeveCriarUsuarioEDevolverToken()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", NovoRegistro(EmailUnico()));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var conteudo = await resposta.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(conteudo);
        Assert.False(string.IsNullOrWhiteSpace(conteudo!.Token));
        Assert.Equal("Bearer", conteudo.TipoToken);
        Assert.NotEqual(Guid.Empty, conteudo.Usuario.Id);
        Assert.True(conteudo.ExpiraEm > DateTime.UtcNow);
    }

    [Fact]
    public async Task Registrar_DeveRecusarEmailJaCadastrado()
    {
        var registro = NovoRegistro(EmailUnico());

        await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", registro);
        var repetida = await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", registro);

        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
    }

    [Fact]
    public async Task Registrar_DeveRecusarSenhaCurta()
    {
        var registro = NovoRegistro(EmailUnico());
        registro.Senha = "curta";

        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", registro);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_DeveDevolverTokenParaCredenciaisValidas()
    {
        var registro = NovoRegistro(EmailUnico());
        await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", registro);

        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/login", new LoginRequest
        {
            Email = registro.Email.ToUpperInvariant(),
            Senha = registro.Senha
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var conteudo = await resposta.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(conteudo?.Token));
    }

    [Fact]
    public async Task Login_DeveRecusarSenhaIncorreta()
    {
        var registro = NovoRegistro(EmailUnico());
        await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", registro);

        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/login", new LoginRequest
        {
            Email = registro.Email,
            Senha = "SenhaErrada123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_DeveRecusarUsuarioInexistente()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/autenticacao/login", new LoginRequest
        {
            Email = EmailUnico(),
            Senha = "SenhaSegura123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_DeveRecusarRequisicaoSemToken()
    {
        var resposta = await _cliente.GetAsync("/api/autenticacao/eu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_DeveRecusarTokenInvalido()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/autenticacao/eu");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token.completamente.invalido");

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_DeveDevolverUsuarioDoToken()
    {
        var registro = NovoRegistro(EmailUnico());
        var criacao = await _cliente.PostAsJsonAsync("/api/autenticacao/registrar", registro);
        var token = (await criacao.Content.ReadFromJsonAsync<TokenResponse>())!.Token;

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/autenticacao/eu");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal(registro.Email.ToLowerInvariant(), usuario!.Email);
        Assert.Equal(registro.Nome, usuario.Nome);
    }
}
