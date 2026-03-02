# FGC.Users.Tests

Projeto de testes unitários para a aplicação FGC.Users, seguindo os padrões de **Clean Architecture** com **xUnit**, **Moq** e **FluentAssertions**.

## 📋 Estrutura

```
FGC.Users.Tests/
├── Services/
│   └── UserServiceTests.cs           # Testes para UserService
├── Validators/
│   └── RegisterValidatorTests.cs     # Testes para RegisterValidator
└── Fixtures/
    ├── UserServiceFixture.cs         # Fixture com mocks pré-configurados
    └── TestData.cs                   # Dados de teste reutilizáveis
```

## 🧪 Cobertura de Testes

### UserService
- ✅ `RegisterAsync` - Criar novo usuário com sucesso
- ✅ `RegisterAsync` - Falhar se email já existe
- ✅ `RegisterAsync` - Auditoria e publicação de eventos
- ✅ `LoginAsync` - Login com credenciais válidas
- ✅ `LoginAsync` - Falhar com email inválido
- ✅ `LoginAsync` - Falhar com senha errada
- ✅ `GetMeAsync` - Retornar usuário válido
- ✅ `GetMeAsync` - Lançar exceção se usuário não existe
- ✅ `UpdateMeAsync` - Atualizar usuário com sucesso
- ✅ `UpdateMeAsync` - Falhar se usuário não existe
- ✅ `UpdateMeAsync` - Auditoria e publicação de eventos

### RegisterValidator
- ✅ Email válido e inválido
- ✅ Senha com comprimento mínimo
- ✅ Validação combinada
- ✅ Casos de teste com Theory (InlineData)

## 🚀 Como Executar

### Executar todos os testes
```bash
dotnet test
```

### Executar testes com verbose output
```bash
dotnet test --verbosity detailed
```

### Executar testes de um arquivo específico
```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests"
```

### Executar com cobertura de código (requer OpenCover ou similar)
```bash
dotnet test /p:CollectCoverage=true
```

### Executar testes no Visual Studio
- Abra o **Test Explorer** (View > Test Explorer ou Ctrl+E, T)
- Click em "Run All Tests" ou execute testes específicos

## 📚 Padrões Utilizados

### Arrange-Act-Assert (AAA)
Todos os testes seguem o padrão AAA para melhor legibilidade:
```csharp
[Fact]
public async Task RegisterAsync_WithValidRequest_ShouldCreateUser()
{
    // Arrange - Preparar dados de teste
    var request = TestData.Requests.ValidRegisterRequest;
    
    // Act - Executar a ação
    var result = await _userService.RegisterAsync(request, correlationId);
    
    // Assert - Verificar o resultado
    result.Should().NotBeNull();
}
```

### Mocks e Fixtures
Reutilização de mocks através de fixtures:
```csharp
public UserServiceTests()
{
    _fixture = new UserServiceFixture();
    _userService = new UserService(
        _fixture.MockDbContext.Object,
        _fixture.MockAuditService.Object,
        // ...
    );
}
```

### Data Builders
Dados de teste centralizados e reutilizáveis:
```csharp
public static RegisterRequest ValidRegisterRequest => new(
    Email: "newuser@example.com",
    Password: "ValidPassword123!",
    FullName: "New User"
);
```

## 🔧 Tecnologias

| Ferramenta | Versão | Propósito |
|-----------|--------|----------|
| **xUnit** | 2.7.0 | Framework de testes |
| **Moq** | 4.20.70 | Mocking de dependências |
| **FluentAssertions** | 6.12.0 | Assertions fluentes e legíveis |
| **FluentValidation** | 11.9.2 | Testes de validação |

## 💡 Boas Práticas

✅ **Um conceito por teste** - Cada teste valida um único comportamento  
✅ **Nomes descritivos** - `RegisterAsync_WithValidRequest_ShouldCreateUser`  
✅ **Dados isolados** - Cada teste possui seus próprios dados  
✅ **Mocks, não stubs** - Verificação de comportamento com Moq  
✅ **Sem dependências reais** - Todos os serviços são mockados  
✅ **Fixtures reutilizáveis** - Código DRY nos testes  

## 📝 Próximas Melhorias

- [ ] Testes de integração com banco de dados real
- [ ] Testes para Controllers (UsersController)
- [ ] Testes para AuditService
- [ ] Testes para EventPublishers (InMemory e ServiceBus)
- [ ] Benchmarks de performance
- [ ] CI/CD com cobertura de código obrigatória

## 🤝 Contribuindo

Ao adicionar novos testes:
1. Siga o padrão AAA
2. Use nomes descritivos
3. Reutilize TestData e Fixtures
4. Mantenha testes independentes
5. Adicione comentários para lógica complexa

---

**Última atualização:** 2 de março de 2026
