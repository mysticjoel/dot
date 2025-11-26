# WebApiTemplate.Tests

Unit tests for the BidSphere Auction Management System.

## Test Coverage

### ✅ Extension Methods (100% Coverage)
- **DateTimeExtensions** - 8 tests
- **DecimalExtensions** - 6 tests  
- **ClaimsPrincipalExtensions** - 11 tests

### ✅ Services (Critical Business Logic)
- **DashboardService** - 6 tests
- **AuthService** - 8 tests

### ✅ Filters (Cross-Cutting Concerns)
- **ActivityLoggingFilter** - 3 tests
- **ValidateModelStateFilter** - 4 tests

## Running Tests

### Run All Tests
```bash
cd WebApiTemplate.Tests
dotnet test
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true
```

### Run Specific Test Class
```bash
dotnet test --filter FullyQualifiedName~DateTimeExtensionsTests
```

### Run in Watch Mode
```bash
dotnet watch test
```

## Test Frameworks & Libraries

- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **EF Core InMemory** - In-memory database for testing

## Test Structure

```
WebApiTemplate.Tests/
├── Extensions/
│   ├── DateTimeExtensionsTests.cs
│   ├── DecimalExtensionsTests.cs
│   └── ClaimsPrincipalExtensionsTests.cs
├── Services/
│   ├── DashboardServiceTests.cs
│   └── AuthServiceTests.cs
├── Filters/
│   ├── ActivityLoggingFilterTests.cs
│   └── ValidateModelStateFilterTests.cs
├── GlobalUsings.cs
├── WebApiTemplate.Tests.csproj
└── README.md
```

## Writing New Tests

### Example Test
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = "test";

    // Act
    var result = await _service.MethodAsync(input);

    // Assert
    result.Should().NotBeNull();
    result.Should().Be("expected");
}
```

### Using Theory for Multiple Cases
```csharp
[Theory]
[InlineData(10, true)]
[InlineData(0, false)]
[InlineData(-5, false)]
public void IsPositive_WithVariousInputs_ReturnsExpected(decimal input, bool expected)
{
    // Act
    var result = input.IsPositive();

    // Assert
    result.Should().Be(expected);
}
```

## Best Practices

1. **AAA Pattern**: Arrange, Act, Assert
2. **One Assert Per Test**: Focus on single behavior
3. **Descriptive Names**: `MethodName_Scenario_ExpectedResult`
4. **Independent Tests**: No dependencies between tests
5. **Mock External Dependencies**: Use in-memory DB, mock services
6. **Clean Up**: Dispose resources properly

## Test Categories

- **Unit Tests**: Test single methods in isolation
- **Integration Tests**: Test multiple components together (not included yet)
- **Fast Tests**: Run quickly (< 100ms each)
- **Reliable Tests**: Consistent results every time

## Coverage Goals

✅ **Extension Methods**: 100% (Pure functions)  
✅ **Critical Services**: 80%+ (DashboardService, AuthService)  
✅ **Filters**: 80%+ (ActivityLogging, ModelValidation)  
⚠️ **Controllers**: Not tested (thin layer, minimal logic)  
⚠️ **Database Operations**: Tested via services

## CI/CD Integration

Add to your CI/CD pipeline:
```yaml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal

- name: Generate Coverage
  run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Future Enhancements

- [ ] Integration tests for controllers
- [ ] Performance tests for dashboard queries
- [ ] Load tests for concurrent bid placement
- [ ] E2E tests with TestContainers (PostgreSQL)
- [ ] Increase coverage to 90%+

---

**Total Tests**: 46  
**Test Execution Time**: < 5 seconds  
**Status**: ✅ All Passing

