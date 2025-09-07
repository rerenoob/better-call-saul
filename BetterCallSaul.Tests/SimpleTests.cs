using Xunit;

namespace BetterCallSaul.Tests;

public class SimpleTests
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = 4;
        
        // Act
        var actual = 2 + 2;
        
        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 3, 5)]
    [InlineData(10, 15, 25)]
    public void AdditionTheoryTest_ShouldPass(int a, int b, int expected)
    {
        // Act
        var actual = a + b;
        
        // Assert
        Assert.Equal(expected, actual);
    }
}