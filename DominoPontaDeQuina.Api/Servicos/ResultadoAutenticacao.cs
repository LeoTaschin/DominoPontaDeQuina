using DominoPontaDeQuina.Api.Dtos;

namespace DominoPontaDeQuina.Api.Servicos;

public enum ErroAutenticacao
{
    Nenhum = 0,
    EmailJaCadastrado,
    CredenciaisInvalidas,
    RefreshTokenInvalido
}

/// <summary>Resultado das operacoes de autenticacao, sem usar excecao para fluxo esperado.</summary>
public class ResultadoAutenticacao
{
    private ResultadoAutenticacao(TokenResponse? token, ErroAutenticacao erro, string? mensagem)
    {
        Token = token;
        Erro = erro;
        Mensagem = mensagem;
    }

    public TokenResponse? Token { get; }

    public ErroAutenticacao Erro { get; }

    public string? Mensagem { get; }

    public bool Sucesso => Erro == ErroAutenticacao.Nenhum;

    public static ResultadoAutenticacao ComSucesso(TokenResponse token) =>
        new(token, ErroAutenticacao.Nenhum, null);

    public static ResultadoAutenticacao ComFalha(ErroAutenticacao erro, string mensagem) =>
        new(null, erro, mensagem);
}
