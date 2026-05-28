# notX

Plataforma de envio de notificações multi-canal (Email e SMS) construída com .NET 9. Expõe uma API REST com autenticação por API key, processa mensagens de forma assíncrona via Outbox Pattern e entrega atualizações em tempo real para dashboards via Server-Sent Events.

---

## Sumário

- [Visão geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Projetos da solução](#projetos-da-solução)
- [Conceitos e decisões técnicas](#conceitos-e-decisões-técnicas)
- [Fluxo de uma notificação](#fluxo-de-uma-notificação)
- [API](#api)
- [Configuração e variáveis de ambiente](#configuração-e-variáveis-de-ambiente)
- [Rodando localmente](#rodando-localmente)
- [Banco de dados e migrações](#banco-de-dados-e-migrações)
- [Próximos passos](#próximos-passos)

---

## Visão geral

O notX funciona como um serviço centralizado de notificações. Qualquer sistema externo cria uma aplicação, recebe uma API key e começa a enfileirar notificações via HTTP. O Worker consome essa fila em background, realiza o envio e atualiza o status, sem que a API precise aguardar o resultado do SMTP ou do gateway de SMS.

```
Cliente HTTP
    │
    ▼
notX API  ──── grava Notification + OutboxMessage ────► PostgreSQL
    │
    └──── publica evento SSE via Redis Pub/Sub ────► Dashboard (browser)

notX EmailWorker
    │
    ├── polling a cada 10s (com distributed lock via Redis)
    ├── consome OutboxMessages pendentes
    ├── envia via SMTP (MailKit)
    └── atualiza status + publica evento SSE
```

---

## Arquitetura

A solução segue **Clean Architecture** com separação clara em quatro camadas:

```
notX.Domain          → entidades, enums, regras de negócio puras
notX.Application     → casos de uso (Commands/Queries), interfaces, validação
notX.Infrastructure  → repositórios, migrations, SMTP, Redis, Realtime
notX.Api             → controllers, middleware, SSE, DI root
notX.EmailWorker     → BackgroundService que processa o Outbox
```

Nenhuma camada interna conhece a camada externa. A `Application` define interfaces (`IEmailService`, `INotificationRepository`) que a `Infrastructure` implementa — a inversão de dependência é resolvida pelo container de DI.

---

## Projetos da solução

| Projeto | Tipo | Responsabilidade |
|---|---|---|
| `notX.Domain` | Class Library | Entidades (`Notification`, `OutboxMessage`, `Application`), enums e regras de domínio |
| `notX.Application` | Class Library | Commands/Queries com MediatR, validação com FluentValidation, interfaces de repositório e serviços |
| `notX.Infrastructure` | Class Library | Repositórios com Dapper, migrations com FluentMigrator, `EmailService` (MailKit), `DashboardEvents` (Redis Pub/Sub) |
| `notX.Api` | ASP.NET Core Web API | Controllers REST, middleware de API key, SSE endpoint, SPA estática |
| `notX.EmailWorker` | Worker Service | Loop de processamento do Outbox com distributed lock via Redis |
| `notX.AppHost` | .NET Aspire AppHost | Orquestração local: PostgreSQL, Redis, Api, Worker com referências e health checks |
| `notX.ServiceDefaults` | Class Library | Configurações padrão de observabilidade (OpenTelemetry, health checks) via .NET Aspire |
| `notX.Shared` | Class Library | `Result<T>`, `Error`, `PagedResult<T>` — tipos utilitários sem dependências |

---

## Conceitos e decisões técnicas

### Clean Architecture

A separação em camadas concêntricas garante que as regras de negócio não dependam de frameworks, bancos de dados ou protocolos de rede. O domínio pode ser testado isoladamente sem subir infraestrutura. A regra de ouro: dependências sempre apontam para dentro (em direção ao domínio).

### CQRS com MediatR

Commands e Queries são classes separadas com handlers dedicados. Um `CreateNotificationCommand` sabe apenas o que precisa para criar uma notificação; o controller não conhece nenhum serviço diretamente — despacha para o MediatR e recebe um `Result<T>`. Isso simplifica testes e permite adicionar behaviors (ex.: `ValidationBehavior`) de forma transparente.

### Outbox Pattern

O problema que o Outbox resolve: se a API enviasse o e-mail diretamente dentro do request HTTP, uma falha de rede entre gravar no banco e enviar o e-mail resultaria em dados inconsistentes (notificação gravada, e-mail não enviado — ou vice-versa).

Com o Outbox, a API grava a `Notification` e uma `OutboxMessage` na **mesma transação de banco**. O Worker lê o Outbox em background e faz o envio. Dessa forma, ou os dois são gravados ou nenhum é — a atomicidade do PostgreSQL garante a consistência.

```
API (request HTTP)
├── BEGIN TRANSACTION
├── INSERT INTO notifications (...)
├── INSERT INTO outbox_messages (...)
└── COMMIT

Worker (background)
├── SELECT outbox_messages WHERE processed_at IS NULL
├── Envia e-mail / SMS
├── UPDATE notifications SET status = 'Sent'
└── UPDATE outbox_messages SET processed_at = NOW()
```

### Distributed Lock com Redis

O Worker usa `SET notx:outbox:lock <token> EX 60 NX` — operação atômica do Redis — para garantir que apenas uma instância do Worker processe o Outbox por vez. Isso permite escalar horizontalmente o Worker sem risco de processamento duplicado.

O token é um `Guid` gerado a cada ciclo. Antes de liberar o lock, o Worker verifica se o token ainda é o dele (evitando que uma instância lenta libere o lock de outra que já o adquiriu após o TTL expirar).

### Server-Sent Events (SSE)

SSE é um protocolo unidirecional sobre HTTP/1.1 onde o servidor mantém a conexão aberta e empurra eventos formatados como `text/event-stream`. É mais simples que WebSockets quando o cliente só precisa ouvir (não enviar).

O fluxo aqui usa Redis Pub/Sub como camada intermediária: o Worker publica eventos no canal `notx:events:app:{applicationId}` e o endpoint SSE da API subscreve esse canal, repassando cada evento para o browser conectado. Isso significa que a API e o Worker podem rodar em máquinas diferentes sem acoplamento direto.

Cada aplicação pode abrir até 5 conexões SSE simultâneas (controlado pelo `SseConnectionLimiter` em memória). Keepalives são enviados a cada 20s para evitar que proxies fechem a conexão por timeout.

### Result Pattern

Em vez de lançar exceções para controle de fluxo (o que é caro e obscurece a intenção), os handlers retornam `Result<T>`. O controller verifica `result.IsFailure` e decide o status HTTP adequado. Isso torna o caminho de erro explícito e rastreável.

### Validação com FluentValidation + Pipeline Behavior

O `ValidationBehavior<TRequest, TResponse>` é registrado como pipeline behavior do MediatR. Toda requisição passa por ele antes de chegar ao handler. Se houver erros de validação, retorna `Result.Failure` sem nem executar o handler — separação limpa entre validação e lógica de negócio.

### Dapper + SQL explícito

A escolha por Dapper (ao invés de EF Core) é intencional: queries SQL ficam visíveis, previsíveis e fáceis de otimizar. Os repositórios separam as queries em arquivos `*.Sql.cs` (classes parciais com as strings SQL como constantes), mantendo o código de acesso a dados organizado sem o overhead de um ORM pesado.

### .NET Aspire

O `notX.AppHost` usa .NET Aspire para orquestração local. Com um único `dotnet run`, ele sobe PostgreSQL e Redis em containers, inicia a API e o Worker com as referências corretas, e abre o painel de observabilidade do Aspire com logs, traces e métricas de todos os serviços.

---

## Fluxo de uma notificação

```
1. POST /notifications
   └── ApiKeyMiddleware valida X-Api-Key e resolve o ApplicationId

2. CreateNotificationCommandHandler
   ├── Cria Notification (status: Pending)
   ├── Cria OutboxMessage com payload JSON
   └── Grava ambos atomicamente no PostgreSQL

3. NotificationsController
   └── Publica evento "notification.created" no Redis

4. SSE → browser recebe "notification.created" em tempo real

5. Worker (a cada 10s)
   ├── Tenta adquirir distributed lock via Redis
   ├── Busca OutboxMessages pendentes (batch de 50)
   ├── Para cada mensagem:
   │   ├── Atualiza Notification para Processing
   │   ├── Envia via SMTP (Email) ou gateway SMS (SMS)
   │   ├── Em sucesso: marca Sent, marca Outbox como processed
   │   ├── Em falha: incrementa RetryCount
   │   └── Após 3 falhas: marca Notification como Failed
   └── Libera o lock

6. Worker publica "notification.status_changed" no Redis
   └── SSE → browser atualiza o dashboard em tempo real
```

---

## API

A documentação interativa completa está disponível em `/scalar/v1` quando rodando em modo Development.

**Autenticação:** todas as rotas (exceto `/applications` e `/health`) exigem o header `X-Api-Key` com a key da aplicação.

### Aplicações

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/applications` | Cria uma aplicação e retorna a API key gerada |

### Notificações

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/notifications` | Cria uma notificação individual |
| `POST` | `/notifications/batch` | Cria até 1000 notificações atomicamente |
| `GET` | `/notifications` | Lista notificações com filtros e paginação |
| `POST` | `/notifications/{id}/cancel` | Cancela uma notificação não enviada |
| `POST` | `/notifications/{id}/retry` | Recoloca uma notificação falha/cancelada na fila |

### Eventos em tempo real

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/events/dashboard` | Stream SSE com eventos e métricas do dashboard |

**Eventos SSE emitidos:**

- `notification.created` — nova notificação criada
- `notification.status_changed` — status alterado (cancelled, retry, processing, sent, failed)
- `metrics.snapshot` — snapshot completo de métricas (emitido ao conectar e a cada 5s)

---

## Configuração e variáveis de ambiente

Copie `.env.example` para `.env` e preencha os valores:

```bash
cp .env.example .env
```

| Variável | Descrição |
|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string do PostgreSQL |
| `SMTP_HOST` | Host do servidor SMTP |
| `SMTP_PORT` | Porta SMTP (padrão: 587) |
| `SMTP_ENABLE_SSL` | Habilitar STARTTLS (padrão: true) |
| `SMTP_USERNAME` | Usuário SMTP |
| `SMTP_PASSWORD` | Senha SMTP (para Gmail, use senha de app) |
| `SMTP_FROM_NAME` | Nome exibido no campo "De:" |
| `SMTP_FROM_EMAIL` | E-mail exibido no campo "De:" |
| `Twilio__AccountSid` | Account SID da sua conta Twilio |
| `Twilio__AuthToken` | Auth Token da sua conta Twilio |
| `Twilio__FromNumber` | Número Twilio no formato E.164 (ex: +5511999999999) |

**Gmail:** para usar o Gmail como SMTP, ative autenticação de dois fatores na sua conta e gere uma "Senha de app" em `Configurações → Segurança`. Use essa senha gerada em `SMTP_PASSWORD`, não a sua senha normal.

---

## Rodando localmente

### Com .NET Aspire (recomendado)

Requer .NET 9 SDK e Docker rodando.

```bash
dotnet run --project src/notX.AppHost
```

O Aspire sobe PostgreSQL e Redis automaticamente, inicia a API e o Worker, e abre o painel de observabilidade. A API fica disponível em `https://localhost:{porta}` e a documentação em `/scalar/v1`.

### Com Docker Compose

```bash
# Sobe PostgreSQL e Redis
docker-compose up -d postgres redis

# Roda a API
dotnet run --project src/notX.Api

# Roda o Worker (terminal separado)
dotnet run --project src/notX.EmailWorker
```

### Worker em container

```bash
docker-compose up -d
```

O `docker-compose.yml` inclui o `email-worker` como serviço com `restart: unless-stopped`.

---

## Banco de dados e migrações

As migrações são aplicadas automaticamente na inicialização da API em ambiente Development (`MigrationExtensions.ApplyMigrations`). Em produção, execute manualmente ou via pipeline CI/CD.

As migrações ficam em `src/notX.Infrastructure/Persistence/Migrations/` e usam FluentMigrator. A ordem de execução é determinada pelo prefixo numérico do nome da classe.

---

## Próximos passos

### Envio de SMS via Twilio

A infraestrutura para SMS já está parcialmente preparada: o enum `NotificationType.Sms` existe, as credenciais Twilio estão no `.env.example` e a classe `TwilioSettings` está mapeada. O que falta:

1. Criar `ISmsService` em `notX.Application/Interfaces/`
2. Implementar `SmsService` em `notX.Infrastructure/Services/` usando o SDK oficial do Twilio (`Twilio.Rest.Api.V2010.Account.MessageResource.CreateAsync`)
3. Registrar `ISmsService` no container em `InfrastructureServiceCollectionExtensions`
4. Adicionar o `SmsWorker` (ou expandir o `EmailWorker`) para despachar notificações do tipo `Sms` — o `Worker` já tem o branch `else` que hoje apenas loga "not supported"
5. Adicionar `Twilio__AccountSid`, `Twilio__AuthToken` e `Twilio__FromNumber` ao `docker-compose.yml` e ao `AppHost`

### Kubernetes

Com a plataforma estabilizada, o deploy em Kubernetes segue essa estrutura mínima:

**Deployments:**
- `notx-api` — 2+ réplicas, HorizontalPodAutoscaler baseado em CPU/RPS
- `notx-emailworker` — 1 réplica por enquanto (o distributed lock via Redis já garante consistência com múltiplas réplicas se necessário)

**Services:**
- `notx-api-svc` — ClusterIP exposto via Ingress (nginx ou Traefik)

**Ingress:**
- TLS terminado no Ingress Controller, rota `/` para a SPA, `/api` para a API

**ConfigMaps e Secrets:**
- `notx-config` — variáveis não sensíveis (portas, hosts)
- `notx-secrets` — SMTP credentials, Twilio tokens, connection string do banco (usar External Secrets Operator em produção para sincronizar com AWS Secrets Manager ou Vault)

**Infraestrutura gerenciada (recomendado para produção):**
- PostgreSQL → RDS ou Cloud SQL (fora do cluster)
- Redis → ElastiCache ou Memorystore (fora do cluster)

**Observabilidade:**
- O `notX.ServiceDefaults` já instrumenta OpenTelemetry. Basta apontar o `OTEL_EXPORTER_OTLP_ENDPOINT` para um Jaeger, Grafana Tempo ou Honeycomb e os traces aparecem automaticamente.
