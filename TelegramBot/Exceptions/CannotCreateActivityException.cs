namespace TelegramBot.Exceptions;

public class CannotCreateActivityException : Exception
{
    public CannotCreateActivityException(string message) : base(message) { }
}