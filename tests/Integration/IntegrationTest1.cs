namespace EightBitten.Tests.Integration;

/// <summary>
/// Basic integration test to verify test infrastructure
/// </summary>
public class IntegrationTest1
{
    /// <summary>
    /// Basic test to verify integration test framework is working
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
