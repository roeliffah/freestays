namespace FreeStays.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException() : base("Access forbidden.")
    {
    }
    
    public ForbiddenException(string message) : base(message)
    {
    }
}
