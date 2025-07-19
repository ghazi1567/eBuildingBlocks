namespace eBuildingBlocks.Application.Exceptions;

public class MethodNotAllowedException(string error) : Exception
{
    public string Error { get; } = error;
}