using DominoPontaDeQuina.Repository.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DominoPontaDeQuina.Tests.Autenticacao;

/// <summary>
/// Sobe a API em memoria com um SQLite isolado por instancia de teste.
/// </summary>
public class ApiAutenticacaoFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ChaveSecreta = "chave-de-teste-com-mais-de-32-caracteres!!";
    public const string Emissor = "DominoPontaDeQuina";
    public const string Audiencia = "DominoPontaDeQuina.Clientes";

    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Emissor", Emissor);
        builder.UseSetting("Jwt:Audiencia", Audiencia);
        builder.UseSetting("Jwt:ChaveSecreta", ChaveSecreta);
        builder.UseSetting("Jwt:MinutosExpiracao", "60");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DominoDbContext>>();
            services.RemoveAll<DominoDbContext>();
            services.AddDbContext<DominoDbContext>(opcoes => opcoes.UseSqlite(_conexao));

            using var provedor = services.BuildServiceProvider();
            using var escopo = provedor.CreateScope();
            escopo.ServiceProvider.GetRequiredService<DominoDbContext>().Database.EnsureCreated();
        });
    }

    public Task InitializeAsync()
    {
        _conexao.Open();
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _conexao.DisposeAsync();
        await base.DisposeAsync();
    }
}
