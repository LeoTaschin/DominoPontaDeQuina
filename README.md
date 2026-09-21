# Domino Ponta de Quina

## Projetos

- `DominoPontaDeQuina.Core`: regras e fluxo do jogo.
- `DominoPontaDeQuina.Domain`: entidades e enums persistentes.
- `DominoPontaDeQuina.Repository`: `DominoDbContext`, mapeamentos Fluent API e repositorios EF Core.
- `DominoPontaDeQuina.Api`: API HTTP com autenticacao JWT.
- `DominoPontaDeQuina.Migrations`: aplicacao console usada como startup project para migrations.
- `DominoPontaDeQuina.Tests`: testes automatizados do nucleo do jogo e da autenticacao.

## Modelo persistente

`Usuario` representa a conta do aplicativo cliente e pode possuir varios `Jogador`, que sao perfis de jogo.
`Jogo` representa uma partida armazenada para consulta de historico. `ParticipacaoJogo` liga um jogador a um jogo e registra sua posicao, pontuacao e resultado.
`RefreshToken` guarda o hash dos tokens de renovacao emitidos para cada usuario, com data de expiracao, revogacao e o hash do token que o substituiu na rotacao.

## Pre-requisitos

- .NET 8 SDK
- Ferramenta `dotnet-ef` 8.x (`dotnet tool install --global dotnet-ef --version 8.*`)

## Restaurar e compilar

```bash
dotnet restore
dotnet build
```

## Autenticacao JWT

A API emite um **access token** JWT de vida curta (60 min por padrao) acompanhado de um
**refresh token** opaco de vida longa (7 dias por padrao). O cliente usa o access token no
cabecalho `Authorization` e, quando ele expira, troca o refresh token por um novo par sem
pedir a senha de novo.

As senhas nunca sao armazenadas em claro: o `ServicoSenha` aplica PBKDF2/HMAC-SHA256 com salt
aleatorio por usuario, e o valor gravado em `Usuario.HashSenha` tem o formato
`pbkdf2-sha256.{iteracoes}.{salt}.{hash}`, o que permite aumentar o numero de iteracoes no futuro
sem invalidar os usuarios ja cadastrados.

Os refresh tokens tambem nao sao guardados em claro. A tabela `RefreshTokens` armazena apenas o
hash SHA-256 do valor entregue ao cliente, entao um vazamento do banco nao permite renovar sessoes.

### Endpoints

| Metodo | Rota                          | Protegido | Descricao                                          |
| ------ | ----------------------------- | --------- | -------------------------------------------------- |
| POST   | `/api/autenticacao/registrar` | nao       | Cria a conta e ja devolve o par de tokens (`201`).  |
| POST   | `/api/autenticacao/login`     | nao       | Troca e-mail e senha pelo par de tokens (`200`).    |
| POST   | `/api/autenticacao/renovar`   | nao       | Troca o refresh token por um novo par (`200`).      |
| POST   | `/api/autenticacao/revogar`   | nao       | Logout: revoga o refresh token informado (`204`).   |
| GET    | `/api/autenticacao/eu`        | sim       | Devolve o usuario dono do access token enviado.     |

Respostas de erro usam `ProblemDetails`: `409 Conflict` para e-mail ja cadastrado,
`401 Unauthorized` para credenciais invalidas ou refresh token invalido, e `400 Bad Request` para
payload que nao passa nas validacoes.

Para proteger um novo endpoint basta anotar a action com `[Authorize]`. O identificador do usuario
autenticado fica na claim `sub` do access token.

### Rotacao e deteccao de reuso

Cada renovacao **rotaciona** o refresh token: o antigo e revogado na mesma operacao e passa a
apontar para o substituto (`SubstituidoPorHash`), formando a cadeia da sessao.

