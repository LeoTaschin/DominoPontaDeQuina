namespace DominoPontaDeQuina.Api.Servicos;

public interface IServicoSenha
{
    /// <summary>Gera o hash da senha em claro, ja com salt aleatorio embutido.</summary>
    string GerarHash(string senha);

    /// <summary>Confere a senha em claro contra o hash armazenado.</summary>
    bool Verificar(string senha, string hashArmazenado);
}
