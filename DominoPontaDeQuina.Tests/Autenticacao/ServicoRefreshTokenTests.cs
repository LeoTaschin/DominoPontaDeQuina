using DominoPontaDeQuina.Api.Servicos;

namespace DominoPontaDeQuina.Tests.Autenticacao;

public class ServicoRefreshTokenTests
{
    private readonly ServicoRefreshToken _servico = new();

    [Fact]
    public void GerarToken_DeveProduzirValorNovoACadaChamada()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => _servico.GerarToken()).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    [Fact]
    public void GerarToken_DeveUsarAlfabetoSeguroParaUrl()
    {
        var token = _servico.GerarToken();

        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
        // 32 bytes em base64 sem padding ocupam 43 caracteres.
        Assert.Equal(43, token.Length);
    }

    [Fact]
    public void CalcularHash_DeveSerDeterministicoEDiferenteDoToken()
    {
        var token = _servico.GerarToken();

        var hash = _servico.CalcularHash(token);

        Assert.Equal(hash, _servico.CalcularHash(token));
        Assert.NotEqual(token, hash);
    }

    [Fact]
    public void CalcularHash_DeveDiferirParaTokensDiferentes()
    {
        var primeiro = _servico.CalcularHash(_servico.GerarToken());
        var segundo = _servico.CalcularHash(_servico.GerarToken());

        Assert.NotEqual(primeiro, segundo);
    }

    [Fact]
    public void CalcularHash_DeveRecusarTokenVazio()
    {
        Assert.Throws<ArgumentException>(() => _servico.CalcularHash(" "));
    }
}