Se um refresh token ja rotacionado for reapresentado, a API trata isso como possivel vazamento:
nao da para saber se foi um cliente desatualizado ou um atacante com uma copia do token, entao
**todos os refresh tokens ativos daquele usuario sao revogados** e um aviso e registrado no log.
Na pratica, o usuario precisa fazer login de novo — e o atacante perde o acesso junto.

O `revogar` derruba apenas a sessao do token informado; as outras sessoes do mesmo usuario seguem
validas. Ele responde `204` mesmo para um token desconhecido, para nao revelar quais tokens existem.

Note que revogar um refresh token **nao** invalida um access token ja emitido: ele continua valendo
ate expirar. Esse e o custo normal de um JWT sem estado, e e o motivo de a expiracao do access token
ser curta.

### Configuracao

A secao `Jwt` do `appsettings.json` define emissor, audiencia, `MinutosExpiracao` (access token) e
`DiasExpiracaoRefreshToken`. A chave de assinatura **nao** fica versionada: em desenvolvimento ela
esta em `appsettings.Development.json` e em qualquer outro ambiente deve vir de variavel de ambiente
ou de user secrets.

```bash
dotnet user-secrets init --project DominoPontaDeQuina.Api
dotnet user-secrets set "Jwt:ChaveSecreta" "<chave com pelo menos 32 caracteres>" --project DominoPontaDeQuina.Api
```

A aplicacao valida essas opcoes na inicializacao (`ValidateOnStart`), entao ela falha logo no start
caso a chave esteja ausente ou curta demais.

### Executar a API

```bash
dotnet run --project DominoPontaDeQuina.Api
```

Em ambiente de desenvolvimento o Swagger fica em `/swagger` e o banco SQLite local e criado
automaticamente. Use o botao **Authorize** do Swagger colando apenas o access token (sem o prefixo
`Bearer`).

Exemplo pelo terminal:

```bash
curl -X POST http://localhost:5080/api/autenticacao/registrar \
  -H "Content-Type: application/json" \
  -d '{"nome":"Jogador","email":"jogador@exemplo.com","senha":"SenhaSegura123"}'

PAR=$(curl -s -X POST http://localhost:5080/api/autenticacao/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jogador@exemplo.com","senha":"SenhaSegura123"}')

TOKEN=$(echo "$PAR" | jq -r .token)
REFRESH=$(echo "$PAR" | jq -r .refreshToken)

curl http://localhost:5080/api/autenticacao/eu -H "Authorization: Bearer $TOKEN"

# quando o access token expirar, troque o refresh por um novo par
curl -X POST http://localhost:5080/api/autenticacao/renovar \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH\"}"

# logout
curl -X POST http://localhost:5080/api/autenticacao/revogar \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH\"}"
```
## Migrations

Os comandos devem usar `DominoPontaDeQuina.Migrations` como startup project e `DominoPontaDeQuina.Repository` como projeto do contexto:

```bash
dotnet ef migrations add Inicial \
  --project DominoPontaDeQuina.Repository \
  --startup-project DominoPontaDeQuina.Migrations

dotnet ef database update \
  --project DominoPontaDeQuina.Repository \
  --startup-project DominoPontaDeQuina.Migrations
```

Enquanto nao houver nenhuma migration gerada, a API cria o schema de desenvolvimento direto a partir
do modelo (`EnsureCreated`). Assim que a primeira migration existir, ela passa a aplicar as migrations.

O banco SQLite local `domino.db` e ignorado pelo Git.

## Testes

```bash
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj
```

Os testes de autenticacao ficam em `DominoPontaDeQuina.Tests/Autenticacao` e sobem a API em memoria
com um SQLite isolado, cobrindo registro, login, hash de senha, emissao e renovacao de tokens,
rotacao com deteccao de reuso, revogacao e bloqueio das rotas protegidas. Para rodar so eles:

```bash
dotnet test DominoPontaDeQuina.Tests/DominoPontaDeQuina.Tests.csproj \
  --filter "FullyQualifiedName~DominoPontaDeQuina.Tests.Autenticacao"
```
