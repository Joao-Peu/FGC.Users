# FGC.Users

Microserviço de Usuários - API .NET 8

Executando localmente (Docker):

- Inicie o docker compose: `docker compose up --build`
- A API ficará disponível em `http://localhost:5000`

Variáveis de ambiente (exemplos):
- `ConnectionStrings__Default`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Secret`
- `ServiceBus__ConnectionString` (opcional)
- `OpenTelemetry__OtlpEndpoint` (opcional)

Endpoints:
- `POST /api/users/register` — registrar novo usuário
- `POST /api/users/login` — autenticar e obter token JWT
- `GET /api/users/me` — obter perfil do usuário (requer JWT)
- `PUT /api/users/me` — atualizar perfil do usuário (requer JWT)
- `GET /health` — verificação de saúde
- `GET /ready` — prontidão

Log de auditoria:
Todas as alterações em usuários criam um registro na tabela `AuditEvents` com os campos: `Id`, `AggregateType`, `AggregateId`, `EventType`, `BeforeJson`, `AfterJson`, `CreatedAtUtc`, `CorrelationId`, `TraceId`, `UserId`.

Publicação de eventos:
O serviço publicará os eventos `UserRegistered` e `UserProfileUpdated` em um tópico do Service Bus com o mesmo nome do evento se a conexão do Service Bus estiver configurada. Caso contrário, os eventos serão registrados em memória (logger).

Observações:
- Execute as migrações do EF Core com `dotnet ef database update` ou deixe a aplicação aplicar as migrações no startup, conforme configuração.
- Ajuste as variáveis de ambiente conforme seu ambiente local ou de produção.

