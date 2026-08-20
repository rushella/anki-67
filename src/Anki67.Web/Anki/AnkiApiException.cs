namespace Anki67.Web.Anki;

public sealed class AnkiApiException : Exception
{
    public AnkiApiException(string message)
        : base(message)
    {
    }

    public AnkiApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
