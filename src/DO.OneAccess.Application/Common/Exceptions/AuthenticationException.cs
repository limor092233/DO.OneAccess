namespace DO.OneAccess.Application.Common.Exceptions;

public class AuthenticationException : Exception
{
    public AuthenticationException(string message = "Invalid username or password.") 
        : base(message)
    {
    }
}
