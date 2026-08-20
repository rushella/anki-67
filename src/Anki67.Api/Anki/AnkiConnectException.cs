namespace Anki67.Api.Anki;

public sealed class AnkiConnectException : Exception
{
    public AnkiConnectException(string message)
        : base(message)
    {
    }

    public AnkiConnectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
