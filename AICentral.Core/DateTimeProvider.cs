namespace AICentral.Core;

/// <summary>
/// Implementation of IDateTimeProvider using DateTimeOffset.Now
/// </summary>
public class DateTimeProvider: IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}