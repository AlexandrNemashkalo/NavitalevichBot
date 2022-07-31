namespace NavitalevichBot.Exceptions;

public class UncorrectDataException : Exception
{
    public UncorrectDataException(string message = "") : base(message) { }
}