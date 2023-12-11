namespace AICentral.Core;

public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
}