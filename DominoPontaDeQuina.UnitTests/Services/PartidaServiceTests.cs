using DominoPontaDeQuina.Application.Services;
using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Domain.Repositories;

namespace DominoPontaDeQuina.UnitTests.Services;

/// <summary>
/// Testa as decisoes do PartidaService: quando ele consulta e quando ele salva
/// nos repositorios. Os repositorios sao substituidos por mocks do Moq.
/// </summary>
public class PartidaServiceTests
{
    readonly Mock<IPartidaRepository> _partidas = new();
    readonly Mock<IJogadorRepository> _jogadores = new();
    readonly Mock<IParticipacaoRepository> _participacoes = new();
    readonly Mock<ILanceRepository> _lances = new();
    readonly Mock<IRankingRepository> _ranking = new();

    PartidaService CriarServico() =>
        new(_partidas.Object, _jogadores.Object, _participacoes.Object, _lances.Object, _ranking.Object);

    static Partida NovaPartida(string status = "EmAndamento") =>
        new() { Id = Guid.NewGuid(), PontuacaoAlvo = 50, Status = status };

    // ---------------------------------------------------------------- IniciarPartidaAsync

    [Fact]
    public async Task IniciarPartida_AlvoValido_SalvaPartidaEmAndamento()
    {
        // Arrange
        _partidas
            .Setup(r => r.AdicionarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Partida p, CancellationToken _) => p);
        var servico = CriarServico();

        // Act
        var partida = await servico.IniciarPartidaAsync(100);

