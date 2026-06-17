using Domain.Shared.Exceptions;

namespace Domain.UnitTests.Shared.Exceptions;

public class ValidationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var message = "Validation failed";

        var exception = new ValidationException(message);

        Assert.Equal(message, exception.Message);
        Assert.Null(exception.PropertyName);
    }

    [Fact]
    public void Constructor_WithPropertyAndMessage_SetsBoth()
    {
        var propertyName = "Email";
        var message = "Invalid email format";

        var exception = new ValidationException(propertyName, message);

        Assert.Equal(message, exception.Message);
        Assert.Equal(propertyName, exception.PropertyName);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var message = "Validation failed";
        var innerException = new FormatException("Invalid format");

        var exception = new ValidationException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }
}
