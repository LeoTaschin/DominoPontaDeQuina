using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DominoPontaDeQuina.Api.Dtos;
using DominoPontaDeQuina.Api.Servicos;
using DominoPontaDeQuina.Repository.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DominoPontaDeQuina.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController : ControllerBase
{
    private readonly IServicoAutenticacao _servicoAutenticacao;
    private readonly IUsuarioRepository _usuarios;

    public AutenticacaoController(IServicoAutenticacao servicoAutenticacao, IUsuarioRepository usuarios)
    {
        _servicoAutenticacao = servicoAutenticacao;
        _usuarios = usuarios;
    }

    /// <summary>Cria uma conta e ja devolve o token de acesso.</summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistrarUsuarioRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _servicoAutenticacao.RegistrarAsync(requisicao, cancellationToken);

        if (!resultado.Sucesso)
        {
            return Problem(
                title: "Nao foi possivel registrar o usuario.",
                detail: resultado.Mensagem,
                statusCode: StatusCodes.Status409Conflict);
        }

        return CreatedAtAction(nameof(ObterUsuarioAutenticado), null, resultado.Token);
    }

    /// <summary>Troca e-mail e senha por um token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _servicoAutenticacao.AutenticarAsync(requisicao, cancellationToken);

        if (!resultado.Sucesso)
        {
            return Problem(
                title: "Falha na autenticacao.",
                detail: resultado.Mensagem,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(resultado.Token);
    }

    /// <summary>Troca um refresh token valido por um novo par de tokens.</summary>
    [HttpPost("renovar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Renovar(RefreshTokenRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _servicoAutenticacao.RenovarAsync(requisicao, cancellationToken);

        if (!resultado.Sucesso)
        {
            return Problem(
                title: "Falha ao renovar a sessao.",
                detail: resultado.Mensagem,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(resultado.Token);
    }

    /// <summary>Revoga o refresh token informado (logout). Responde 204 mesmo se o token ja nao valia.</summary>
    [HttpPost("revogar")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revogar(RefreshTokenRequest requisicao, CancellationToken cancellationToken)
    {
        await _servicoAutenticacao.RevogarAsync(requisicao, cancellationToken);

        return NoContent();
    }

    /// <summary>Devolve o usuario dono do token enviado no cabecalho Authorization.</summary>
    [HttpGet("eu")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObterUsuarioAutenticado(CancellationToken cancellationToken)
    {
        var identificador = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(identificador, out var usuarioId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarios.ObterPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            CriadoEm = usuario.CriadoEm
        });
    }
}
