namespace FreeStays.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException() : base("Unauthorized access.")
    {
    }
    
    public UnauthorizedException(string message) : base(message)
    {
    }
}
