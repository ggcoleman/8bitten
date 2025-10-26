#pragma warning disable CA1707 // Identifiers should not contain underscores - Test method naming convention
using System;
using EightBitten.Core.CPU;
using FluentAssertions;
using Xunit;

namespace EightBitten.Tests.Unit.Core.CPU;

/// <summary>
/// Unit tests for CPU state management and processor flags
/// </summary>
public class CPUStateTests
{
    [Fact]
    public void CPUState_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var state = new CPUState();

        // Assert
        state.ComponentName.Should().Be("CPU");
        state.PC.Should().Be(0x0000);
        state.SP.Should().Be(0xFD);
        state.A.Should().Be(0x00);
        state.X.Should().Be(0x00);
        state.Y.Should().Be(0x00);
        state.P.Should().HaveFlag(ProcessorStatus.Interrupt);
        state.P.Should().HaveFlag(ProcessorStatus.Unused);
        state.CurrentInstruction.Should().Be(0x00);
        state.InstructionCycle.Should().Be(0);
        state.TotalCycles.Should().Be(0);
        state.IsHalted.Should().BeFalse();
        state.PendingInterrupts.Should().Be(InterruptType.None);
    }

    [Fact]
    public void CPUState_Reset_ShouldRestorePowerOnState()
    {
        // Arrange
        var state = new CPUState
        {
            PC = 0x1234,
            SP = 0x00,
            A = 0xFF,
            X = 0xFF,
            Y = 0xFF,
            P = ProcessorStatus.Carry | ProcessorStatus.Zero,
            TotalCycles = 12345,
            IsHalted = true,
            PendingInterrupts = InterruptType.NMI
        };

        // Act
        state.Reset();

        // Assert
        state.PC.Should().Be(0x0000);
        state.SP.Should().Be(0xFD);
        state.A.Should().Be(0x00);
        state.X.Should().Be(0x00);
        state.Y.Should().Be(0x00);
        state.P.Should().HaveFlag(ProcessorStatus.Interrupt);
        state.P.Should().HaveFlag(ProcessorStatus.Unused);
        state.TotalCycles.Should().Be(0);
        state.IsHalted.Should().BeFalse();
        state.PendingInterrupts.Should().Be(InterruptType.None);
    }

    [Fact]
    public void CPUState_Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new CPUState
        {
            PC = 0x1234,
            SP = 0xFE,
            A = 0x42,
            X = 0x24,
            Y = 0x84,
            P = ProcessorStatus.Carry | ProcessorStatus.Zero | ProcessorStatus.Negative,
            CurrentInstruction = 0xEA,
            InstructionCycle = 2,
            TotalCycles = 98765,
            IsHalted = true,
            PendingInterrupts = InterruptType.IRQ,
            LastMemoryAddress = 0x2000,
            LastMemoryValue = 0x55,
            LastMemoryWasWrite = true
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.PC.Should().Be(original.PC);
        clone.SP.Should().Be(original.SP);
        clone.A.Should().Be(original.A);
        clone.X.Should().Be(original.X);
        clone.Y.Should().Be(original.Y);
        clone.P.Should().Be(original.P);
        clone.CurrentInstruction.Should().Be(original.CurrentInstruction);
        clone.InstructionCycle.Should().Be(original.InstructionCycle);
        clone.TotalCycles.Should().Be(original.TotalCycles);
        clone.IsHalted.Should().Be(original.IsHalted);
        clone.PendingInterrupts.Should().Be(original.PendingInterrupts);
        clone.LastMemoryAddress.Should().Be(original.LastMemoryAddress);
        clone.LastMemoryValue.Should().Be(original.LastMemoryValue);
        clone.LastMemoryWasWrite.Should().Be(original.LastMemoryWasWrite);
    }

    [Fact]
    public void CPUState_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var state = new CPUState
        {
            PC = 0x8000,
            SP = 0xFF,
            A = 0x42,
            X = 0x24,
            Y = 0x84,
            P = ProcessorStatus.Carry | ProcessorStatus.Zero,
            TotalCycles = 12345
        };

        // Act
        var result = state.ToString();

        // Assert
        result.Should().Contain("PC:$8000");
        result.Should().Contain("SP:$FF");
        result.Should().Contain("A:$42");
        result.Should().Contain("X:$24");
        result.Should().Contain("Y:$84");
        result.Should().Contain("Cycles:12345");
    }

    [Theory]
    [InlineData(ProcessorStatus.Carry, 0x01)]
    [InlineData(ProcessorStatus.Zero, 0x02)]
    [InlineData(ProcessorStatus.Interrupt, 0x04)]
    [InlineData(ProcessorStatus.DecimalMode, 0x08)]
    [InlineData(ProcessorStatus.Break, 0x10)]
    [InlineData(ProcessorStatus.Unused, 0x20)]
    [InlineData(ProcessorStatus.Overflow, 0x40)]
    [InlineData(ProcessorStatus.Negative, 0x80)]
    public void ProcessorFlags_ShouldHaveCorrectBitValues(ProcessorStatus flag, byte expectedValue)
    {
        // Act & Assert
        ((byte)flag).Should().Be(expectedValue);
    }

    [Fact]
    public void ProcessorFlags_HasFlag_ShouldWorkCorrectly()
    {
        // Arrange
        var flags = ProcessorStatus.Carry | ProcessorStatus.Zero | ProcessorStatus.Negative;

        // Act & Assert
        flags.HasFlag(ProcessorStatus.Carry).Should().BeTrue();
        flags.HasFlag(ProcessorStatus.Zero).Should().BeTrue();
        flags.HasFlag(ProcessorStatus.Negative).Should().BeTrue();
        flags.HasFlag(ProcessorStatus.Overflow).Should().BeFalse();
        flags.HasFlag(ProcessorStatus.Interrupt).Should().BeFalse();
    }

    [Theory]
    [InlineData(ProcessorStatus.Carry, true)]
    [InlineData(ProcessorStatus.Zero, false)]
    [InlineData(ProcessorStatus.Negative, true)]
    public void ProcessorFlags_SetFlag_ShouldModifyCorrectly(ProcessorStatus flag, bool value)
    {
        // Arrange
        var flags = ProcessorStatus.Unused; // Start with just unused bit

        // Act
        flags = flags.SetFlag(flag, value);

        // Assert
        flags.HasFlag(flag).Should().Be(value);
        flags.HasFlag(ProcessorStatus.Unused).Should().BeTrue(); // Should preserve other flags
    }

    [Theory]
    [InlineData(0x00, true, false)]  // Zero value sets Z, clears N
    [InlineData(0x01, false, false)] // Non-zero, positive clears Z and N
    [InlineData(0x80, false, true)]  // Negative value clears Z, sets N
    [InlineData(0xFF, false, true)]  // Negative value clears Z, sets N
    public void ProcessorFlags_UpdateZN_ShouldSetFlagsCorrectly(byte value, bool expectedZero, bool expectedNegative)
    {
        // Arrange
        var flags = ProcessorStatus.Unused;

        // Act
        flags = flags.UpdateZN(value);

        // Assert
        flags.HasFlag(ProcessorStatus.Zero).Should().Be(expectedZero);
        flags.HasFlag(ProcessorStatus.Negative).Should().Be(expectedNegative);
        flags.HasFlag(ProcessorStatus.Unused).Should().BeTrue(); // Should preserve other flags
    }

    [Theory]
    [InlineData(InterruptType.None, 0x00)]
    [InlineData(InterruptType.NMI, 0x01)]
    [InlineData(InterruptType.IRQ, 0x02)]
    [InlineData(InterruptType.Reset, 0x04)]
    public void InterruptFlags_ShouldHaveCorrectValues(InterruptType flag, byte expectedValue)
    {
        // Act & Assert
        ((byte)flag).Should().Be(expectedValue);
    }

    [Fact]
    public void InterruptFlags_ShouldSupportCombinations()
    {
        // Arrange
        var combined = InterruptType.NMI | InterruptType.IRQ;

        // Act & Assert
        combined.Should().HaveFlag(InterruptType.NMI);
        combined.Should().HaveFlag(InterruptType.IRQ);
        combined.Should().NotHaveFlag(InterruptType.Reset);
        ((byte)combined).Should().Be(0x03);
    }

    [Fact]
    public void CPUState_MemoryTracking_ShouldWorkCorrectly()
    {
        // Arrange
        var state = new CPUState();

        // Act
        state.LastMemoryAddress = 0x2000;
        state.LastMemoryValue = 0x42;
        state.LastMemoryWasWrite = true;

        // Assert
        state.LastMemoryAddress.Should().Be(0x2000);
        state.LastMemoryValue.Should().Be(0x42);
        state.LastMemoryWasWrite.Should().BeTrue();
    }

    [Fact]
    public void CPUState_InstructionTracking_ShouldWorkCorrectly()
    {
        // Arrange
        var state = new CPUState();

        // Act
        state.CurrentInstruction = 0xEA; // NOP
        state.InstructionCycle = 2;

        // Assert
        state.CurrentInstruction.Should().Be(0xEA);
        state.InstructionCycle.Should().Be(2);
    }

    [Fact]
    public void CPUState_CycleTracking_ShouldAccumulate()
    {
        // Arrange
        var state = new CPUState();

        // Act
        state.TotalCycles = 1000;
        state.TotalCycles += 500;

        // Assert
        state.TotalCycles.Should().Be(1500);
    }

    [Fact]
    public void ProcessorFlags_AllCombinations_ShouldBeValid()
    {
        // Arrange
        var allFlags = ProcessorStatus.Carry | ProcessorStatus.Zero | ProcessorStatus.Interrupt |
                      ProcessorStatus.DecimalMode | ProcessorStatus.Break | ProcessorStatus.Unused |
                      ProcessorStatus.Overflow | ProcessorStatus.Negative;

        // Act & Assert
        ((byte)allFlags).Should().Be(0xFF);

        // Each individual flag should be detectable
        allFlags.HasFlag(ProcessorStatus.Carry).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.Zero).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.Interrupt).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.DecimalMode).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.Break).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.Unused).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.Overflow).Should().BeTrue();
        allFlags.HasFlag(ProcessorStatus.Negative).Should().BeTrue();
    }
}
