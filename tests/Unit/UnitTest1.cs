namespace EightBitten.Tests.Unit;

/// <summary>
/// Basic unit test to verify test infrastructure
/// </summary>
public class UnitTest1
{
    /// <summary>
    /// Basic test to verify test framework is working
    /// </summary>
    [Fact]
    public void Test1()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }
}
