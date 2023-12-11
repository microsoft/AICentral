using AICentral;
using AICentral.Core;

namespace AICentralTests.TestHelpers;

public class FakeDateTimeProvider: IDateTimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.Now;

    public DateTimeOffset Now => _now;

    public void Advance(TimeSpan timeSpan)
    {
        _now = _now.Add(timeSpan);
    }
}