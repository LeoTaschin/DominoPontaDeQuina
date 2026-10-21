using DominoPontaDeQuina.Core.Models;

namespace DominoPontaDeQuina.Core.Regras;

/// <summary>
/// Regra de pontuacao do Domino Ponta de Quina:
/// quando a soma das pontas externas for multipla de 5, a jogada vale soma / 5 pontos.
/// </summary>
public static class CalculadoraPontuacao
{
    /// <summary>
    /// Calcula os pontos gerados por uma soma de pontas externas.
    /// </summary>
    /// <param name="somaPontas">A soma das pontas externas do tabuleiro.</param>
    /// <returns>soma / 5 quando a soma for multipla de 5 e maior que zero; caso contrario, 0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando a soma for negativa.</exception>
    public static int Calcular(int somaPontas)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(somaPontas);

        return somaPontas > 0 && somaPontas % 5 == 0
            ? somaPontas / 5
            : 0;
    }

    /// <summary>
    /// Calcula os pontos a partir do estado atual do tabuleiro.
    /// </summary>
    /// <param name="tabuleiro">O tabuleiro da rodada.</param>
    /// <returns>Os pontos gerados pelas pontas externas do tabuleiro.</returns>
    /// <exception cref="ArgumentNullException">Quando o tabuleiro for nulo.</exception>
    public static int Calcular(Tabuleiro tabuleiro)
    {
        ArgumentNullException.ThrowIfNull(tabuleiro);
        return Calcular(tabuleiro.SomarPontasExternas());
    }
}
