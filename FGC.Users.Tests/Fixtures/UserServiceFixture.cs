using Moq;
using FGC.Users.Infrastructure;
using FGC.Users.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FGC.Users.Tests.Fixtures;

/// <summary>
/// Fixture base para testes de UserService com mocks pré-configurados
/// </summary>
public class UserServiceFixture
{
    public Mock<ApplicationDbContext> MockDbContext { get; }
    public Mock<IAuditService> MockAuditService { get; }
    public Mock<IEventPublisher> MockEventPublisher { get; }
    public Mock<IConfiguration> MockConfiguration { get; }

    public UserServiceFixture()
    {
        MockDbContext = new Mock<ApplicationDbContext>();
        MockAuditService = new Mock<IAuditService>();
        MockEventPublisher = new Mock<IEventPublisher>();
        MockConfiguration = new Mock<IConfiguration>();

        // Configurar valores padrão para GetValue chamadas
        MockConfiguration
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key switch
            {
                "Jwt:Issuer" => "fgc.local",
                "Jwt:Audience" => "fgc.clients",
                "Jwt:Secret" => "your-super-secret-key-that-is-at-least-32-characters-long!!",
                "Jwt:SecretKey" => "your-super-secret-key-that-is-at-least-32-characters-long!!",
                _ => null
            });
    }

    /// <summary>
    /// Reseta todos os mocks para um estado limpo
    /// </summary>
    public void Reset()
    {
        MockDbContext.Reset();
        MockAuditService.Reset();
        MockEventPublisher.Reset();
    }
}
