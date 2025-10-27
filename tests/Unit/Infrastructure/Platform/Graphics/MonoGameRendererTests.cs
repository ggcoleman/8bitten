using System;
using System.Drawing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using EightBitten.Infrastructure.Platform.Graphics;
using EightBitten.Core.PPU;

namespace EightBitten.Tests.Unit.Infrastructure.Platform.Graphics;

/// <summary>
/// Unit tests for MonoGame graphics renderer implementation
/// Tests graphics output, frame buffer management, and rendering pipeline
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test class implements IDisposable and properly disposes in Dispose method")]
public sealed class MonoGameRendererTests : IDisposable
{
    private readonly MonoGameRenderer _renderer;
    private readonly ILogger<MonoGameRenderer> _logger;

    public MonoGameRendererTests()
    {
        _logger = NullLogger<MonoGameRenderer>.Instance;
        _renderer = new MonoGameRenderer(_logger);
    }

    [Fact]
    public void ConstructorShouldInitializeWithValidLogger()
    {
        // Arrange & Act
        var renderer = new MonoGameRenderer(_logger);

        // Assert
        renderer.Should().NotBeNull();
        renderer.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void ConstructorWithNullLoggerShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new MonoGameRenderer(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void InitializeShouldSetupGraphicsDevice()
    {
        // Arrange
        var width = 256;
        var height = 240;

        // Act
        var result = _renderer.Initialize(width, height);

        // Assert
        result.Should().BeTrue();
        _renderer.IsInitialized.Should().BeTrue();
        _renderer.ScreenWidth.Should().Be(width);
        _renderer.ScreenHeight.Should().Be(height);
    }

    [Theory]
    [InlineData(0, 240)]
    [InlineData(256, 0)]
    [InlineData(-1, 240)]
    [InlineData(256, -1)]
    public void InitializeWithInvalidDimensionsShouldReturnFalse(int width, int height)
    {
        // Act
        var result = _renderer.Initialize(width, height);

        // Assert
        result.Should().BeFalse();
        _renderer.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void RenderFrameShouldProcessPixelData()
    {
        // Arrange
        _renderer.Initialize(256, 240);
        var frameData = new byte[256 * 240 * 4]; // RGBA format
        
        // Fill with test pattern
        for (int i = 0; i < frameData.Length; i += 4)
        {
            frameData[i] = 255;     // R
            frameData[i + 1] = 0;   // G
            frameData[i + 2] = 0;   // B
            frameData[i + 3] = 255; // A
        }

        // Act
        var result = _renderer.RenderFrame(frameData);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RenderFrameWithoutInitializationShouldReturnFalse()
    {
        // Arrange
        var frameData = new byte[256 * 240 * 4];

        // Act
        var result = _renderer.RenderFrame(frameData);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RenderFrameWithNullDataShouldThrowArgumentNullException()
    {
        // Arrange
        _renderer.Initialize(256, 240);

        // Act & Assert
        var action = () => _renderer.RenderFrame(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("frameData");
    }

    [Fact]
    public void RenderFrameWithInvalidDataSizeShouldReturnFalse()
    {
        // Arrange
        _renderer.Initialize(256, 240);
        var invalidFrameData = new byte[100]; // Too small

        // Act
        var result = _renderer.RenderFrame(invalidFrameData);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetScalingShouldUpdateRenderScale()
    {
        // Arrange
        _renderer.Initialize(256, 240);
        var scale = 2.0f;

        // Act
        _renderer.SetScaling(scale);

        // Assert
        _renderer.RenderScale.Should().Be(scale);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void SetScalingWithInvalidValueShouldThrowArgumentException(float scale)
    {
        // Arrange
        _renderer.Initialize(256, 240);

        // Act & Assert
        var action = () => _renderer.SetScaling(scale);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnableVSyncShouldUpdateVSyncState()
    {
        // Arrange
        _renderer.Initialize(256, 240);

        // Act
        _renderer.EnableVSync(true);

        // Assert
        _renderer.IsVSyncEnabled.Should().BeTrue();

        // Act
        _renderer.EnableVSync(false);

        // Assert
        _renderer.IsVSyncEnabled.Should().BeFalse();
    }

    [Fact]
    public void GetFrameBufferShouldReturnCurrentFrameData()
    {
        // Arrange
        _renderer.Initialize(256, 240);
        var frameData = new byte[256 * 240 * 4];
        _renderer.RenderFrame(frameData);

        // Act
        var buffer = _renderer.GetFrameBuffer();

        // Assert
        buffer.Should().NotBeNull();
        buffer.Length.Should().Be(256 * 240 * 4);
    }

    [Fact]
    public void GetFrameBufferWithoutRenderShouldReturnEmptyBuffer()
    {
        // Arrange
        _renderer.Initialize(256, 240);

        // Act
        var buffer = _renderer.GetFrameBuffer();

        // Assert
        buffer.Should().NotBeNull();
        buffer.Length.Should().Be(256 * 240 * 4);
        buffer.Should().AllBeEquivalentTo((byte)0);
    }

    [Fact]
    public void DisposeShouldCleanupResources()
    {
        // Arrange
        _renderer.Initialize(256, 240);

        // Act
        _renderer.Dispose();

        // Assert
        _renderer.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void MultipleDisposeShouldNotThrow()
    {
        // Arrange
        _renderer.Initialize(256, 240);

        // Act & Assert
        var action = () =>
        {
            _renderer.Dispose();
            _renderer.Dispose();
        };
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        _renderer?.Dispose();
    }
}
