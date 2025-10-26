#pragma warning disable CA1707 // Identifiers should not contain underscores - Test method naming convention
#pragma warning disable CA2000 // Dispose objects before losing scope - Test objects
#pragma warning disable CA2201 // Do not raise reserved exception types - Test exceptions
#pragma warning disable CA2263 // Prefer generic overload - Test code
using System;
using System.Threading;
using System.Threading.Tasks;
using EightBitten.Core.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace EightBitten.Tests.Unit.Core.Contracts;

/// <summary>
/// Unit tests for IEmulator interface and related types
/// </summary>
public class IEmulatorTests
{
    [Fact]
    public void EmulatorState_ShouldHaveAllExpectedValues()
    {
        // Arrange & Act
        var states = Enum.GetValues<EmulatorState>();

        // Assert
        states.Should().Contain(EmulatorState.Uninitialized);
        states.Should().Contain(EmulatorState.Ready);
        states.Should().Contain(EmulatorState.Running);
        states.Should().Contain(EmulatorState.Paused);
        states.Should().Contain(EmulatorState.Stopped);
        states.Should().Contain(EmulatorState.Error);
    }

    [Fact]
    public void EmulatorStateChangedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var previousState = EmulatorState.Ready;
        var newState = EmulatorState.Running;
        var reason = "User started emulation";

        // Act
        var eventArgs = new EmulatorStateChangedEventArgs(previousState, newState, reason);

