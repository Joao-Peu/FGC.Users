# FGC.Users

Microservice Users - .NET 8 API

Running locally (Docker):

- Start docker compose: `docker compose up --build`
- API will be available at `http://localhost:5000`

Environment variables (examples):
- ConnectionStrings__Default
- Jwt__Issuer
- Jwt__Audience
- Jwt__Secret
- ServiceBus__ConnectionString (optional)
- OpenTelemetry__OtlpEndpoint (optional)

Endpoints:
- POST /api/users/register
- POST /api/users/login
- GET /api/users/me (requires JWT)
- PUT /api/users/me (requires JWT)
- GET /health
- GET /ready

Audit log:
All changes to users create a record in the `AuditEvents` table with fields: Id, AggregateType, AggregateId, EventType, BeforeJson, AfterJson, CreatedAtUtc, CorrelationId, TraceId, UserId.

Publishing events:
The service will publish `UserRegistered` and `UserProfileUpdated` to Service Bus topic named same as event if a ServiceBus connection string is provided. Otherwise publishes to in-memory logger.

