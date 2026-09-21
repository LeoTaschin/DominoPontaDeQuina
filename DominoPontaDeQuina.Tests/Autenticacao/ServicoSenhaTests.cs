using DominoPontaDeQuina.Api.Servicos;

namespace DominoPontaDeQuina.Tests.Autenticacao;

public class ServicoSenhaTests
{
    private readonly ServicoSenha _servico = new();

    [Fact]
    public void GerarHash_NaoDeveArmazenarSenhaEmClaro()
    {
        var hash = _servico.GerarHash("SenhaSegura123");

        Assert.DoesNotContain("SenhaSegura123", hash);
        Assert.StartsWith("pbkdf2-sha256.", hash);
    }

    [Fact]
    public void GerarHash_DeveProduzirValoresDiferentesParaMesmaSenha()
    {
        var primeiro = _servico.GerarHash("SenhaSegura123");
        var segundo = _servico.GerarHash("SenhaSegura123");

        Assert.NotEqual(primeiro, segundo);
    }

    [Fact]
    public void Verificar_DeveAceitarSenhaCorreta()
    {
        var hash = _servico.GerarHash("SenhaSegura123");

        Assert.True(_servico.Verificar("SenhaSegura123", hash));
    }

    [Fact]
    public void Verificar_DeveRecusarSenhaIncorreta()
    {
        var hash = _servico.GerarHash("SenhaSegura123");

        Assert.False(_servico.Verificar("senhasegura123", hash));
        Assert.False(_servico.Verificar("OutraSenha456", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("formato-invalido")]
    [InlineData("pbkdf2-sha256.abc.def.ghi")]
    public void Verificar_DeveRecusarHashMalFormado(string hashArmazenado)
    {
        Assert.False(_servico.Verificar("SenhaSegura123", hashArmazenado));
    }
}
