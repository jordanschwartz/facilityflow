namespace FacilityFlow.Core.Exceptions;

public class InvalidTransitionException : Exception
{
    public InvalidTransitionException(string from, string to)
        : base($"Invalid status transition from '{from}' to '{to}'") { }
}
