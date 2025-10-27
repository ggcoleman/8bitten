using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using EightBitten.Infrastructure.Platform.Audio;

namespace EightBitten.Tests.Unit.Infrastructure.Platform.Audio;

/// <summary>
/// Unit tests for NAudio audio renderer implementation
/// Tests audio output, buffer management, and audio pipeline
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test class implements IDisposable and properly disposes in Dispose method")]
public sealed class NAudioRendererTests : IDisposable
{
    private readonly NAudioRenderer _renderer;
    private readonly ILogger<NAudioRenderer> _logger;

    public NAudioRendererTests()
    {
        _logger = NullLogger<NAudioRenderer>.Instance;
        _renderer = new NAudioRenderer(_logger);
    }

    [Fact]
    public void ConstructorShouldInitializeWithValidLogger()
    {
        // Arrange & Act
        var renderer = new NAudioRenderer(_logger);

        // Assert
        renderer.Should().NotBeNull();
        renderer.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void ConstructorWithNullLoggerShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new NAudioRenderer(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void InitializeShouldSetupAudioDevice()
    {
        // Arrange
        var sampleRate = 44100;
        var channels = 2;
        var bufferSize = 1024;

        // Act
        var result = _renderer.Initialize(sampleRate, channels, bufferSize);

        // Assert
        result.Should().BeTrue();
        _renderer.IsInitialized.Should().BeTrue();
        _renderer.SampleRate.Should().Be(sampleRate);
        _renderer.Channels.Should().Be(channels);
        _renderer.BufferSize.Should().Be(bufferSize);
    }

    [Theory]
    [InlineData(0, 2, 1024)]
    [InlineData(44100, 0, 1024)]
    [InlineData(44100, 2, 0)]
    [InlineData(-1, 2, 1024)]
    public void InitializeWithInvalidParametersShouldReturnFalse(int sampleRate, int channels, int bufferSize)
    {
        // Act
        var result = _renderer.Initialize(sampleRate, channels, bufferSize);

        // Assert
        result.Should().BeFalse();
        _renderer.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void PlayAudioShouldProcessSampleData()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);
        var samples = new float[1024];
        
        // Fill with test sine wave
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)Math.Sin(2 * Math.PI * 440 * i / 44100.0); // 440Hz tone
        }

        // Act
        var result = _renderer.PlayAudio(samples);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PlayAudioWithoutInitializationShouldReturnFalse()
    {
        // Arrange
        var samples = new float[1024];

        // Act
        var result = _renderer.PlayAudio(samples);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PlayAudioWithNullDataShouldThrowArgumentNullException()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);

        // Act & Assert
        var action = () => _renderer.PlayAudio(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("samples");
    }

    [Fact]
    public void SetVolumeShouldUpdateAudioVolume()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);
        var volume = 0.5f;

        // Act
        _renderer.SetVolume(volume);

        // Assert
        _renderer.Volume.Should().Be(volume);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void SetVolumeWithInvalidValueShouldThrowArgumentException(float volume)
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);

        // Act & Assert
        var action = () => _renderer.SetVolume(volume);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartPlaybackShouldBeginAudioOutput()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);

        // Act
        var result = _renderer.StartPlayback();

        // Assert
        result.Should().BeTrue();
        _renderer.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void StopPlaybackShouldEndAudioOutput()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);
        _renderer.StartPlayback();

        // Act
        _renderer.StopPlayback();

        // Assert
        _renderer.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void PausePlaybackShouldSuspendAudioOutput()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);
        _renderer.StartPlayback();

        // Act
        _renderer.PausePlayback();

        // Assert
        _renderer.IsPaused.Should().BeTrue();
        _renderer.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void ResumePlaybackShouldContinueAudioOutput()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);
        _renderer.StartPlayback();
        _renderer.PausePlayback();

        // Act
        _renderer.ResumePlayback();

        // Assert
        _renderer.IsPaused.Should().BeFalse();
        _renderer.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void GetLatencyShouldReturnAudioLatency()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);

        // Act
        var latency = _renderer.GetLatency();

        // Assert
        latency.Should().BeGreaterThan(TimeSpan.Zero);
        latency.Should().BeLessThan(TimeSpan.FromMilliseconds(100)); // Reasonable upper bound
    }

    [Fact]
    public void GetBufferUsageShouldReturnCurrentBufferLevel()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);

        // Act
        var usage = _renderer.GetBufferUsage();

        // Assert
        usage.Should().BeInRange(0.0f, 1.0f);
    }

    [Fact]
    public void DisposeShouldCleanupResources()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);
        _renderer.StartPlayback();

        // Act
        _renderer.Dispose();

        // Assert
        _renderer.IsInitialized.Should().BeFalse();
        _renderer.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void MultipleDisposeShouldNotThrow()
    {
        // Arrange
        _renderer.Initialize(44100, 2, 1024);

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
