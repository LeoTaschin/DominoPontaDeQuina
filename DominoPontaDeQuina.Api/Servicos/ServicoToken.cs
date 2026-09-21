using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DominoPontaDeQuina.Api.Configuracoes;
using DominoPontaDeQuina.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DominoPontaDeQuina.Api.Servicos;

public class ServicoToken : IServicoToken
{
    private readonly JwtOptions _opcoes;
    private readonly TimeProvider _relogio;

    public ServicoToken(IOptions<JwtOptions> opcoes, TimeProvider relogio)
    {
        _opcoes = opcoes.Value;
        _relogio = relogio;
    }

    public TokenAcesso GerarTokenAcesso(Usuario usuario)
    {
        var emitidoEm = _relogio.GetUtcNow().UtcDateTime;
        var expiraEm = emitidoEm.AddMinutes(_opcoes.MinutosExpiracao);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Name, usuario.Nome),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.ChaveSecreta));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opcoes.Emissor,
            audience: _opcoes.Audiencia,
            claims: claims,
            notBefore: emitidoEm,
            expires: expiraEm,
            signingCredentials: credenciais);

        return new TokenAcesso(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