        // Assert
        eventArgs.PreviousState.Should().Be(previousState);
        eventArgs.NewState.Should().Be(newState);
        eventArgs.Reason.Should().Be(reason);
    }

    [Fact]
    public void EmulatorStateChangedEventArgs_ShouldAllowNullReason()
    {
        // Arrange
        var previousState = EmulatorState.Running;
        var newState = EmulatorState.Paused;

        // Act
        var eventArgs = new EmulatorStateChangedEventArgs(previousState, newState);

        // Assert
        eventArgs.PreviousState.Should().Be(previousState);
        eventArgs.NewState.Should().Be(newState);
        eventArgs.Reason.Should().BeNull();
    }

    [Fact]
    public void FrameCompletedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var frameNumber = 12345L;
        var cycleCount = 987654321L;
        var frameTime = TimeSpan.FromMilliseconds(16.67);

        // Act
        var eventArgs = new FrameCompletedEventArgs(frameNumber, cycleCount, frameTime);

        // Assert
        eventArgs.FrameNumber.Should().Be(frameNumber);
        eventArgs.CycleCount.Should().Be(cycleCount);
        eventArgs.FrameTime.Should().Be(frameTime);
    }

    [Fact]
    public void EmulationErrorEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "CPU execution";
        var isFatal = true;

        // Act
        var eventArgs = new EmulationErrorEventArgs(exception, context, isFatal);

        // Assert
        eventArgs.Exception.Should().Be(exception);
        eventArgs.Context.Should().Be(context);
        eventArgs.IsFatal.Should().Be(isFatal);
    }

    [Fact]
    public void EmulationErrorEventArgs_ShouldDefaultToNonFatal()
    {
        // Arrange
        var exception = new ArgumentException("Test exception");
        var context = "Memory access";

        // Act
        var eventArgs = new EmulationErrorEventArgs(exception, context);

        // Assert
        eventArgs.Exception.Should().Be(exception);
        eventArgs.Context.Should().Be(context);
        eventArgs.IsFatal.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmulatorState.Uninitialized)]
    [InlineData(EmulatorState.Ready)]
    [InlineData(EmulatorState.Running)]
    [InlineData(EmulatorState.Paused)]
    [InlineData(EmulatorState.Stopped)]
    [InlineData(EmulatorState.Error)]
    public void EmulatorState_AllValuesShouldBeValid(EmulatorState state)
    {
        // Arrange & Act
        var stateValue = (int)state;

        // Assert
        stateValue.Should().BeGreaterThanOrEqualTo(0);
        Enum.IsDefined(typeof(EmulatorState), state).Should().BeTrue();
    }

    [Fact]
    public async Task IEmulator_LoadRomAsync_ShouldAcceptValidParameters()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        var romData = new ReadOnlyMemory<byte>(new byte[] { 0x4E, 0x45, 0x53, 0x1A }); // NES header
        var cancellationToken = CancellationToken.None;

        mockEmulator.Setup(e => e.LoadRomAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        // Act
        var result = await mockEmulator.Object.LoadRomAsync(romData, cancellationToken);

        // Assert
        result.Should().BeTrue();
        mockEmulator.Verify(e => e.LoadRomAsync(romData, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task IEmulator_StartAsync_ShouldAcceptCancellationToken()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        var cancellationToken = new CancellationTokenSource().Token;

        mockEmulator.Setup(e => e.StartAsync(It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        // Act
        await mockEmulator.Object.StartAsync(cancellationToken);

        // Assert
        mockEmulator.Verify(e => e.StartAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public void IEmulator_StepFrame_ShouldReturnBoolean()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        mockEmulator.Setup(e => e.StepFrame()).Returns(true);

        // Act
        var result = mockEmulator.Object.StepFrame();

        // Assert
        result.Should().BeTrue();
        mockEmulator.Verify(e => e.StepFrame(), Times.Once);
    }

    [Fact]
    public void IEmulator_StepInstruction_ShouldReturnCycleCount()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        var expectedCycles = 4;
        mockEmulator.Setup(e => e.StepInstruction()).Returns(expectedCycles);

        // Act
        var result = mockEmulator.Object.StepInstruction();

        // Assert
        result.Should().Be(expectedCycles);
        mockEmulator.Verify(e => e.StepInstruction(), Times.Once);
    }

    [Fact]
    public async Task IEmulator_SaveStateAsync_ShouldReturnByteArray()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        var expectedState = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        mockEmulator.Setup(e => e.SaveStateAsync()).ReturnsAsync(expectedState);

        // Act
        var result = await mockEmulator.Object.SaveStateAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedState);
        mockEmulator.Verify(e => e.SaveStateAsync(), Times.Once);
    }

    [Fact]
    public async Task IEmulator_LoadStateAsync_ShouldAcceptStateData()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        var stateData = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var cancellationToken = CancellationToken.None;

        mockEmulator.Setup(e => e.LoadStateAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        // Act
        var result = await mockEmulator.Object.LoadStateAsync(stateData, cancellationToken);

        // Assert
        result.Should().BeTrue();
        mockEmulator.Verify(e => e.LoadStateAsync(stateData, cancellationToken), Times.Once);
    }

    [Fact]
    public void IEmulator_Properties_ShouldBeReadable()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        mockEmulator.Setup(e => e.State).Returns(EmulatorState.Running);
        mockEmulator.Setup(e => e.FrameNumber).Returns(12345L);
        mockEmulator.Setup(e => e.CycleCount).Returns(987654321L);
        mockEmulator.Setup(e => e.IsHeadless).Returns(true);

        // Act & Assert
        mockEmulator.Object.State.Should().Be(EmulatorState.Running);
        mockEmulator.Object.FrameNumber.Should().Be(12345L);
        mockEmulator.Object.CycleCount.Should().Be(987654321L);
        mockEmulator.Object.IsHeadless.Should().BeTrue();
    }

    [Fact]
    public void IEmulator_ControlMethods_ShouldBeCallable()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();

        // Act & Assert - These should not throw
        mockEmulator.Object.Pause();
        mockEmulator.Object.ResumeEmulation();
        mockEmulator.Object.StopEmulation();
        mockEmulator.Object.Reset();

        // Verify all methods were called
        mockEmulator.Verify(e => e.Pause(), Times.Once);
        mockEmulator.Verify(e => e.ResumeEmulation(), Times.Once);
        mockEmulator.Verify(e => e.StopEmulation(), Times.Once);
        mockEmulator.Verify(e => e.Reset(), Times.Once);
    }

    [Fact]
    public void IEmulator_Events_ShouldBeSubscribable()
    {
        // Arrange
        var mockEmulator = new Mock<IEmulator>();
        var stateChangedCalled = false;
        var frameCompletedCalled = false;
        var emulationErrorCalled = false;

        // Act
        mockEmulator.Object.StateChanged += (sender, args) => stateChangedCalled = true;
        mockEmulator.Object.FrameCompleted += (sender, args) => frameCompletedCalled = true;
        mockEmulator.Object.EmulationError += (sender, args) => emulationErrorCalled = true;

        // Simulate events
        mockEmulator.Raise(e => e.StateChanged += null, 
            new EmulatorStateChangedEventArgs(EmulatorState.Ready, EmulatorState.Running));
        mockEmulator.Raise(e => e.FrameCompleted += null, 
            new FrameCompletedEventArgs(1, 1000, TimeSpan.FromMilliseconds(16.67)));
        mockEmulator.Raise(e => e.EmulationError += null, 
            new EmulationErrorEventArgs(new Exception("Test"), "Test context"));

        // Assert
        stateChangedCalled.Should().BeTrue();
        frameCompletedCalled.Should().BeTrue();
        emulationErrorCalled.Should().BeTrue();
    }
}