        // Assert
        partida.PontuacaoAlvo.Should().Be(100);
        partida.Status.Should().Be("EmAndamento");
        partida.Id.Should().NotBeEmpty();
        _partidas.Verify(r => r.AdicionarAsync(
                It.Is<Partida>(p => p.PontuacaoAlvo == 100 && p.Status == "EmAndamento"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task IniciarPartida_AlvoInvalido_LancaExcecaoENaoSalva(int alvo)
    {
        // Arrange
        var servico = CriarServico();

        // Act
        var act = () => servico.IniciarPartidaAsync(alvo);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
                 .WithParameterName("pontuacaoAlvo");
        _partidas.Verify(r => r.AdicionarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- RegistrarJogadorAsync

    [Fact]
    public async Task RegistrarJogador_PartidaExiste_SalvaJogadorEParticipacaoComZeroPontos()
    {
        // Arrange
        var partida = NovaPartida();
        var usuarioId = Guid.NewGuid();
        _partidas.Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>())).ReturnsAsync(partida);
        _jogadores
            .Setup(r => r.AdicionarComUsuarioAsync(It.IsAny<Jogador>(), usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Jogador j, Guid? _, CancellationToken _) => j);
        var servico = CriarServico();

        // Act
        var jogador = await servico.RegistrarJogadorAsync(partida.Id, "Leo", usuarioId);

        // Assert
        jogador.Nome.Should().Be("Leo");
        _partidas.Verify(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()), Times.Once);
        _jogadores.Verify(r => r.AdicionarComUsuarioAsync(
                It.Is<Jogador>(j => j.Nome == "Leo"), usuarioId, It.IsAny<CancellationToken>()),
            Times.Once);
        _participacoes.Verify(r => r.AdicionarNaPartidaAsync(
                It.Is<ParticipacaoPartida>(p => p.Pontos == 0), partida.Id, jogador.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarJogador_PartidaInexistente_LancaKeyNotFoundENaoSalva()
    {
        // Arrange
        var partidaId = Guid.NewGuid();
        _partidas.Setup(r => r.ObterAsync(partidaId, It.IsAny<CancellationToken>())).ReturnsAsync((Partida?)null);
        var servico = CriarServico();

        // Act
        var act = () => servico.RegistrarJogadorAsync(partidaId, "Leo");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Partida não encontrada.");
        _jogadores.Verify(r => r.AdicionarComUsuarioAsync(It.IsAny<Jogador>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _participacoes.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<ParticipacaoPartida>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RegistrarJogador_NomeInvalido_LancaArgumentExceptionSemConsultarRepositorio(string? nome)
    {
        // Arrange
        var servico = CriarServico();

        // Act
        var act = () => servico.RegistrarJogadorAsync(Guid.NewGuid(), nome!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithParameterName("nome");
        _partidas.Verify(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _jogadores.VerifyNoOtherCalls();
        _participacoes.VerifyNoOtherCalls();
    }

    // ---------------------------------------------------------------- RegistrarLanceAsync

    [Fact]
    public async Task RegistrarLance_PartidaEmAndamentoEJogadorExiste_SalvaLance()
    {
        // Arrange
        var partida = NovaPartida();
        var jogadorId = Guid.NewGuid();
        _partidas.Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>())).ReturnsAsync(partida);
        _jogadores.Setup(r => r.ExisteAsync(jogadorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _lances
            .Setup(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), partida.Id, jogadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lance l, Guid _, Guid _, CancellationToken _) => l);
        var servico = CriarServico();

        // Act
        var lance = await servico.RegistrarLanceAsync(partida.Id, jogadorId);

        // Assert
        lance.Should().NotBeNull();
        lance.Id.Should().NotBeEmpty();
        _jogadores.Verify(r => r.ExisteAsync(jogadorId, It.IsAny<CancellationToken>()), Times.Once);
        _lances.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), partida.Id, jogadorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarLance_PartidaFinalizada_LancaInvalidOperationSemConsultarJogador()
    {
        // Arrange
        var partida = NovaPartida(status: "Finalizada");
        _partidas.Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>())).ReturnsAsync(partida);
        var servico = CriarServico();

        // Act
        var act = () => servico.RegistrarLanceAsync(partida.Id, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("A partida não está em andamento.");
        _jogadores.Verify(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _lances.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarLance_JogadorInexistente_LancaKeyNotFoundENaoSalva()
    {
        // Arrange
        var partida = NovaPartida();
        var jogadorId = Guid.NewGuid();
        _partidas.Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>())).ReturnsAsync(partida);
        _jogadores.Setup(r => r.ExisteAsync(jogadorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var servico = CriarServico();

        // Act
        var act = () => servico.RegistrarLanceAsync(partida.Id, jogadorId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Jogador não encontrado.");
        _lances.Verify(r => r.AdicionarNaPartidaAsync(It.IsAny<Lance>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarLance_PartidaInexistente_LancaKeyNotFoundSemConsultarJogador()
    {
        // Arrange
        _partidas.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Partida?)null);
        var servico = CriarServico();

        // Act
        var act = () => servico.RegistrarLanceAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Partida não encontrada.");
        _jogadores.Verify(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _lances.VerifyNoOtherCalls();
    }

    // ---------------------------------------------------------------- Consultas

    [Fact]
    public async Task VerificarStatus_PartidaExiste_RetornaPartidaDoRepositorio()
    {
        // Arrange
        var partida = NovaPartida();
        _partidas.Setup(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>())).ReturnsAsync(partida);
        var servico = CriarServico();

        // Act
        var resultado = await servico.VerificarStatusAsync(partida.Id);

        // Assert
        resultado.Should().BeSameAs(partida);
        _partidas.Verify(r => r.ObterAsync(partida.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerificarStatus_PartidaInexistente_LancaKeyNotFound()
    {
        // Arrange
        _partidas.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Partida?)null);
        var servico = CriarServico();

        // Act
        var act = () => servico.VerificarStatusAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ConsultarRanking_DelegaAoRepositorioERetornaLista()
    {
        // Arrange
        IReadOnlyList<Ranking> esperado =
        [
            new() { Vitorias = 5, Jogador = new Jogador { Nome = "Ana" } },
            new() { Vitorias = 3, Jogador = new Jogador { Nome = "Leo" } }
        ];
        _ranking.Setup(r => r.ObterAsync(It.IsAny<CancellationToken>())).ReturnsAsync(esperado);
        var servico = CriarServico();

        // Act
        var resultado = await servico.ConsultarRankingAsync();

        // Assert
        resultado.Should().BeEquivalentTo(esperado, o => o.WithStrictOrdering());
        _ranking.Verify(r => r.ObterAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConsultarHistorico_DelegaAoRepositorioSemSalvarNada()
    {
        // Arrange
        IReadOnlyList<Partida> historico = [NovaPartida(), NovaPartida("Finalizada")];
        _partidas.Setup(r => r.ObterHistoricoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(historico);
        var servico = CriarServico();

        // Act
        var resultado = await servico.ConsultarHistoricoAsync();

        // Assert
        resultado.Should().HaveCount(2).And.ContainInOrder(historico);
        _partidas.Verify(r => r.AdicionarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()), Times.Never);
        _partidas.Verify(r => r.AtualizarAsync(It.IsAny<Partida>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
