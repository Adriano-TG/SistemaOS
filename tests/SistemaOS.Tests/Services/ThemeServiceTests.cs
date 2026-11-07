using System.IO;
using FluentAssertions;
using Moq;
using SistemaOS.App.Services;
using Xunit;

namespace SistemaOS.Tests.Services;

public class ThemeServiceTests : IDisposable
{
    private readonly string _testFile;

    public ThemeServiceTests()
    {
        // Create a unique test file path for each test run
        _testFile = Path.Combine(Path.GetTempPath(), $"SistemaOS_tema_test_{Guid.NewGuid()}.json");
    }

    [Fact]
    public void Constructor_ShouldHaveDefaultConfig()
    {
        // Arrange & Act
        var service = new ThemeService();

        // Assert
        service.Config.Should().NotBeNull();
        service.Config.Tema.Should().Be("Dark"); // Default is dark theme
        service.Config.CorDestaque.Should().Be("#6C5CE7"); // Default accent color
    }

    [Fact]
    public void AlternarTema_ShouldToggleBetweenLightAndDark()
    {
        // Arrange
        var service = new ThemeService();

        // Act - first toggle: Dark -> Light
        service.AlternarTema();

        // Assert
        service.Config.Tema.Should().Be("Light");

        // Act again - second toggle: Light -> Dark
        service.AlternarTema();

        // Assert
        service.Config.Tema.Should().Be("Dark");
    }

    [Fact]
    public void DefinirTema_ShouldSetTheme()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.DefinirTema("Light");

        // Assert
        service.Config.Tema.Should().Be("Light");

        // Act
        service.DefinirTema("Blue");

        // Assert
        service.Config.Tema.Should().Be("Blue");
    }

    [Fact]
    public void DefinirCor_ShouldUpdateCorDestaque()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.DefinirCor("#FF0000");

        // Assert
        service.Config.CorDestaque.Should().Be("#FF0000");
    }

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#00FF00")]
    [InlineData("#0000FF")]
    [InlineData("#FFFFFF")]
    [InlineData("#000000")]
    [InlineData("#ABCDEF")]
    [InlineData("#123456")]
    public void DefinirCor_ShouldAcceptValidHexColors(string hex)
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.DefinirCor(hex);

        // Assert
        service.Config.CorDestaque.Should().Be(hex);
    }

    [Fact]
    public void Carregar_ShouldLoadFromFile_WhenFileExists()
    {
        // Arrange
        var config = new ConfiguracoesTema { Tema = "Light", CorDestaque = "#123456" };
        File.WriteAllText(_testFile, System.Text.Json.JsonSerializer.Serialize(config));

        var service = CreateServiceWithTestFile();

        // Act
        service.Carregar();

        // Assert
        service.Config.Tema.Should().Be("Light");
        service.Config.CorDestaque.Should().Be("#123456");
    }

    [Fact]
    public void Carregar_ShouldUseDefaults_WhenFileDoesNotExist()
    {
        // Arrange
        var service = CreateServiceWithTestFile(); // File doesn't exist

        // Act
        service.Carregar();

        // Assert
        service.Config.Tema.Should().Be("Dark");
        service.Config.CorDestaque.Should().Be("#6C5CE7");
    }

    [Fact]
    public void Carregar_ShouldUseDefaults_WhenFileIsCorrupted()
    {
        // Arrange
        File.WriteAllText(_testFile, "invalid json");
        var service = CreateServiceWithTestFile();

        // Act
        service.Carregar();

        // Assert
        service.Config.Tema.Should().Be("Dark");
        service.Config.CorDestaque.Should().Be("#6C5CE7");
    }

    [Fact]
    public void Carregar_ShouldMigrateOldConfig_WhenFileHasOldFormat()
    {
        // Arrange - old format with Escuro property
        var oldConfig = new { Escuro = false, CorDestaque = "#123456" };
        File.WriteAllText(_testFile, System.Text.Json.JsonSerializer.Serialize(oldConfig));

        var service = CreateServiceWithTestFile();

        // Act
        service.Carregar();

        // Assert - should migrate Escuro=false to Tema="Light"
        service.Config.Tema.Should().Be("Light");
        service.Config.CorDestaque.Should().Be("#123456");
    }

    [Fact]
    public void TemaMudado_ShouldBeRaised_WhenAlternarTema()
    {
        // Arrange
        var service = CreateServiceWithTestFile();
        var raised = false;
        service.TemaMudado += () => raised = true;

        // Act
        service.AlternarTema();

        // Assert
        raised.Should().BeTrue();
    }

    [Fact]
    public void TemaMudado_ShouldBeRaised_WhenDefinirTema()
    {
        // Arrange
        var service = CreateServiceWithTestFile();
        var raised = false;
        service.TemaMudado += () => raised = true;

        // Act
        service.DefinirTema("Light");

        // Assert
        raised.Should().BeTrue();
    }

    [Fact]
    public void TemaMudado_ShouldBeRaised_WhenDefinirCor()
    {
        // Arrange
        var service = CreateServiceWithTestFile();
        var raised = false;
        service.TemaMudado += () => raised = true;

        // Act
        service.DefinirCor("#FF0000");

        // Assert
        raised.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_ShouldBeRaised_WhenConfigChanges()
    {
        // Arrange
        var service = CreateServiceWithTestFile();
        string? changedProperty = null;
        service.PropertyChanged += (sender, e) => changedProperty = e.PropertyName;

        // Act
        service.AlternarTema();

        // Assert
        changedProperty.Should().Be(nameof(service.Config));
    }

    private ThemeService CreateServiceWithTestFile()
    {
        // Use reflection to set the private _arquivo field for testing
        var service = new ThemeService();
        var field = typeof(ThemeService).GetField("_arquivo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, _testFile);
        return service;
    }

    public void Dispose()
    {
        if (File.Exists(_testFile))
            File.Delete(_testFile);
    }
}