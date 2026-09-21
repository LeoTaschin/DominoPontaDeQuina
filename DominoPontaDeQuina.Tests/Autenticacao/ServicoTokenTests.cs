using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DominoPontaDeQuina.Api.Configuracoes;
using DominoPontaDeQuina.Api.Servicos;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DominoPontaDeQuina.Tests.Autenticacao;

public class ServicoTokenTests
{
    private static readonly JwtOptions Opcoes = new()
    {
        Emissor = "DominoPontaDeQuina",
        Audiencia = "DominoPontaDeQuina.Clientes",
        ChaveSecreta = "chave-de-teste-com-mais-de-32-caracteres!!",
        MinutosExpiracao = 30
    };

    private readonly ServicoToken _servico = new(Options.Create(Opcoes), TimeProvider.System);

    private static Usuario NovoUsuario() => new()
    {
        Nome = "Maria",
        Email = "maria@exemplo.com"
    };

    [Fact]
    public void GerarToken_DeveIncluirClaimsDoUsuario()
    {
        var usuario = NovoUsuario();

        var resposta = _servico.GerarTokenAcesso(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(resposta.Token);

        Assert.Equal(usuario.Id.ToString(), token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(usuario.Email, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(usuario.Nome, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(Opcoes.Emissor, token.Issuer);
        Assert.Contains(Opcoes.Audiencia, token.Audiences);
    }

    [Fact]
    public void GerarToken_DeveRespeitarTempoDeExpiracaoConfigurado()
    {
        var antes = DateTime.UtcNow;

        var resposta = _servico.GerarTokenAcesso(NovoUsuario());

        var esperado = antes.AddMinutes(Opcoes.MinutosExpiracao);
        Assert.InRange(resposta.ExpiraEm, esperado.AddSeconds(-30), esperado.AddSeconds(30));
    }

    [Fact]
    public void GerarToken_DeveSerValidavelComAChaveConfigurada()
    {
        var resposta = _servico.GerarTokenAcesso(NovoUsuario());

        var parametros = new TokenValidationParameters
        {
            ValidIssuer = Opcoes.Emissor,
            ValidAudience = Opcoes.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Opcoes.ChaveSecreta)),
            ValidateLifetime = true
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(resposta.Token, parametros, out _);

        Assert.True(principal.Identity?.IsAuthenticated);
    }

    [Fact]
    public void GerarToken_NaoDeveSerValidoComOutraChave()
    {
        var resposta = _servico.GerarTokenAcesso(NovoUsuario());

        var parametros = new TokenValidationParameters
        {
            ValidIssuer = Opcoes.Emissor,
            ValidAudience = Opcoes.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("outra-chave-de-teste-com-32-caracteres!!")),
            ValidateLifetime = true
        };

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => new JwtSecurityTokenHandler().ValidateToken(resposta.Token, parametros, out _));
    }
}
