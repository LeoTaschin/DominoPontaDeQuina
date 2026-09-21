using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Api.Servicos;

/// <summary>Token JWT de acesso ja emitido, com o instante em que deixa de valer.</summary>
public record TokenAcesso(string Token, DateTime ExpiraEm);

public interface IServicoToken
{
    /// <summary>Emite um token JWT de acesso assinado para o usuario informado.</summary>
    TokenAcesso GerarTokenAcesso(Usuario usuario);
}
