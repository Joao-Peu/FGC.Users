# Guia Rápido de Testes - FGC.Users

## 🚀 Começar Rápido

### 1. **Rodar todos os testes**
```bash
dotnet test
```

### 2. **Rodar testes com verbose output**
```bash
dotnet test --verbosity detailed
```

### 3. **Rodar testes de um arquivo específico**
```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests"
```

### 4. **Rodar um teste específico**
```bash
dotnet test --filter "Name=RegisterAsync_WithValidRequest_ShouldCreateUser"
```

## 📊 Cobertura de Testes Implementados

### ✅ UserServiceTests (11 testes)
Testam toda a lógica de negócio do serviço de usuários:

| Método | Teste | Status |
|--------|-------|--------|
| **RegisterAsync** | Deve criar novo usuário | ✓ |
| | Deve falhar se email existe | ✓ |
| | Deve fazer auditoria e publicar eventos | ✓ |
| **LoginAsync** | Deve retornar token com credenciais válidas | ✓ |
| | Deve falhar com email inválido | ✓ |
| | Deve falhar com senha errada | ✓ |
| **GetMeAsync** | Deve retornar usuário válido | ✓ |
| | Deve falhar se usuário não existe | ✓ |
| **UpdateMeAsync** | Deve atualizar usuário com sucesso | ✓ |
| | Deve falhar se usuário não existe | ✓ |
| | Deve fazer auditoria e publicar eventos | ✓ |

### ✅ RegisterValidatorTests (14 testes)
Testam validação de dados de entrada:

| Validação | Teste | Status |
|-----------|-------|--------|
| **Email** | Email válido | ✓ |
| | Email vazio | ✓ |
| | Formato inválido | ✓ |
| | Múltiplos formatos válidos | ✓ |
| **Password** | Senha válida | ✓ |
| | Senha vazia | ✓ |
| | Senha curta | ✓ |
| | Múltiplas senhas válidas | ✓ |
| **Validação Combinada** | Todos os campos válidos | ✓ |
| | Múltiplos campos inválidos | ✓ |

## 🛠️ Estrutura do Projeto

```
FGC.Users.Tests/
├── Services/
│   └── UserServiceTests.cs           # 11 testes para UserService
├── Validators/
│   └── RegisterValidatorTests.cs     # 14 testes para RegisterValidator
├── Fixtures/
│   ├── UserServiceFixture.cs         # Mocks reutilizáveis (legacy)
│   └── TestData.cs                   # Dados de teste centralizados
├── FGC.Users.Tests.csproj            # Configuração do projeto
└── README.md                          # Documentação completa
```

## 📝 Padrões de Teste Utilizados

### 1. **AAA Pattern (Arrange-Act-Assert)**
```csharp
[Fact]
public async Task MethodName_Condition_ExpectedResult()
{
    // Arrange - Preparar dados e mocks
    var request = TestData.Requests.ValidRegisterRequest;
    
    // Act - Executar a ação
    var result = await _userService.RegisterAsync(request, correlationId);
    
    // Assert - Verificar o resultado
    result.Should().NotBeNull();
}
```

### 2. **Fixtures e Dados Reutilizáveis**
```csharp
public static class TestData
{
    public static RegisterRequest ValidRegisterRequest => new(
        Email: "newuser@example.com",
        Password: "ValidPassword123!",
        FullName: "New User"
    );
}
```

### 3. **InMemory Database para Testes**
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

var dbContext = new ApplicationDbContext(options);
```

### 4. **Mocks com Moq**
```csharp
var mockAuditService = new Mock<IAuditService>();
var mockEventPublisher = new Mock<IEventPublisher>();

// Verificar se foi chamado
mockAuditService.Verify(x => x.AuditAsync(...), Times.Once);
```

## 💡 Como Adicionar Novos Testes

### Passo 1: Criar nova classe de teste
```csharp
public class NewServiceTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ISomeService> _mockService;
    private readonly NewService _service;

    public NewServiceTests()
    {
        // Setup...
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }
}
```

### Passo 2: Adicionar método de teste
```csharp
[Fact]
public async Task MethodName_Condition_ShouldExpectation()
{
    // Arrange
    var input = TestData.GetInput();
    
    // Act
    var result = await _service.MethodAsync(input);
    
    // Assert
    result.Should().NotBeNull();
}
```

### Passo 3: Rodar e validar
```bash
dotnet test --filter "FullyQualifiedName~NewServiceTests"
```

## 🔍 Depuração de Testes

### Ver logs de um teste específico
```bash
dotnet test --filter "Name=MyTestName" --logger "console;verbosity=detailed"
```

### Rodar teste no debugger (Visual Studio)
1. Abra o Test Explorer (View > Test Explorer)
2. Clique com botão direito no teste
3. Selecione "Debug Selected Tests"

### Rodar teste no VS Code
1. Instale a extensão ".NET Test Explorer"
2. Clique no ícone de play próximo ao teste

## ⚠️ Boas Práticas

✅ **FAÇA:**
- Um conceito por teste
- Nomes descritivos: `MethodName_Condition_ExpectedResult`
- Use `Fact` para testes sem parâmetros
- Use `Theory` + `InlineData` para múltiplos cenários
- Isole dados de cada teste (use `IAsyncLifetime`)
- Reutilize `TestData` para evitar duplicação
- Teste comportamento, não implementação

❌ **NÃO FAÇA:**
- Testes que dependem um do outro
- Use `Thread.Sleep()` em testes assíncronos
- Testes muito grandes (difíceis de manter)
- Teste detalhes de implementação interna
- Deixe testes pendentes sem remover

## 📈 Próximas Melhorias

- [ ] Testes de integração com SQL Server real
- [ ] Testes para Controllers (UsersController)
- [ ] Testes para AuditService
- [ ] Testes para EventPublishers
- [ ] Benchmarks de performance
- [ ] GitHub Actions com relatório de cobertura

---

**Total de Testes:** 25  
**Taxa de Sucesso:** 100%  
**Última atualização:** 2 de março de 2026
