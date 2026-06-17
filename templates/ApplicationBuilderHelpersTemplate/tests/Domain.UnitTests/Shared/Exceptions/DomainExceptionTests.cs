using Domain.Shared.Exceptions;

namespace Domain.UnitTests.Shared.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var message = "Test error message";

        var exception = new DomainException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var message = "Outer error";
        var innerException = new InvalidOperationException("Inner error");

        var exception = new DomainException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }
}
