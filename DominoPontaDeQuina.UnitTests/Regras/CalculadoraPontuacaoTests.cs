using DominoPontaDeQuina.Core.Models;
using DominoPontaDeQuina.Core.Regras;

namespace DominoPontaDeQuina.UnitTests.Regras;

/// <summary>
/// Regra: quando a soma das pontas externas for multipla de 5, a jogada vale soma / 5 pontos.
/// </summary>
public class CalculadoraPontuacaoTests
{
    [Fact]
    public void Calcular_Soma10_Retorna2Pontos()
    {
        // Arrange
        const int soma = 10;

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(2);
    }

    [Fact]
    public void Calcular_Soma15_Retorna3Pontos()
    {
        // Arrange
        const int soma = 15;

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(3);
    }

    [Theory]
    [InlineData(5, 1)]
    [InlineData(10, 2)]
    [InlineData(15, 3)]
    [InlineData(20, 4)]
    [InlineData(25, 5)]
    public void Calcular_SomaMultiplaDe5_RetornaSomaDivididaPor5(int soma, int pontosEsperados)
    {
        // Arrange (dados vindos do InlineData)

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(pontosEsperados, "a soma {0} e multipla de 5", soma);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(14)]
    public void Calcular_SomaNaoMultiplaDe5_RetornaZero(int soma)
    {
        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(0);
    }

    [Fact]
    public void Calcular_SomaZero_RetornaZero()
    {
        // Arrange
        const int soma = 0;

        // Act
        var pontos = CalculadoraPontuacao.Calcular(soma);

        // Assert
        pontos.Should().Be(0, "zero e multiplo de 5, mas nao gera pontos");
    }

    [Fact]
    public void Calcular_SomaNegativa_LancaArgumentOutOfRangeException()
    {
        // Arrange
        Action act = () => CalculadoraPontuacao.Calcular(-5);

        // Act + Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("somaPontas");
    }

    [Fact]
    public void Calcular_TabuleiroComPontas5e5_Retorna2Pontos()
    {
        // Arrange: [5|3][3|5] -> pontas externas 5 + 5 = 10
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(5, 3));
        tabuleiro.Pecas.Add(new Peca(3, 5));

        // Act
        var pontos = CalculadoraPontuacao.Calcular(tabuleiro);

        // Assert
        tabuleiro.SomarPontasExternas().Should().Be(10);
        pontos.Should().Be(2);
    }

    [Fact]
    public void Calcular_TabuleiroComPontas6e1_RetornaZero()
    {
        // Arrange: [6|2][2|1] -> pontas externas 6 + 1 = 7
        var tabuleiro = new Tabuleiro();
        tabuleiro.Pecas.Add(new Peca(6, 2));
        tabuleiro.Pecas.Add(new Peca(2, 1));

        // Act
        var pontos = CalculadoraPontuacao.Calcular(tabuleiro);

        // Assert
        pontos.Should().Be(0);
    }

    [Fact]
    public void Calcular_TabuleiroVazio_RetornaZero()
    {
        // Arrange
        var tabuleiro = new Tabuleiro();

        // Act
        var pontos = CalculadoraPontuacao.Calcular(tabuleiro);

        // Assert
        pontos.Should().Be(0);
    }

    [Fact]
    public void Calcular_TabuleiroNulo_LancaArgumentNullException()
    {
        // Arrange
        Action act = () => CalculadoraPontuacao.Calcular((Tabuleiro)null!);

        // Act + Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("tabuleiro");
    }
}
