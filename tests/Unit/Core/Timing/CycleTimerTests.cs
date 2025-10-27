using FluentAssertions;
using Xunit;
using System;
using EightBitten.Core.Timing;

namespace EightBitten.Tests.Unit.Core.Timing;

/// <summary>
/// Unit tests for cycle-accurate timing system
/// Tests NTSC timing (1.789773 MHz CPU clock) and cycle coordination
/// </summary>
public class CycleTimerTests
{
    private readonly CycleTimer _timer;

    public CycleTimerTests()
    {
        _timer = new CycleTimer();
    }

    [Fact]
    public void ConstructorShouldInitializeWithNTSCTiming()
    {
        // Act & Assert
        CycleTimer.CPUClockFrequency.Should().BeApproximately(1789773.0, 0.1);
        _timer.CycleCount.Should().Be(0);
        _timer.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void StartShouldSetRunningState()
    {
        // Act
        _timer.Start();

        // Assert
        _timer.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void StopShouldClearRunningState()
    {
        // Arrange
        _timer.Start();

        // Act
        _timer.Stop();

        // Assert
        _timer.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void ResetShouldResetCycleCount()
    {
        // Arrange
        _timer.Start();
        _timer.ExecuteCycles(100);

        // Act
        _timer.Reset();

        // Assert
        _timer.CycleCount.Should().Be(0);
        _timer.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void ExecuteCyclesWhenRunningShouldIncrementCycleCount()
    {
        // Arrange
        _timer.Start();

        // Act
        _timer.ExecuteCycles(10);

        // Assert
        _timer.CycleCount.Should().Be(10);
    }

    [Fact]
    public void ExecuteCyclesWhenNotRunningShouldThrowException()
    {
        // Act & Assert
        var action = () => _timer.ExecuteCycles(10);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Timer is not running");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void ExecuteCyclesWithVariousCountsShouldAccumulateCorrectly(int cycles)
    {
        // Arrange
        _timer.Start();

        // Act
        _timer.ExecuteCycles(cycles);

        // Assert
        _timer.CycleCount.Should().Be((ulong)cycles);
    }

    [Fact]
    public void ExecuteCyclesMultipleCallsWhenRunningShouldAccumulate()
    {
        // Arrange
        _timer.Start();

        // Act
        _timer.ExecuteCycles(10);
        _timer.ExecuteCycles(20);
        _timer.ExecuteCycles(30);

        // Assert
        _timer.CycleCount.Should().Be(60);
    }

    [Fact]
    public void GetElapsedTimeShouldCalculateCorrectTimeFromCycles()
    {
        // Arrange
        _timer.Start();
        _timer.ExecuteCycles(1789773); // 1 second worth of cycles

        // Act
        var elapsed = _timer.GetElapsedTime();

        // Assert
        elapsed.TotalSeconds.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void GetElapsedTimeWithZeroCyclesShouldReturnZero()
    {
        // Act
        var elapsed = _timer.GetElapsedTime();

        // Assert
        elapsed.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CalculateCyclesForTimeShouldReturnCorrectCycleCount()
    {
        // Arrange
        var oneSecond = TimeSpan.FromSeconds(1);

        // Act
        var cycles = CycleTimer.CalculateCyclesForTime(oneSecond);

        // Assert
        cycles.Should().Be(1789773);
    }

    [Fact]
    public void CalculateCyclesForTimeWithFractionalSecondsShouldRoundCorrectly()
    {
        // Arrange
        var halfSecond = TimeSpan.FromMilliseconds(500);

        // Act
        var cycles = CycleTimer.CalculateCyclesForTime(halfSecond);

        // Assert
        cycles.Should().BeInRange(894885UL, 894887UL); // Half of 1789773
    }

    [Fact]
    public void SynchronizeWithTargetWhenAheadShouldReturnWaitTime()
    {
        // Arrange
        _timer.Start();
        var targetCycles = 1000UL;
        _timer.ExecuteCycles(500); // We're behind target

        // Act
        var waitTime = _timer.SynchronizeWithTarget(targetCycles);

        // Assert
        waitTime.Should().Be(TimeSpan.Zero); // No wait needed when behind
    }

    [Fact]
    public void SynchronizeWithTargetWhenBehindShouldReturnZero()
    {
        // Arrange
        _timer.Start();
        var targetCycles = 500UL;
        _timer.ExecuteCycles(1000); // We're ahead of target

        // Act
        var waitTime = _timer.SynchronizeWithTarget(targetCycles);

        // Assert
        waitTime.Should().BeGreaterThan(TimeSpan.Zero); // Should wait to sync
    }

    [Fact]
    public void GetCyclesPerFrameShouldReturnNTSCFrameCycles()
    {
        // Act
        var cyclesPerFrame = CycleTimer.GetCyclesPerFrame();

        // Assert
        // NTSC: 60 FPS, so 1789773 / 60 ≈ 29829 cycles per frame
        cyclesPerFrame.Should().BeInRange(29828UL, 29830UL);
    }

    [Fact]
    public void FrameRateShouldReturnNTSCFrameRate()
    {
        // Act
        var frameRate = CycleTimer.FrameRate;

        // Assert
        frameRate.Should().BeApproximately(60.0, 0.1);
    }

    [Fact]
    public void IsFrameBoundaryAtFrameBoundaryShouldReturnTrue()
    {
        // Arrange
        _timer.Start();
        var cyclesPerFrame = CycleTimer.GetCyclesPerFrame();
        _timer.ExecuteCycles((int)cyclesPerFrame);

        // Act
        var isFrameBoundary = _timer.IsFrameBoundary();

        // Assert
        isFrameBoundary.Should().BeTrue();
    }

    [Fact]
    public void IsFrameBoundaryNotAtFrameBoundaryShouldReturnFalse()
    {
        // Arrange
        _timer.Start();
        _timer.ExecuteCycles(100); // Not at frame boundary

        // Act
        var isFrameBoundary = _timer.IsFrameBoundary();

        // Assert
        isFrameBoundary.Should().BeFalse();
    }

    [Fact]
    public void GetCurrentFrameShouldCalculateCorrectFrameNumber()
    {
        // Arrange
        _timer.Start();
        var cyclesPerFrame = CycleTimer.GetCyclesPerFrame();
        _timer.ExecuteCycles((int)(cyclesPerFrame * 2.5)); // 2.5 frames

        // Act
        var currentFrame = _timer.GetCurrentFrame();

        // Assert
        currentFrame.Should().Be(2); // Should be in frame 2 (0-indexed)
    }
}
