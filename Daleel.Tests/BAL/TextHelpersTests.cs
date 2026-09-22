using Daleel.BAL.Services;

namespace Daleel.Tests.BAL
{
    public class TextHelpersTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("\t\r\n", null)]
        [InlineData("Hello", "Hello")]
        [InlineData("  Hello World  ", "Hello World")]
        [InlineData("  شركة دليل  ", "شركة دليل")]
        public void Clean_NormalizesInputProperly(string? input, string? expected)
        {
            // Act
            var result = Text.Clean(input);

            // Assert
            result.Should().Be(expected);
        }
    }
}
