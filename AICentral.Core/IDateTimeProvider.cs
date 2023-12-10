namespace AICentral;

public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
}