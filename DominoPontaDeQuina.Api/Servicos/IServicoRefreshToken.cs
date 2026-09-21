namespace DominoPontaDeQuina.Api.Servicos;

public interface IServicoRefreshToken
{
    /// <summary>Gera um token opaco aleatorio para entregar ao cliente.</summary>
    string GerarToken();

    /// <summary>Calcula o hash que representa o token no banco.</summary>
    string CalcularHash(string token);
}
