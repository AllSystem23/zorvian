namespace Zorvian.Core.Domain;

public sealed class InvalidWarrantyStateTransitionException : InvalidOperationException
{
    public InvalidWarrantyStateTransitionException(string message) : base(message)
    {
    }
}
